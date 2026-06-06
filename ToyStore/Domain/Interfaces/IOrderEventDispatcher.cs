using ToyStore.Domain.Events;

namespace ToyStore.Domain.Interfaces;

/// <summary>
/// Dispatcher trung tâm cho các Order Events (Observer / Domain Events Pattern).
/// </summary>
public interface IOrderEventDispatcher
{
    Task PublishAsync<TEvent>(TEvent orderEvent) where TEvent : IOrderEvent;
}

