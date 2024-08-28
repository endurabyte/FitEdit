using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;
using FitEdit.Data.Fit;

namespace FitEdit.Ui.Converters;

public class MessageWrapperFieldValueConverter(MessageWrapper mesg, bool prettify) : IValueConverter
{
  public bool Prettify { get; set; } = prettify;

  public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
  {
    _ when parameter is string fieldName => $"{mesg.GetFieldValue(fieldName, Prettify)}",
    _ => BindingOperations.DoNothing,
  };

  public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => BindingOperations.DoNothing;
}
