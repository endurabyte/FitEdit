using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;
using FitEdit.Data.Fit;

namespace FitEdit.Ui.Converters;

/// <summary>
/// Given a <see cref="MessageWrapper"/> and a field name, get or set the value of the field.
/// </summary>
public class MessageWrapperFieldNameValueConverter(bool prettify) : IValueConverter
{
  public bool Prettify { get; set; } = prettify;

  public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
  {
    _ when value is MessageWrapper mesg && parameter is string fieldName => $"{mesg.GetValue(fieldName, Prettify)}",
    _ => BindingOperations.DoNothing,
  };

  public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
  {
    if (value is not MessageWrapper mesg || parameter is not (string s, object o)) { return BindingOperations.DoNothing; }

    mesg.SetFieldValue(s, o, Prettify);
    return mesg;
  }
}