using Dynastream.Fit;
using FitEdit.Model.Data;

namespace FitEdit.Data.Fit;

public class LapAggregator : Aggregator<LapMesg>
{
  public LapAggregator(List<LapMesg> toAggregate) : base(toAggregate, 0, GetWeight)
  {
    totalWeight_ = GetSum(l => (double)l.GetTotalElapsedTime()!);
  }

  private static double GetWeight(LapMesg l) => (double)l.GetTotalElapsedTime()!;
}
