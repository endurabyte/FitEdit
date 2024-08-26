using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;
using FitEdit.Data.Fit;

namespace FitEdit.Ui.Converters;

/// <summary>
/// Variant of <see cref="MessageWrapperFieldNameValueConverter"/>, 
/// but <see cref="MessageWrapper"/> is a constructor parameter instead of a converter parameter.
/// </summary>
public class SharedMessageWrapperFieldNameValueConverter(MessageWrapper mesg, bool prettify) : IValueConverter
{
  public bool Prettify { get; set; } = prettify;

  public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
  {
    _ when parameter is string fieldName => $"{mesg.GetValue(fieldName, Prettify)}",
    _ => BindingOperations.DoNothing,
  };

  public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
  {
    if (parameter is not (string s, object o)) { return BindingOperations.DoNothing; }

    mesg.SetFieldValue(s, o, Prettify);
    return mesg;
  }
}
