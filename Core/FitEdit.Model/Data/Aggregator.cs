using FitEdit.Model.Extensions;
#nullable enable

namespace FitEdit.Model.Data;

public abstract class Aggregator<TAgg>
{
  protected readonly IEnumerable<TAgg> toAggregate_;
  protected double totalWeight_;
  protected Func<TAgg, double> getOneWeight_;

  protected Aggregator(IEnumerable<TAgg> toAggregate, double totalWeight, Func<TAgg, double> getOneWeight)
  {
    toAggregate_ = toAggregate;
    totalWeight_ = totalWeight;
    getOneWeight_ = getOneWeight;
  }

  /// <summary>
  /// Return the sum of the values returned by the given func.
  /// </summary>
  public TResult? GetSum<TResult>(Func<TAgg, TResult> getValue) => apply(getValue, Enumerable.Sum);

  /// <summary>
  /// Return the max of the values returned by the given func.
  /// </summary>
  public TResult? GetMax<TResult>(Func<TAgg, TResult> getValue) => apply(getValue, Enumerable.Max);

  /// <summary>
  /// Return the min of the values returned by the given func.
  /// </summary>
  public TResult? GetMin<TResult>(Func<TAgg, TResult> getValue) => apply(getValue, Enumerable.Min);

  /// <summary>
  /// Return the average of the given value weighted by the contribution of the weight of this element to the total weight. 
  /// <para/>
  /// Example:
  /// Consider a 2-lap workout. The element weight is lap duration. The total weight is the duration of all laps.
  ///   lap 1 has avg hr 100, lap 2 has avg hr 200.
  ///   lap 1 lasts 9 minutes, lap 2 lasts 1 minute.
  ///   avg hr = 9/10 * 100 + 1/10 + 200 = 110
  /// </summary>
  public TResult? GetWeightedAvg<TResult>(Func<TAgg, TResult> getValue)
  {
    double? avg = toAggregate_.Select(l =>
    {
      double? weight = getOneWeight_(l) / totalWeight_;
      double? avgAsFloat = getValue(l).Cast<TResult, double>();
      return weight * avgAsFloat;
    }).Sum();

    return avg == null ? default : ((double)avg!).Cast<double, TResult>();
  }

  private TResult? apply<TResult>(Func<TAgg, TResult> getLapValue, Func<IEnumerable<double>, double> func) =>
    func(toAggregate_.Select(l => getLapValue(l)
      .Cast<TResult, double>()))
      .Cast<double, TResult>();
}
