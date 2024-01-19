using System.Globalization;
using Avalonia.Data.Converters;

namespace FitEdit.Ui.Converters;

public class TimeSpanToStringValueConverter : IValueConverter
{
  public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
  {
    if (value is not TimeSpan ts) { return $"{value}"; }

    return (ts.Hours > 0 ? $"{ts:hh}h " : "")
      + (ts.Minutes > 0 ? $"{ts.Minutes}min " : "")
      + $"{ts.Seconds}s";
  }

  public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
  {
    if (value is not string s) { throw new ArgumentException($"{value} must be a string"); }
    return TimeSpan.Parse(s);
  }
}
