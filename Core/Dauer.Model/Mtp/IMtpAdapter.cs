#nullable enable

using System.Reactive.Subjects;

namespace Dauer.Model.Mtp;

public interface IMtpAdapter
{
  IObservable<LocalActivity> ActivityFound { get; }
  void Scan();
}

public class NullMtpAdapter : IMtpAdapter
{
  public IObservable<LocalActivity> ActivityFound => new Subject<LocalActivity>();
  public void Scan() { }
}