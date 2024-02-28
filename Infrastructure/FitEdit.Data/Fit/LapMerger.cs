using Dynastream.Fit;
#nullable enable

namespace FitEdit.Data.Fit;

public class LapMerger
{
  /// <summary>
  /// Merge the given laps into one lap.
  /// </summary>
  public LapMesg? Merge(List<LapMesg> toMerge) 
  {
    LapMesg? first = toMerge.FirstOrDefault();
    LapMesg? last = toMerge.LastOrDefault();

    if (first == null) { return null; }
    if (last == null) { return null; }

    var agg = new LapAggregator(toMerge);

    float? totalDistance = agg.GetSum(l => l.GetTotalDistance());
    uint? totalCycles = agg.GetSum(l => l.GetTotalCycles());
    ushort? totalCalories = agg.GetSum(l => l.GetTotalCalories());
    byte? avgHeartRate = agg.GetWeightedAvg(l => l.GetAvgHeartRate());
    byte? maxHeartRate = agg.GetMax(l => l.GetMaxHeartRate());
    byte? avgCadence = agg.GetWeightedAvg(l => l.GetAvgCadence());
    byte? maxCadence = agg.GetMax(l => l.GetMaxCadence());
    ushort? avgPower = agg.GetWeightedAvg(l => l.GetAvgPower());
    ushort? maxPower = agg.GetMax(l => l.GetMaxPower());
    ushort? normalizedPower = agg.GetWeightedAvg(l => l.GetNormalizedPower());
    var totalWork = agg.GetSum(l => l.GetTotalWork());

    var avgVerticalOscillation = agg.GetWeightedAvg(l => l.GetAvgVerticalOscillation());
    var avgVerticalRatio = agg.GetWeightedAvg(l => l.GetAvgVerticalRatio());
    var avgStepLength = agg.GetWeightedAvg(l => l.GetAvgStepLength());
    var avgStanceTime = agg.GetWeightedAvg(l => l.GetAvgStanceTime());
    var avgFractionalCadence = agg.GetWeightedAvg(l => l.GetAvgFractionalCadence());
    var maxFractionalCadence = agg.GetMax(l => l.GetMaxFractionalCadence());

    ushort? totalAscent = agg.GetSum(l => l.GetTotalAscent());
    ushort? totalDescent = agg.GetSum(l => l.GetTotalDescent());
    sbyte? avgTemperature = agg.GetWeightedAvg(l => l.GetAvgTemperature());
    sbyte? maxTemperature = agg.GetMax(l => l.GetMaxTemperature());
    sbyte? minTemperature = agg.GetMin(l => l.GetMinTemperature());

    float? avgLeftPowerPhase = agg.GetWeightedAvg(l => l.GetAvgLeftPowerPhase(0));
    float? avgLeftPowerPhasePeak = agg.GetWeightedAvg(l => l.GetAvgLeftPowerPhasePeak(0));
    float? avgRightPowerPhase = agg.GetWeightedAvg(l => l.GetAvgRightPowerPhase(0));
    float? avgRightPowerPhasePeak = agg.GetWeightedAvg(l => l.GetAvgRightPowerPhasePeak(0));
    ushort? avgPowerPosition = agg.GetWeightedAvg(l => l.GetAvgPowerPosition(0));
    ushort? maxPowerPosition = agg.GetMax(l => l.GetMaxPowerPosition(0));
    byte? avgCadencePosition = agg.GetWeightedAvg(l => l.GetAvgCadencePosition(0));
    byte? maxCadencePosition = agg.GetMax(l => l.GetMaxCadencePosition(0));
    float? enhancedAvgSpeed = agg.GetWeightedAvg(l => l.GetEnhancedAvgSpeed());
    float? enhancedMaxSpeed = agg.GetMax(l => l.GetEnhancedMaxSpeed());
    float? enhancedAvgRespirationRate = agg.GetWeightedAvg(l => l.GetEnhancedAvgRespirationRate());
    float? enhancedMaxRespirationRate = agg.GetMax(l => l.GetEnhancedMaxRespirationRate());
    float? enhancedAvgAltitude = agg.GetWeightedAvg(l => l.GetEnhancedAvgAltitude());
    float? enhancedMaxAltitude = agg.GetMax(l => l.GetEnhancedMaxAltitude());
    float? enhancedMinAltitude = agg.GetMin(l => l.GetEnhancedMinAltitude());
    float? avgVam = agg.GetWeightedAvg(l => l.GetAvgVam());

    // Swimming
    ushort? numLengths = agg.GetSum(l => l.GetNumLengths());
    ushort? firstLengthIndex = first.GetFirstLengthIndex();
    float? avgStrokeDistance = agg.GetWeightedAvg(l => l.GetAvgStrokeDistance());
    ushort? numActiveLengths = agg.GetSum(l => l.GetNumActiveLengths());

    var lap = new LapMesg();

    lap.SetMessageIndex(first.GetMessageIndex());
    lap.SetStartTime(first.GetStartTime());
    lap.SetTimestamp(last.GetStartTime());
    lap.SetEvent(first.GetEvent());
    lap.SetEventType(first.GetEventType());
    lap.SetStartPositionLat(first.GetStartPositionLat());
    lap.SetStartPositionLong(first.GetStartPositionLong());
    lap.SetEndPositionLat(last.GetEndPositionLat());
    lap.SetEndPositionLong(last.GetEndPositionLong());
    lap.SetTotalElapsedTime((float)(last.End() - first.Start()).TotalSeconds);
    lap.SetTotalTimerTime((float)(last.End() - first.Start()).TotalSeconds);
    lap.SetTotalDistance(totalDistance);
    lap.SetTotalCycles(totalCycles);
    lap.SetTotalCalories(totalCalories);
    lap.SetAvgHeartRate(avgHeartRate);
    lap.SetMaxHeartRate(maxHeartRate);
    lap.SetAvgCadence(avgCadence);
    lap.SetMaxCadence(maxCadence);
    lap.SetAvgPower(avgPower);
    lap.SetMaxPower(maxPower);
    lap.SetNormalizedPower(normalizedPower);
    lap.SetTotalWork(totalWork);
    lap.SetAvgVerticalOscillation(avgVerticalOscillation);
    lap.SetAvgVerticalRatio(avgVerticalRatio);
    lap.SetAvgStepLength(avgStepLength);
    lap.SetAvgStanceTime(avgStanceTime);
    lap.SetAvgFractionalCadence(avgFractionalCadence);
    lap.SetMaxFractionalCadence(maxFractionalCadence);
    lap.SetTotalAscent(totalAscent);
    lap.SetTotalDescent(totalDescent);
    lap.SetIntensity(first.GetIntensity());
    lap.SetLapTrigger(first.GetLapTrigger());
    lap.SetSport(first.GetSport());
    lap.SetSubSport(first.GetSubSport());
    lap.SetAvgTemperature(avgTemperature);
    lap.SetMaxTemperature(maxTemperature);
    lap.SetMinTemperature(minTemperature);

    if (avgLeftPowerPhase is not null)
    {
      lap.SetAvgLeftPowerPhase(0, avgLeftPowerPhase);
    }
    if (avgLeftPowerPhasePeak is not null)
    {
      lap.SetAvgLeftPowerPhasePeak(0, avgLeftPowerPhasePeak);
    }
    if (avgRightPowerPhase is not null)
    {
      lap.SetAvgRightPowerPhase(0, avgRightPowerPhase);
    }
    if (avgRightPowerPhasePeak is not null)
    {
      lap.SetAvgRightPowerPhasePeak(0, avgRightPowerPhasePeak);
    }
    if (avgPowerPosition is not null)
    {
      lap.SetAvgPowerPosition(0, avgPowerPosition);
    }
    if (maxPowerPosition is not null)
    {
      lap.SetMaxPowerPosition(0, maxPowerPosition);
    }
    if (avgCadencePosition is not null)
    {
      lap.SetAvgCadencePosition(0, avgCadencePosition);
    }
    if (maxCadencePosition is not null)
    {
      lap.SetMaxCadencePosition(0, maxCadencePosition);
    }
    lap.SetEnhancedAvgSpeed(enhancedAvgSpeed);
    lap.SetEnhancedMaxSpeed(enhancedMaxSpeed);
    lap.SetEnhancedAvgRespirationRate(enhancedAvgRespirationRate);
    lap.SetEnhancedMaxRespirationRate(enhancedMaxRespirationRate);
    lap.SetEnhancedAvgAltitude(enhancedAvgAltitude);
    lap.SetEnhancedMinAltitude(enhancedMinAltitude);
    lap.SetEnhancedMaxAltitude(enhancedMaxAltitude);
    lap.SetMaxAltitude(enhancedMaxAltitude);
    lap.SetMinAltitude(enhancedMinAltitude);
    lap.SetAvgVam(avgVam);

    lap.SetNumLengths(numLengths);
    lap.SetFirstLengthIndex(firstLengthIndex);
    lap.SetAvgStrokeDistance(avgStrokeDistance);
    lap.SetNumActiveLengths(numActiveLengths);

    return lap;
  }
}
