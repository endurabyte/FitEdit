using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Dynastream.Fit;
using FitEdit.Data.Fit;

namespace FitEdit.Ui.Converters;

/// <summary>
/// Given a <see cref="Mesg"/> and a field name, get or set the value of the field.
/// </summary>
public class MesgFieldValueConverter(bool prettify) : IValueConverter
{
  public bool Prettify { get; set; } = prettify;

  public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
  {
    _ when value is Mesg mesg && parameter is string fieldName => $"{mesg.GetFieldValue(fieldName, Prettify)}",
    _ => BindingOperations.DoNothing,
  };

  public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => BindingOperations.DoNothing;
}