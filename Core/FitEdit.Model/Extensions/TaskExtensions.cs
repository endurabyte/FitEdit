namespace FitEdit.Model.Extensions;

public static class TaskExtensions
{
  /// <summary>
  /// Shorthand for GetAwait().GetResult().
  /// Use when use of the await keyword isn't possible
  /// </summary>
  public static void Await(this Task t) => t.GetAwaiter().GetResult();

  /// <summary>
  /// Shorthand for GetAwait().GetResult().
  /// Use when use of the await keyword isn't possible
  /// </summary>
  public static T Await<T>(this Task<T> t) => t.GetAwaiter().GetResult();

  /// <summary>
  /// Shorthand for ConfigureAwait(false)
  /// </summary>
  public static Task AnyContext(this Task t) => t.WithContext(AsyncContext.Any);

  public static Task<T> AsTask<T>(this T t) => Task.FromResult(t);

  /// <summary>
  /// Shorthand for ConfigureAwait(false)
  /// </summary>
  public static Task<T> AnyContext<T>(this Task<T> t) => t.WithContext(AsyncContext.Any);

  public static Task WithContext(this Task t, AsyncContext context)
  {
    t.ConfigureAwait(continueOnCapturedContext: context == AsyncContext.Captured);
    return t;
  }

  public static Task<T> WithContext<T>(this Task<T> t, AsyncContext context)
  {
    t.ConfigureAwait(continueOnCapturedContext: context == AsyncContext.Captured);
    return t;
  }
}