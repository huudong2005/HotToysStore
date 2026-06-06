using Microsoft.Extensions.DependencyInjection;
using ToyStore.Domain.Events;
using ToyStore.Domain.Interfaces;

namespace ToyStore.Infrastructure.Events;

/// <summary>
/// Triển khai IOrderEventDispatcher sử dụng DI container để gọi tất cả các IOrderEventHandler đã đăng ký.
/// </summary>
public class OrderEventDispatcher : IOrderEventDispatcher
{
    private readonly IServiceProvider _serviceProvider;

    public OrderEventDispatcher(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task PublishAsync<TEvent>(TEvent orderEvent) where TEvent : IOrderEvent
    {
        using var scope = _serviceProvider.CreateScope();
        var handlers = scope.ServiceProvider.GetServices<IOrderEventHandler<TEvent>>();

        foreach (var handler in handlers)
        {
            await handler.HandleAsync(orderEvent);
        }
    }
}

