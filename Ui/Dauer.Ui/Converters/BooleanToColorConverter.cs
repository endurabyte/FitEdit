using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Dauer.Ui.Converters;

public class BooleanToColorConverter : IValueConverter
{
  public ISolidColorBrush TrueColor { get; set; } = new SolidColorBrush(Colors.Green);
  public ISolidColorBrush FalseColor { get; set; } = new SolidColorBrush(Colors.Red);
  
  public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
  {
    true => TrueColor,
    false => FalseColor,
    _ => AvaloniaProperty.UnsetValue
  };

  public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}