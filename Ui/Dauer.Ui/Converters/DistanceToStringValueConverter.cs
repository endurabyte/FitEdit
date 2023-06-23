using System.Globalization;
using Avalonia.Data.Converters;
using Dauer.Model.Workouts;
using Units;

namespace Dauer.Ui.Converters;

public class DistanceToStringValueConverter : IValueConverter
{
  public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
  {
    if (value is not Distance d) { return $"{value}"; }

    bool isMetric = d.Unit.IsMetric();
    bool isSmall = d.Value < 1;
    bool isLarge = d.Value > 1000;

    return d.Unit switch
    {
      Unit.Mile when isSmall => $"{d.Convert(Unit.Meter)}",
      Unit.Kilometer when isSmall => $"{d.Convert(Unit.Meter)}",
      Unit.Meter when isMetric && isLarge => $"{d.Convert(Unit.Kilometer)}",
      Unit.Meter when !isMetric && isLarge => $"{d.Convert(Unit.Mile)}",
      _ => $"{d}",
    };
  }

  public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
  {
    throw new NotImplementedException();
  }
}
