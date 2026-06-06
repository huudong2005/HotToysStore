using System.Data;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Oracle.ManagedDataAccess.Client;
using ToyStore.Domain.Interfaces;
using ToyStore.Helpers;
using ToyStore.Models;
using ToyStore.Services;

namespace ToyStore.Controllers;

public class PaymentController : Controller
{
    private readonly IConfiguration _configuration;
    private readonly VnPaySettings _vnPaySettings;
    private readonly ISessionService _sessionService;
    private readonly IGuestCheckoutService _guestCheckoutService;
    private readonly ICartStorageService _cartStorage;
    private readonly DiscountService _discountService;
    private readonly ILogger<PaymentController> _logger;

    public PaymentController(
        IConfiguration configuration,
        IOptions<VnPaySettings> vnPaySettings,
        ISessionService sessionService,
        IGuestCheckoutService guestCheckoutService,
        ICartStorageService cartStorage,
        DiscountService discountService,
        ILogger<PaymentController> logger)
    {
        _configuration = configuration;
        _vnPaySettings = vnPaySettings.Value;
        _sessionService = sessionService;
        _guestCheckoutService = guestCheckoutService;
        _cartStorage = cartStorage;
        _discountService = discountService;
        _logger = logger;
    }

    /// <summary>
    /// Bước A: Tạo đơn hàng Pending qua SP_CreateOrderHeader (alias SP_CreateOrder).
    /// Bước B: Tạo URL VNPAY và redirect khách hàng.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreatePaymentUrl()
    {
        try
        {
            var customerId = await ResolveCustomerIdForPaymentAsync();
            if (customerId == 0)
            {
                TempData["ErrorMessage"] = "Vui lòng nhập đầy đủ thông tin giao hàng trước khi thanh toán VNPAY.";
                return RedirectToAction("Checkout", "Cart");
            }

            var cart = await _cartStorage.GetCartAsync(HttpContext);
            if (!cart.Items.Any())
            {
                TempData["ErrorMessage"] = "Giỏ hàng trống, không thể thanh toán.";
                return RedirectToAction("Index", "Cart");
            }

            var subtotal = cart.Subtotal;
            var (discountValue, finalTotal) = _discountService.CalculateDiscountAndTotal(
                subtotal,
                cart.DiscountStrategyName);

            var discountStrategy = string.IsNullOrWhiteSpace(cart.DiscountStrategyName)
                ? "NoDiscount"
                : cart.DiscountStrategyName!;

            var orderId = await CreateOrderWithDetailsViaOracleAsync(
                customerId,
                finalTotal,
                "VNPAY",
                "Standard",
                subtotal,
                discountValue,
                discountStrategy,
                cart);

            if (!_sessionService.IsCustomer(HttpContext))
            {
                GuestOrderSession.GrantOrderAccess(HttpContext, orderId);
            }

            var paymentUrl = BuildVnPayPaymentUrl(orderId, finalTotal, customerId);
            return Redirect(paymentUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CreatePaymentUrl failed");
            TempData["ErrorMessage"] = "Lỗi khi tạo thanh toán VNPAY: " + ex.Message;
            return RedirectToAction("Checkout", "Cart");
        }
    }

    /// <summary>
    /// GET fallback khi OrderController chuyển hướng sau POST checkout.
    /// </summary>
    [HttpGet]
    public Task<IActionResult> CreatePaymentUrlGet()
    {
        return CreatePaymentUrlInternalAsync();
    }

    /// <summary>
    /// Callback sau khi khách thanh toán trên cổng VNPAY.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> PaymentCallback()
    {
        var secureHash = Request.Query["vnp_SecureHash"].ToString();
        var responseCode = Request.Query["vnp_ResponseCode"].ToString();
        var txnRef = Request.Query["vnp_TxnRef"].ToString();
        var transactionNo = Request.Query["vnp_TransactionNo"].ToString();
        var amount = Request.Query["vnp_Amount"].ToString();
        var bankCode = Request.Query["vnp_BankCode"].ToString();
        var bankTranNo = Request.Query["vnp_BankTranNo"].ToString();
        var cardType = Request.Query["vnp_CardType"].ToString();
        var orderInfo = Request.Query["vnp_OrderInfo"].ToString();
        var payDate = Request.Query["vnp_PayDate"].ToString();

        var vnPayLibrary = new VnPayLibrary();
        VnPayLibrary.LoadResponseFromQuery(vnPayLibrary, Request.Query);

        var isSignatureValid = vnPayLibrary.ValidateSignature(secureHash, _vnPaySettings.HashSecret);
        var isSuccess = isSignatureValid && responseCode == "00";

        if (isSuccess)
        {
            if (int.TryParse(txnRef, out var orderId))
            {
                var customerId = _sessionService.GetUserId(HttpContext);
                await _cartStorage.ClearCartAfterOrderAsync(HttpContext, customerId);

                if (!_sessionService.IsCustomer(HttpContext))
                {
                    GuestOrderSession.GrantOrderAccess(HttpContext, orderId);
                    GuestOrderSession.ClearGuestCheckout(HttpContext);
                }

                try
                {
                    await UpdateVnPayResultViaOracleAsync(orderId, transactionNo, "Confirmed");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "PaymentCallback: could not update order #{OrderId} status", orderId);
                }
            }
        }

        var viewModel = new VnPayPaymentResultViewModel
        {
            IsSuccess = isSuccess,
            IsSignatureValid = isSignatureValid,
            OrderId = txnRef,
            TransactionNo = transactionNo,
            ResponseCode = responseCode,
            Amount = amount,
            DisplayAmount = FormatVnPayAmount(amount),
            OrderInfo = WebUtility.UrlDecode(orderInfo.Replace('+', ' ')),
            PayDate = FormatVnPayPayDate(payDate),
            BankCode = bankCode,
            BankTranNo = bankTranNo,
            CardType = cardType,
            Message = isSuccess
                ? "Thanh toán VNPAY thành công! Đơn hàng của bạn đã được xác nhận."
                : isSignatureValid
                    ? "Thanh toán VNPAY thất bại hoặc đã bị hủy."
                    : "Chữ ký VNPAY không hợp lệ. Giao dịch có thể bị giả mạo."
        };

        return View("PaymentCallback", viewModel);
    }

    private static string FormatVnPayAmount(string amountRaw)
    {
        if (!long.TryParse(amountRaw, out var amount))
        {
            return amountRaw;
        }

        return (amount / 100m).ToString("N0") + " ₫";
    }

    private static string FormatVnPayPayDate(string payDateRaw)
    {
        if (string.IsNullOrWhiteSpace(payDateRaw) || payDateRaw.Length < 14)
        {
            return payDateRaw;
        }

        if (DateTime.TryParseExact(
                payDateRaw,
                "yyyyMMddHHmmss",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None,
                out var payDate))
        {
            return payDate.ToString("dd/MM/yyyy HH:mm:ss");
        }

        return payDateRaw;
    }

    /// <summary>
    /// IPN webhook VNPAY gọi ngầm để cập nhật trạng thái đơn hàng.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> VnpayIpn()
    {
        try
        {
            var secureHash = Request.Query["vnp_SecureHash"].ToString();
            var responseCode = Request.Query["vnp_ResponseCode"].ToString();
            var txnRef = Request.Query["vnp_TxnRef"].ToString();
            var transactionNo = Request.Query["vnp_TransactionNo"].ToString();

            var vnPayLibrary = new VnPayLibrary();
            VnPayLibrary.LoadResponseFromQuery(vnPayLibrary, Request.Query);

            if (!vnPayLibrary.ValidateSignature(secureHash, _vnPaySettings.HashSecret))
            {
                return Json(new { RspCode = "97", Message = "Invalid Signature" });
            }

            if (!int.TryParse(txnRef, out var orderId))
            {
                return Json(new { RspCode = "01", Message = "Order Not Found" });
            }

            var status = responseCode == "00" ? "Confirmed" : "Cancelled";
            await UpdateVnPayResultViaOracleAsync(orderId, transactionNo, status);

            return Json(new { RspCode = "00", Message = "Confirm Success" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "VnpayIpn failed");
            return Json(new { RspCode = "99", Message = "Unknown Error" });
        }
    }

    private async Task<IActionResult> CreatePaymentUrlInternalAsync()
    {
        try
        {
            var customerId = await ResolveCustomerIdForPaymentAsync();
            if (customerId == 0)
            {
                TempData["ErrorMessage"] = "Vui lòng nhập đầy đủ thông tin giao hàng trước khi thanh toán VNPAY.";
                return RedirectToAction("Checkout", "Cart");
            }

            var cart = await _cartStorage.GetCartAsync(HttpContext);
            if (!cart.Items.Any())
            {
                TempData["ErrorMessage"] = "Giỏ hàng trống, không thể thanh toán.";
                return RedirectToAction("Index", "Cart");
            }

            var subtotal = cart.Subtotal;
            var (discountValue, finalTotal) = _discountService.CalculateDiscountAndTotal(
                subtotal,
                cart.DiscountStrategyName);

            var discountStrategy = string.IsNullOrWhiteSpace(cart.DiscountStrategyName)
                ? "NoDiscount"
                : cart.DiscountStrategyName!;

            var orderId = await CreateOrderWithDetailsViaOracleAsync(
                customerId,
                finalTotal,
                "VNPAY",
                "Standard",
                subtotal,
                discountValue,
                discountStrategy,
                cart);

            if (!_sessionService.IsCustomer(HttpContext))
            {
                GuestOrderSession.GrantOrderAccess(HttpContext, orderId);
            }

            var paymentUrl = BuildVnPayPaymentUrl(orderId, finalTotal, customerId);
            return Redirect(paymentUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CreatePaymentUrlInternal failed");
            TempData["ErrorMessage"] = "Lỗi khi tạo thanh toán VNPAY: " + ex.Message;
            return RedirectToAction("Checkout", "Cart");
        }
    }

    private string BuildVnPayPaymentUrl(int orderId, decimal totalAmount, int customerId)
    {
        var vnPayLibrary = new VnPayLibrary();
        var createDate = DateTime.Now.ToString("yyyyMMddHHmmss");
        var amount = ((long)(totalAmount * 100)).ToString();
        var ipAddress = GetClientIpAddress();
        var returnUrl = ResolveReturnUrl();

        vnPayLibrary.AddRequestData("vnp_Version", _vnPaySettings.Version);
        vnPayLibrary.AddRequestData("vnp_Command", _vnPaySettings.Command);
        vnPayLibrary.AddRequestData("vnp_TmnCode", _vnPaySettings.TmnCode);
        vnPayLibrary.AddRequestData("vnp_Amount", amount);
        vnPayLibrary.AddRequestData("vnp_CurrCode", "VND");
        vnPayLibrary.AddRequestData("vnp_TxnRef", orderId.ToString());
        vnPayLibrary.AddRequestData("vnp_OrderInfo", $"Thanh toan don hang {orderId}");
        vnPayLibrary.AddRequestData("vnp_OrderType", "other");
        vnPayLibrary.AddRequestData("vnp_Locale", "vn");
        vnPayLibrary.AddRequestData("vnp_ReturnUrl", returnUrl);
        vnPayLibrary.AddRequestData("vnp_CreateDate", createDate);
        vnPayLibrary.AddRequestData("vnp_IpAddr", ipAddress);

        // Không gửi vnp_IpnUrl localhost trong URL thanh toán — VNPAY sandbox báo lỗi 72.
        // IPN cấu hình riêng trên Merchant Portal hoặc qua ngrok URL public.

        var signData = vnPayLibrary.BuildSignDataPreview();
        var secureHash = VnPayLibrary.ComputeHmacSha512(_vnPaySettings.HashSecret, signData);

        _logger.LogInformation(
            "VNPAY Order #{OrderId} | TmnCode={TmnCode} | ReturnUrl={ReturnUrl} | Amount={Amount} | SignData={SignData} | Hash={Hash}",
            orderId,
            _vnPaySettings.TmnCode,
            returnUrl,
            amount,
            signData,
            secureHash);

        return vnPayLibrary.CreateRequestUrl(_vnPaySettings.Url, _vnPaySettings.HashSecret);
    }

    private string ResolveReturnUrl()
    {
        if (_vnPaySettings.UseDynamicReturnUrl)
        {
            var request = HttpContext.Request;
            return $"{request.Scheme}://{request.Host}/Payment/PaymentCallback";
        }

        if (!string.IsNullOrWhiteSpace(_vnPaySettings.ReturnUrl))
        {
            return _vnPaySettings.ReturnUrl;
        }

        var fallbackRequest = HttpContext.Request;
        return $"{fallbackRequest.Scheme}://{fallbackRequest.Host}/Payment/PaymentCallback";
    }

    private async Task<int> CreateOrderWithDetailsViaOracleAsync(
        int customerId,
        decimal totalAmount,
        string paymentMethod,
        string deliveryMethod,
        decimal subtotal,
        decimal discountValue,
        string discountStrategy,
        ShoppingCart cart)
    {
        var connectionString = _configuration.GetConnectionString("ToyStoreDB")
            ?? throw new InvalidOperationException("Connection string 'ToyStoreDB' not found.");

        using var connection = new OracleConnection(connectionString);
        await connection.OpenAsync();

        using var transaction = connection.BeginTransaction();
        try
        {
            var orderId = await ExecuteCreateOrderProcedureAsync(
                connection,
                transaction,
                customerId,
                totalAmount,
                paymentMethod,
                deliveryMethod,
                subtotal,
                discountValue,
                discountStrategy);

            foreach (var item in cart.Items)
            {
                var resultCode = await ExecuteCreateOrderDetailProcedureAsync(
                    connection,
                    transaction,
                    orderId,
                    item.ProductId,
                    item.Quantity,
                    item.Price);

                if (resultCode != 1)
                {
                    throw new InvalidOperationException(
                        $"Sản phẩm {item.ProductName} không đủ tồn kho để thanh toán.");
                }
            }

            await transaction.CommitAsync();
            return orderId;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private static async Task<int> ExecuteCreateOrderProcedureAsync(
        OracleConnection connection,
        OracleTransaction transaction,
        int customerId,
        decimal totalAmount,
        string paymentMethod,
        string deliveryMethod,
        decimal subtotal,
        decimal discountValue,
        string discountStrategy)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.BindByName = true;

        var pCustomerId = new OracleParameter("p_CustomerId", OracleDbType.Int32) { Value = customerId };
        var pTotalAmount = new OracleParameter("p_TotalAmount", OracleDbType.Decimal) { Value = totalAmount };
        var pPaymentMethod = new OracleParameter("p_PaymentMethod", OracleDbType.Varchar2, 50) { Value = paymentMethod };
        var pDeliveryMethod = new OracleParameter("p_DeliveryMethod", OracleDbType.Varchar2, 50) { Value = deliveryMethod };
        var pSubtotal = new OracleParameter("p_Subtotal", OracleDbType.Decimal) { Value = subtotal };
        var pDiscountValue = new OracleParameter("p_DiscountValue", OracleDbType.Decimal) { Value = discountValue };
        var pDiscountStrategy = new OracleParameter("p_DiscountStrategy", OracleDbType.Varchar2, 50) { Value = discountStrategy };
        var pOrderId = new OracleParameter("p_OrderId", OracleDbType.Int32) { Direction = ParameterDirection.Output };

        // Dùng cùng chữ ký với SP_CreateOrderHeader (đã chạy ổn ở luồng COD)
        command.CommandText =
            "BEGIN \"SP_CreateOrderHeader\"(:p_CustomerId, :p_TotalAmount, :p_PaymentMethod, :p_DeliveryMethod, :p_Subtotal, :p_DiscountValue, :p_DiscountStrategy, :p_OrderId); END;";
        command.CommandType = CommandType.Text;

        command.Parameters.Add(pCustomerId);
        command.Parameters.Add(pTotalAmount);
        command.Parameters.Add(pPaymentMethod);
        command.Parameters.Add(pDeliveryMethod);
        command.Parameters.Add(pSubtotal);
        command.Parameters.Add(pDiscountValue);
        command.Parameters.Add(pDiscountStrategy);
        command.Parameters.Add(pOrderId);

        await command.ExecuteNonQueryAsync();

        return Convert.ToInt32(pOrderId.Value.ToString());
    }

    private static async Task<int> ExecuteCreateOrderDetailProcedureAsync(
        OracleConnection connection,
        OracleTransaction transaction,
        int orderId,
        int productId,
        int quantity,
        decimal unitPrice)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.BindByName = true;

        var pOrderId = new OracleParameter("p_OrderId", OracleDbType.Int32) { Value = orderId };
        var pProductId = new OracleParameter("p_ProductId", OracleDbType.Int32) { Value = productId };
        var pQuantity = new OracleParameter("p_Quantity", OracleDbType.Int32) { Value = quantity };
        var pUnitPrice = new OracleParameter("p_UnitPrice", OracleDbType.Decimal) { Value = unitPrice };
        var pResultCode = new OracleParameter("p_ResultCode", OracleDbType.Int32) { Direction = ParameterDirection.Output };

        command.CommandText =
            "BEGIN \"SP_CreateOrderDetail\"(:p_OrderId, :p_ProductId, :p_Quantity, :p_UnitPrice, :p_ResultCode); END;";
        command.CommandType = CommandType.Text;

        command.Parameters.Add(pOrderId);
        command.Parameters.Add(pProductId);
        command.Parameters.Add(pQuantity);
        command.Parameters.Add(pUnitPrice);
        command.Parameters.Add(pResultCode);

        await command.ExecuteNonQueryAsync();

        return Convert.ToInt32(pResultCode.Value.ToString());
    }

    private async Task UpdateVnPayResultViaOracleAsync(int orderId, string transactionNo, string status)
    {
        var connectionString = _configuration.GetConnectionString("ToyStoreDB")
            ?? throw new InvalidOperationException("Connection string 'ToyStoreDB' not found.");

        using var connection = new OracleConnection(connectionString);
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.BindByName = true;

        var pOrderId = new OracleParameter("p_OrderId", OracleDbType.Int32) { Value = orderId };
        var pTransactionNo = new OracleParameter("p_VnPayTransactionNo", OracleDbType.Varchar2, 100) { Value = transactionNo };
        var pStatus = new OracleParameter("p_Status", OracleDbType.Varchar2, 50) { Value = status };

        command.CommandText =
            "BEGIN \"SP_UpdateVnPayResult\"(:p_OrderId, :p_VnPayTransactionNo, :p_Status); END;";
        command.CommandType = CommandType.Text;

        command.Parameters.Add(pOrderId);
        command.Parameters.Add(pTransactionNo);
        command.Parameters.Add(pStatus);

        await command.ExecuteNonQueryAsync();
    }

    private string GetClientIpAddress()
    {
        var ip = HttpContext.Connection.RemoteIpAddress;
        if (ip == null)
        {
            return "127.0.0.1";
        }

        if (IPAddress.IsLoopback(ip))
        {
            return "127.0.0.1";
        }

        if (ip.IsIPv4MappedToIPv6)
        {
            return ip.MapToIPv4().ToString();
        }

        if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
        {
            return ip.ToString();
        }

        return "127.0.0.1";
    }

    private async Task<int> ResolveCustomerIdForPaymentAsync()
    {
        var customerId = _sessionService.GetUserId(HttpContext);
        if (customerId > 0)
        {
            return customerId;
        }

        var guestInfo = GuestOrderSession.GetGuestCheckout(HttpContext);
        if (guestInfo == null)
        {
            return 0;
        }

        try
        {
            return await _guestCheckoutService.ResolveCustomerIdAsync(HttpContext, guestInfo);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ResolveCustomerIdForPayment failed");
            TempData["ErrorMessage"] = ex.Message;
            return 0;
        }
    }

}
