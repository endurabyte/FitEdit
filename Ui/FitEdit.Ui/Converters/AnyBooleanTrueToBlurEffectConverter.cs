using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using FitEdit.Ui.Extensions;

namespace FitEdit.Ui.Converters;

/// <summary>
/// Return a blur effect if any passed value is true
/// </summary>
public class AnyBooleanTrueToBlurEffectConverter : IMultiValueConverter 
{
  public double Radius { get; set; } = 10;

  public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture) => 
    values.Any(o => o is bool b && b) switch
    {
      true => new BlurEffect().WithRadius(Radius),
      _ => AvaloniaProperty.UnsetValue
    };

  public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}
