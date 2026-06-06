using ToyStore.Domain.Entities;
using ToyStore.Domain.Interfaces;
using ToyStore.Models;

namespace ToyStore.Services;

public class GuestCheckoutService : IGuestCheckoutService
{
    public const string GuestPasswordPrefix = "GUEST:";

    private readonly IUnitOfWork _unitOfWork;
    private readonly ISessionService _sessionService;

    public GuestCheckoutService(IUnitOfWork unitOfWork, ISessionService sessionService)
    {
        _unitOfWork = unitOfWork;
        _sessionService = sessionService;
    }

    public async Task<int> ResolveCustomerIdAsync(HttpContext context, GuestCheckoutInfo guestInfo)
    {
        var loggedInId = _sessionService.GetUserId(context);
        if (loggedInId > 0)
        {
            return loggedInId;
        }

        var email = guestInfo.Email.Trim();
        var existing = await _unitOfWork.Customers.GetCustomerByEmailAsync(email);

        if (existing != null)
        {
            if (!IsGuestPassword(existing.PasswordHash))
            {
                throw new InvalidOperationException(
                    "Email này đã được đăng ký tài khoản. Vui lòng đăng nhập để tiếp tục thanh toán.");
            }

            existing.FullName = guestInfo.FullName.Trim();
            existing.Phone = guestInfo.Phone.Trim();
            existing.Address = guestInfo.Address.Trim();
            await _unitOfWork.Customers.UpdateCustomerViaProcedureAsync(existing, null);
            return existing.CustomerId;
        }

        var guest = new Customer
        {
            FullName = guestInfo.FullName.Trim(),
            Email = email,
            Phone = guestInfo.Phone.Trim(),
            Address = guestInfo.Address.Trim(),
            PasswordHash = GuestPasswordPrefix + Guid.NewGuid().ToString("N"),
            CreatedAt = DateTime.Now
        };

        await _unitOfWork.Customers.CreateCustomerViaProcedureAsync(guest);

        var created = await _unitOfWork.Customers.GetCustomerByEmailAsync(email)
            ?? throw new InvalidOperationException("Không thể tạo thông tin khách vãng lai.");

        return created.CustomerId;
    }

    public bool IsGuestPassword(string? passwordHash)
    {
        return !string.IsNullOrEmpty(passwordHash)
            && passwordHash.StartsWith(GuestPasswordPrefix, StringComparison.Ordinal);
    }
}
