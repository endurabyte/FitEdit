using Dynastream.Fit;

namespace FitEdit.Adapters.Fit.Extensions;

public static class FieldFactory
{
  public static Field FromType(byte num, byte typeIndex) =>
    new Field(num, FitTypes.TypeIndexMap[typeIndex].baseTypeField).WithInvalidValue();
}
