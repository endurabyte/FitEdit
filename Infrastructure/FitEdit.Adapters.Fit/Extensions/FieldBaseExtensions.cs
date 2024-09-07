namespace FitEdit.Adapters.Fit.Extensions;

public static class FieldBaseExtensions
{
  public static T WithInvalidValue<T>(this T field) where T : Dynastream.Fit.FieldBase =>
    field.WithValue(FitTypes.GetInvalidValue(field.Type));

  public static T WithValue<T>(this T field, object value) where T : Dynastream.Fit.FieldBase
  {
    field.SetValue(value);
    return field;
  }
}