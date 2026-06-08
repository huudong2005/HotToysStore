using Microsoft.AspNetCore.Mvc;
using ToyStore.Domain.Interfaces;
using ToyStore.Helpers;

namespace ToyStore.ViewComponents;

/// <summary>
/// Hiển thị huy hiệu hạng thành viên trên thanh tài khoản (dropdown).
/// Chỉ hiển thị khi người đăng nhập là Customer VÀ đã đạt một hạng thẻ.
/// Tài khoản thường (chưa có hạng) sẽ không render gì.
/// </summary>
public class MembershipStatusViewComponent : ViewComponent
{
    private readonly IUnitOfWork _unitOfWork;

    public MembershipStatusViewComponent(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        // Lấy phiên đăng nhập hiện tại (an toàn null).
        var user = AuthHelper.GetCurrentUser(HttpContext);
        if (user == null || !string.Equals(user.UserType, "Customer", StringComparison.OrdinalIgnoreCase))
        {
            return View((Domain.Entities.MembershipTier?)null);
        }

        var customer = await _unitOfWork.Customers.GetCustomerWithTierAsync(user.UserId);

        // Tài khoản thường (chưa có hạng) -> không hiển thị.
        return View(customer?.Tier);
    }
}
