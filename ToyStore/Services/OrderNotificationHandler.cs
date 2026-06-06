using Microsoft.Extensions.Logging;
using ToyStore.Domain.Events;
using ToyStore.Domain.Interfaces;

namespace ToyStore.Services;

/// <summary>
/// Ví dụ về Observer: lắng nghe các sự kiện Order và ghi log.
/// </summary>
public class OrderNotificationHandler :
    IOrderEventHandler<OrderConfirmedEvent>,
    IOrderEventHandler<OrderShippedEvent>,
    IOrderEventHandler<OrderCancelledEvent>
{
    private readonly ILogger<OrderNotificationHandler> _logger;

    public OrderNotificationHandler(ILogger<OrderNotificationHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(OrderConfirmedEvent orderEvent)
    {
        _logger.LogInformation("Order {OrderId} đã được xác nhận.", orderEvent.Order.OrderId);
        return Task.CompletedTask;
    }

    public Task HandleAsync(OrderShippedEvent orderEvent)
    {
        _logger.LogInformation("Order {OrderId} đã được giao.", orderEvent.Order.OrderId);
        return Task.CompletedTask;
    }

    public Task HandleAsync(OrderCancelledEvent orderEvent)
    {
        _logger.LogInformation("Order {OrderId} đã bị hủy.", orderEvent.Order.OrderId);
        return Task.CompletedTask;
    }
}

