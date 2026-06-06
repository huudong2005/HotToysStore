using ToyStore.Domain.Entities;

namespace ToyStore.Domain.Events;

public class OrderConfirmedEvent : IOrderEvent
{
    public Order Order { get; }

    public OrderConfirmedEvent(Order order)
    {
        Order = order;
    }
}

public class OrderShippedEvent : IOrderEvent
{
    public Order Order { get; }

    public OrderShippedEvent(Order order)
    {
        Order = order;
    }
}

public class OrderCancelledEvent : IOrderEvent
{
    public Order Order { get; }

    public OrderCancelledEvent(Order order)
    {
        Order = order;
    }
}

