using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace FitEdit.Ui.Converters;

public class BooleanToStringConverter : IValueConverter
{
  public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
  {
    true => "OK",
    false => parameter is string s ? s : "Not OK",
    _ => AvaloniaProperty.UnsetValue
  };

  public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}
