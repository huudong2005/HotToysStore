using ToyStore.Domain.Events;

namespace ToyStore.Domain.Interfaces;

/// <summary>
/// Observer Pattern: Handler cho các sự kiện Order cụ thể.
/// </summary>
/// <typeparam name="TEvent">Kiểu sự kiện</typeparam>
public interface IOrderEventHandler<in TEvent> where TEvent : IOrderEvent
{
    Task HandleAsync(TEvent orderEvent);
}

