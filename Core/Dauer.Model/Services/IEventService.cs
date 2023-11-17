namespace Dauer.Model.Services
{
  /// <summary>
  /// <see cref="EventService"/> is an <b>event aggregator</b>, implementing an observer-like design pattern to simplify event propagation.
  /// As a layer of indirection, publishers and subscribers register through it instead of with each other.
  /// See <a href="https://martinfowler.com/eaaDev/EventAggregator.html"/>
  /// 
  /// <para/>
  /// In Domain-Driven Design and onion architecture we often find strict layering. For example, in Motion we have 
  /// adapters (which abstract away dependencies)
  /// that are used by domain services (which implement business logic)
  /// that are used by API layer (which provides an interface and data objects (DTOs) sensible to clients).
  /// 
  /// <para/>
  /// Without an event aggregator, propagating an event from a lower layer (say, an adapter) to a high layer (say, the API) 
  /// requires intermediate event subscriptions and handlers which exist only for the purpose of passing the event to a higher layer. Example:
  /// 
  /// <code>
  /// [Adapter] => [ServiceA] => [ServiceB] => [API]
  /// </code>
  /// 
  /// An event aggregator propagates events directly instead of through a subscription chain.
  /// This eliminates boilerplate event code in the middle layers:
  /// 
  /// <code>
  /// [Adapter] => [aggregator] => [API]
  /// </code>
  /// </summary>
  public interface IEventService
  {
    /// <summary>
    /// Publish an event. All subscribed handlers with a matching key will receive the given object.
    /// </summary>
    void Publish(object key, object value);

    /// <summary>
    /// Subscribe to events. Subsequent calls to <see cref="Publish"/>
    /// with a matching key will invoke the given handler with the published object.
    ///  
    /// <para/>
    /// Call <see cref="IDisposable.Dispose()"/> to unsubscribe
    /// </summary>
    IDisposable Subscribe<T>(object key, Action<T> handler);
  }
}
