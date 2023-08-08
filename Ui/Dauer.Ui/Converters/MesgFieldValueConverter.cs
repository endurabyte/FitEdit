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
    _ when value is MessageWrapper mesg && parameter is string fieldName => $"{mesg.GetValue(fieldName, Prettify)}",
    _ => BindingOperations.DoNothing,
  };

  public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
  {
    if (value is not MessageWrapper mesg || parameter is not (string s, object o)) { return BindingOperations.DoNothing; }

    mesg.SetValue(s, o, Prettify);
    return mesg;
  }
}