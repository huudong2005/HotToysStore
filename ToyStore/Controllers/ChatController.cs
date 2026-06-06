using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using ToyStore.Domain.Interfaces;
using ToyStore.Models;

namespace ToyStore.Controllers
{
    // Chatbot tư vấn tự động (ToyStore Assistant) - public, khách vãng lai cũng dùng được
    public class ChatController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public ChatController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        // POST: /Chat/SendMessage
        [HttpPost]
        public async Task<IActionResult> SendMessage([FromBody] ChatRequest request)
        {
            try
            {
                var message = request?.Message?.Trim();

                if (string.IsNullOrWhiteSpace(message))
                {
                    return Json(new { response = "Bạn vui lòng nhập nội dung cần hỏi nhé! 😊" });
                }

                var lower = message.ToLowerInvariant();

                // Nhóm 1: Khách hỏi về sản phẩm / giá / tìm / mua
                if (ContainsAny(lower, "giá", "tìm", "sản phẩm", "mua", "còn hàng", "đồ chơi", "bao nhiêu"))
                {
                    var keyword = ExtractKeyword(message);
                    var botReply = await BuildProductReplyAsync(keyword);
                    return Json(new { response = botReply });
                }

                // Nhóm 2: Câu trả lời tĩnh (hardcode)
                string staticReply;

                if (ContainsAny(lower, "chào", "hello", "hi", "xin chào"))
                {
                    staticReply = "Xin chào! 👋 Tôi là <b>ToyStore Assistant</b>. Bạn cần tìm đồ chơi gì, hoặc hỏi giá sản phẩm nào ạ?";
                }
                else if (ContainsAny(lower, "địa chỉ", "ở đâu", "cửa hàng", "shop"))
                {
                    staticReply = "🏬 Cửa hàng ToyStore tọa lạc tại <b>123 Đường Đồ Chơi, Quận 1, TP.HCM</b>. Rất hân hạnh được đón tiếp bạn!";
                }
                else if (ContainsAny(lower, "giờ", "mở cửa", "thời gian"))
                {
                    staticReply = "🕘 ToyStore mở cửa <b>08:00 - 21:00</b> tất cả các ngày trong tuần (kể cả lễ, Tết).";
                }
                else if (ContainsAny(lower, "liên hệ", "hotline", "điện thoại", "sđt", "số điện thoại", "email"))
                {
                    staticReply = "📞 Hotline: <b>0123 456 789</b><br>📧 Email: <b>support@toystore.com</b>";
                }
                else if (ContainsAny(lower, "giao hàng", "ship", "vận chuyển", "thanh toán"))
                {
                    staticReply = "🚚 ToyStore giao hàng toàn quốc, hỗ trợ thanh toán khi nhận hàng (COD) và thanh toán online qua VnPay.";
                }
                else if (ContainsAny(lower, "cảm ơn", "thank", "cám ơn"))
                {
                    staticReply = "Rất vui được hỗ trợ bạn! ❤️ Chúc bạn mua sắm vui vẻ tại ToyStore.";
                }
                else
                {
                    staticReply = "Xin lỗi, tôi chưa hiểu rõ câu hỏi của bạn. 🤔<br>" +
                                  "Bạn có thể hỏi về <b>giá sản phẩm</b>, <b>tìm đồ chơi</b>, <b>địa chỉ</b> hoặc <b>giờ mở cửa</b> nhé!";
                }

                return Json(new { response = staticReply });
            }
            catch (Exception)
            {
                // Không lộ chi tiết lỗi ra ngoài cho khách hàng
                return Json(new { response = "Xin lỗi, hệ thống đang bận. Bạn vui lòng thử lại sau ít phút nhé! 🙏" });
            }
        }

        // Gọi Stored Procedure tìm kiếm sản phẩm theo từ khóa và dựng câu trả lời HTML
        private async Task<string> BuildProductReplyAsync(string keyword)
        {
            var products = await _unitOfWork.Products
                .FilterProductsViaProcedureAsync(keyword, null, null, null);

            var matched = products?.Take(5).ToList();

            if (matched == null || matched.Count == 0)
            {
                return string.IsNullOrWhiteSpace(keyword)
                    ? "Bạn muốn tìm sản phẩm nào ạ? Hãy cho tôi biết tên đồ chơi bạn quan tâm nhé! 🧸"
                    : $"Rất tiếc, tôi không tìm thấy sản phẩm nào khớp với \"<b>{keyword}</b>\". 😢<br>Bạn thử từ khóa khác xem sao nhé!";
            }

            var culture = new CultureInfo("vi-VN");
            var sb = new StringBuilder();
            sb.Append("Tôi tìm thấy một vài sản phẩm phù hợp cho bạn: 🎁<ul style=\"margin:6px 0;padding-left:18px;\">");

            foreach (var p in matched)
            {
                sb.Append("<li><b>")
                  .Append(System.Net.WebUtility.HtmlEncode(p.ProductName))
                  .Append("</b> — ")
                  .Append(p.Price.ToString("#,##0", culture))
                  .Append(" ₫</li>");
            }

            sb.Append("</ul>Bạn cần tôi tư vấn thêm về sản phẩm nào không ạ?");
            return sb.ToString();
        }

        private static bool ContainsAny(string source, params string[] keywords)
        {
            foreach (var k in keywords)
            {
                if (source.Contains(k, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        // Loại bỏ các từ "lệnh" để lấy phần tên sản phẩm khách muốn tìm
        private static string ExtractKeyword(string message)
        {
            string[] stopWords =
            {
                "giá", "tìm", "sản phẩm", "mua", "còn hàng", "bao nhiêu",
                "cho tôi", "của", "đồ chơi", "bao", "nhiêu", "tiền", "ạ", "?"
            };

            var result = message;
            foreach (var w in stopWords)
            {
                result = result.Replace(w, " ", StringComparison.OrdinalIgnoreCase);
            }

            return result.Trim();
        }
    }
}
