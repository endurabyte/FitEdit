using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Dauer.Data.Fit;

namespace Dauer.Ui.Converters;

public class MesgFieldValueConverter : IValueConverter
{
  public bool Prettify { get; set; }

  public MesgFieldValueConverter(bool prettify)
  {
    Prettify = prettify;
  }

  public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
  {
    _ when value is Message mesg && parameter is string fieldName => $"{mesg.GetValue(fieldName, Prettify)}",
    _ => BindingOperations.DoNothing,
  };

  public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}