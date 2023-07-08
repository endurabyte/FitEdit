using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace Dauer.Ui.Converters;

public class BooleanToLoginStatusConverter : IValueConverter
{
  public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
  {
    true => "You are logged in",
    false => "You are not logged in",
    _ => AvaloniaProperty.UnsetValue
  };

  public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}
