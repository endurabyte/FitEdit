using System.Globalization;
using Avalonia.Data.Converters;
using Dauer.Model.Workouts;

namespace Dauer.Ui.Converters;

public class SpeedToStringValueConverter : IValueConverter
{
  public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
    value is Speed speed && parameter is bool fullPrecision
      ? (object)speed.ToString(fullPrecision)
      : throw new ArgumentException($"Unsupported value {value}");

  public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
    value is string s
      ? (object)new Speed(s)
      : throw new ArgumentException($"Cannot convert {value?.GetType()} to {targetType}");
}
