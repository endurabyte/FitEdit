namespace Dauer.Model;

public struct RetryConfig
{
  public static readonly TimeSpan DefaultInterval = TimeSpan.FromMilliseconds(100);
  public static readonly TimeSpan DefaultDuration = TimeSpan.FromSeconds(5);

  public TimeSpan Interval { get; set; }
  public TimeSpan Duration { get; set; }
  public CancellationToken CancellationToken { get; set; }
  public int RetryLimit { get; set; }
  public string Description { get; set; }
  public Action Callback { get; set; }

  public RetryConfig()
  {
    Interval = DefaultInterval;
    Duration = DefaultDuration;
    CancellationToken = default;
    RetryLimit = int.MaxValue;
    Description = null;
    Callback = null;
  }

  public static bool operator ==(RetryConfig lhs, RetryConfig rhs) => lhs.Equals(rhs);
  public static bool operator !=(RetryConfig lhs, RetryConfig rhs) => !(lhs == rhs);

  public override bool Equals(object obj) =>
    obj is RetryConfig config
      && Interval == config.Interval
      && Duration == config.Duration
      && CancellationToken == config.CancellationToken
      && RetryLimit == config.RetryLimit
      && Callback == config.Callback;

  public override int GetHashCode() => HashCode.Combine(Interval, Duration, CancellationToken, RetryLimit, Callback);

  public RetryConfig WithDescription(string description)
  {
    Description = description;
    return this;
  }
}
