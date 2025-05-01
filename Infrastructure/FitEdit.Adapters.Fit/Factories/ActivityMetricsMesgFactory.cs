using Dynastream.Fit;

namespace FitEdit.Adapters.Fit.Factories;

internal static class ActivityMetricsMesgFactory
{
  /// <summary>
  /// Builds the (undocumented) ActivityMetrics message as seen on newer Garmin devices.
  /// </summary>
  internal static Mesg Create()
  {
    var m = new Mesg("ActivityMetrics", MesgNum.ActivityMetrics);

    Add("NewHrMax",                  1, FitBaseType.Uint8z,    1,         "",           Profile.Type.Uint8z);
    Add("AerobicTrainingEffect",     4, FitBaseType.Uint8,    10,         "",           Profile.Type.Uint8);
    Add("Vo2Max",                    7, FitBaseType.Uint32z,   18724.571428571428, "ml/kg/min", Profile.Type.Uint32z);
    Add("RecoveryTime",              9, FitBaseType.Uint16,    1,        "min",        Profile.Type.Uint16);
    Add("Lthr",                     14, FitBaseType.Uint8,     1,        "bpm",        Profile.Type.Uint8);
    Add("LtPower",                  15, FitBaseType.Uint8,     1,       "watts",       Profile.Type.Uint8);
    Add("LtSpeed",                  16, FitBaseType.Uint8,    10,       "km/h",        Profile.Type.Uint8);
    Add("FinalPerformanceCondition",17, FitBaseType.Uint8,     1,         "",           Profile.Type.Uint8);
    Add("AnaerobicTrainingEffect",  20, FitBaseType.Uint8,    10,         "",           Profile.Type.Uint8);
    Add("FinalBodyBattery",         25, FitBaseType.Uint8,     1,         "",           Profile.Type.Uint8);
    Add("FirstVo2Max",              29, FitBaseType.Uint32z,   18724.571428571428, "ml/kg/min", Profile.Type.Uint32z);
    Add("Time",                     35, FitBaseType.Uint32, 1000,        "s",          Profile.Type.Uint32);
    Add("Distance",                 36, FitBaseType.Uint32,  100,        "m",          Profile.Type.Uint32);
    Add("TrainingLoadPeak1",        37, FitBaseType.Sint32, 65536,        "",           Profile.Type.Sint32);
    Add("TrainingLoadPeak2",        39, FitBaseType.Sint32, 65536,        "",           Profile.Type.Sint32);
    Add("PrimaryBenefit",           41, FitBaseType.Enum,      1,         "",           Profile.Type.PrimaryBenefit);
    Add("LocalTimestamp",           48, FitBaseType.Uint32,    1,        "s",          Profile.Type.LocalDateTime);
    Add("EndingPotential",          50, FitBaseType.Uint8,     1,         "",           Profile.Type.Uint8);
    Add("TotalAscent",              60, FitBaseType.Uint16,    1,        "m",          Profile.Type.Uint16);
    Add("TotalDescent",             61, FitBaseType.Uint16,    1,        "m",          Profile.Type.Uint16);
    Add("AveragePower",             62, FitBaseType.Uint16,    1,       "watts",       Profile.Type.Uint16);
    Add("AverageHeartrate",         63, FitBaseType.Uint8,     1,        "bpm",        Profile.Type.Uint8);

    return m;

    void Add(string name, byte num, byte baseType, double scale, string units, Profile.Type semantic)
      => m.SetField(new Field(name, num, baseType, scale, 0, units, false, semantic));
  }
}