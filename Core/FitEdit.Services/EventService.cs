using System.Collections.Concurrent;
using System.Reactive.Subjects;
using FitEdit.Model.Services;

namespace FitEdit.Services
{
  public class EventService : IEventService
  {
    private readonly ConcurrentDictionary<object, Subject<object>> subjects_ = new ConcurrentDictionary<object, Subject<object>>();

    public void Publish(object key, object value) => SubjectFor(key).OnNext(value);

    public IDisposable Subscribe<T>(object key, Action<T> handler) => SubjectFor(key).Subscribe(o => handler((T)o));

    private Subject<object> SubjectFor(object key) => subjects_.GetOrAdd(key, key => new Subject<object>());
  }
}
