using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ToyStore.Models;
using ToyStore.Attributes;
using ToyStore.Domain.Events;
using ToyStore.Domain.Interfaces;
using ToyStore.Domain.Entities;
using ToyStore.Infrastructure.Data;

namespace ToyStore.Controllers
{
    [AuthorizeRole("Admin", "Staff")]
    public class OrdersController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IOrderEventDispatcher _orderEventDispatcher;

        public OrdersController(IUnitOfWork unitOfWork, IOrderEventDispatcher orderEventDispatcher)
        {
            _unitOfWork = unitOfWork;
            _orderEventDispatcher = orderEventDispatcher;
        }

        // GET: Orders
        public async Task<IActionResult> Index(string searchCustomer, string searchDate)
        {
            var ordersQuery = _unitOfWork.Orders.Query()
                .Include(o => o.Customer)
                .AsQueryable();

            // Filter by customer name if provided
            if (!string.IsNullOrEmpty(searchCustomer))
            {
                ordersQuery = ordersQuery.Where(o => o.Customer.FullName.Contains(searchCustomer));
            }

            // Filter by date if provided
            if (!string.IsNullOrEmpty(searchDate))
            {
                if (DateTime.TryParse(searchDate, out DateTime parsedDate))
                {
                    ordersQuery = ordersQuery.Where(o => o.OrderDate.HasValue && 
                        o.OrderDate.Value.Date == parsedDate.Date);
                }
            }

            var orders = await ordersQuery
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            ViewBag.SearchCustomer = searchCustomer;
            ViewBag.SearchDate = searchDate;
            
            return View(orders);
        }

        // GET: Orders/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _unitOfWork.Orders.GetOrderWithDetailsAsync(id.Value);
            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        // GET: Orders/Create
        public async Task<IActionResult> Create()
        {
            var customers = await _unitOfWork.Customers.GetAllAsync();
            ViewData["CustomerId"] = new SelectList(customers, "CustomerId", "CustomerId");
            return View();
        }

        // POST: Orders/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("OrderId,CustomerId,OrderDate,TotalAmount,Status,PaymentMethod,DeliveryMethod")] Order order)
        {
            // Tránh ModelState false do navigation properties không submit từ form
            ModelState.Remove("Customer");
            ModelState.Remove("OrderDetails");

            if (ModelState.IsValid)
            {
                try
                {
                    // Đảm bảo Status mặc định là Pending
                    if (string.IsNullOrEmpty(order.Status))
                    {
                        order.Status = "Pending";
                    }

                    // Tự động lấy giờ hiện tại nếu chưa có
                    if (!order.OrderDate.HasValue)
                    {
                        order.OrderDate = DateTime.Now;
                    }

                    await _unitOfWork.Orders.AddAsync(order);
                    await _unitOfWork.SaveChangesAsync();

                    TempData["SuccessMessage"] = $"Tạo đơn hàng #{order.OrderId} thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "Lỗi khi tạo đơn hàng: " + ex.Message;
                }
            }

            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage) ? "Dữ liệu không hợp lệ." : e.ErrorMessage)
                .ToList();

            if (errors.Count > 0)
            {
                TempData["ErrorMessage"] = "Không thể tạo đơn hàng: " + string.Join(" | ", errors);
            }

            var customers = await _unitOfWork.Customers.GetAllAsync();
            ViewData["CustomerId"] = new SelectList(customers, "CustomerId", "CustomerId", order.CustomerId);
            return View(order);
        }

        // GET: Orders/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _unitOfWork.Orders.GetByIdAsync(id.Value);
            if (order == null)
            {
                return NotFound();
            }
            var customers = await _unitOfWork.Customers.GetAllAsync();
            ViewData["CustomerId"] = new SelectList(customers, "CustomerId", "CustomerId", order.CustomerId);
            return View(order);
        }

        // POST: Orders/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("OrderId,CustomerId,OrderDate,TotalAmount,Status,PaymentMethod,DeliveryMethod")] Order order)
        {
            if (id != order.OrderId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingOrder = await _unitOfWork.Orders.GetByIdAsync(id);
                    if (existingOrder == null)
                    {
                        return NotFound();
                    }

                    // Cập nhật các thuộc tính (không thay đổi Status trực tiếp, sử dụng State Pattern)
                    existingOrder.CustomerId = order.CustomerId;
                    existingOrder.OrderDate = order.OrderDate;
                    existingOrder.TotalAmount = order.TotalAmount;
                    existingOrder.PaymentMethod = order.PaymentMethod;
                    existingOrder.DeliveryMethod = order.DeliveryMethod;
                    
                    // Status sẽ được quản lý bởi State Pattern, không set trực tiếp ở đây
                    // Nếu cần thay đổi Status, sử dụng các action riêng (Confirm, Ship, Cancel)

                    _unitOfWork.Orders.Update(existingOrder);
                    await _unitOfWork.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await OrderExists(order.OrderId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            var customers = await _unitOfWork.Customers.GetAllAsync();
            ViewData["CustomerId"] = new SelectList(customers, "CustomerId", "CustomerId", order.CustomerId);
            return View(order);
        }

        // GET: Orders/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _unitOfWork.Orders.GetOrderWithDetailsAsync(id.Value);
            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        // SỬA HÀM XÓA ĐƠN HÀNG
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                int resultCode = await _unitOfWork.Orders.DeleteOrderViaProcedureAsync(id);

                if (resultCode == 1)
                {
                    TempData["SuccessMessage"] = $"Xóa thành công đơn hàng #{id} và các chi tiết liên quan!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Lỗi khi xóa đơn hàng. Đơn hàng không tồn tại hoặc có lỗi cơ sở dữ liệu.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi: " + ex.Message;
            }
            return RedirectToAction("Index");
        }

        // SỬA HÀM XÁC NHẬN VÀ GIAO HÀNG (Làm tương tự cho Ship)
        [HttpPost]
        public async Task<IActionResult> Confirm(int id)
        {
            try
            {
                var order = await _unitOfWork.Orders.GetByIdAsync(id);
                if (order == null) return NotFound();

                if (!order.CanConfirm())
                {
                    TempData["ErrorMessage"] = $"Không thể xác nhận đơn hàng ở trạng thái {order.GetState().StateName}.";
                    return RedirectToAction("Index");
                }

                order.Confirm(); // Thay đổi state nội bộ

                // Gọi Stored Procedure thay vì Entity Framework Update
                await _unitOfWork.Orders.UpdateOrderStatusViaProcedureAsync(id, order.Status);

                // Kích hoạt Event
                await _orderEventDispatcher.PublishAsync(new OrderConfirmedEvent(order));
                TempData["SuccessMessage"] = $"Đã xác nhận đơn hàng #{order.OrderId} thành công!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        // POST: Orders/Ship/5
        [HttpPost]
        public async Task<IActionResult> Ship(int id)
        {
            try
            {
                var order = await _unitOfWork.Orders.GetByIdAsync(id);
                if (order == null)
                {
                    TempData["ErrorMessage"] = "Đơn hàng không tồn tại";
                    return RedirectToAction("Index");
                }

                // Sử dụng State Pattern để giao hàng
                if (!order.CanShip())
                {
                    var stateName = order.GetState().StateName;
                    TempData["ErrorMessage"] = $"Không thể giao hàng ở trạng thái {stateName}. Chỉ có thể giao hàng khi đơn hàng đã được xác nhận (Confirmed).";
                    return RedirectToAction("Index");
                }

                // Sử dụng State Pattern để giao hàng
                order.Ship();
                _unitOfWork.Orders.Update(order);
                await _unitOfWork.SaveChangesAsync();

                // Observer / Domain Events: publish sự kiện OrderShippedEvent
                await _orderEventDispatcher.PublishAsync(new OrderShippedEvent(order));

                TempData["SuccessMessage"] = $"Đã giao đơn hàng #{order.OrderId} thành công!";
                return RedirectToAction("Index");
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        // POST: Orders/Cancel/5
        // SỬA HÀM HỦY ĐƠN HÀNG (ĐÂY LÀ PHẦN LỢI HẠI NHẤT)
        [HttpPost]
        public async Task<IActionResult> Cancel(int id)
        {
            try
            {
                var order = await _unitOfWork.Orders.GetOrderWithDetailsAsync(id);
                if (order == null) return NotFound();

                if (!order.CanCancel())
                {
                    TempData["ErrorMessage"] = $"Không thể hủy đơn hàng ở trạng thái {order.GetState().StateName}.";
                    return RedirectToAction("Index");
                }

                order.Cancel(); // Thay đổi state nội bộ sang 'Cancelled'

                // 1 Dòng duy nhất thay cho cả khối Transaction và vòng lặp foreach cũ!
                int resultCode = await _unitOfWork.Orders.CancelOrderAndRestoreStockViaProcedureAsync(id, order.Status);

                if (resultCode == 1)
                {
                    await _orderEventDispatcher.PublishAsync(new OrderCancelledEvent(order));
                    TempData["SuccessMessage"] = $"Đã hủy đơn hàng #{order.OrderId} và hoàn trả tồn kho thành công!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Lỗi Database khi hủy đơn hàng và hoàn trả tồn kho.";
                }
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        private async Task<bool> OrderExists(int id)
        {
            var order = await _unitOfWork.Orders.GetByIdAsync(id);
            return order != null;
        }
    }
}
