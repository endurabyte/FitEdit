namespace FitEdit.Adapters.Fit;

public static class FitTypes
{
  static FitTypes()
  {
    TypeMap = Dynastream.Fit.Fit.BaseType.ToDictionary(bt => bt.baseTypeField, bt => bt);
    TypeIndexMap = Dynastream.Fit.Fit.BaseType
      .Select((bt, i) => new { bt, i })
      .ToDictionary(pair => (byte)pair.i, pair => pair.bt);
  }

  /// <summary>
  /// Maps base type field to FitType
  /// </summary>
  public static Dictionary<byte, Dynastream.Fit.Fit.FitType> TypeMap { get; set; }

  /// <summary>
  /// Maps index into the BaseTypes array to FitType
  /// </summary>
  public static Dictionary<byte, Dynastream.Fit.Fit.FitType> TypeIndexMap { get; set; }
}