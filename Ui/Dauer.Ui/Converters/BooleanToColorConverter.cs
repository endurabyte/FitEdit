using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Dauer.Ui.Converters;

public class BooleanToColorConverter : IValueConverter
{
  public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
  {
    true => new SolidColorBrush(FitColor.GreenCrayon),
    false => new SolidColorBrush(FitColor.RedCrayon),
    _ => AvaloniaProperty.UnsetValue
  };

  public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}