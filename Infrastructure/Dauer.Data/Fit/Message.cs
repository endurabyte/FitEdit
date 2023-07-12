#nullable enable
using System.Text.RegularExpressions;
using Dauer.Model;
using Dynastream.Fit;

namespace Dauer.Data.Fit;

public partial class Message : HasProperties
{
  public Mesg Mesg { get; set; }
  public bool IsNamed => Mesg.Name != "unknown";

  public Message(Mesg mesg)
  {
    Mesg = mesg;
  }

  public void SetValue(string name, object? value)
  {
    Mesg.SetFieldValue(name, value);
    NotifyPropertyChanged(nameof(Mesg));
  }

  public object? GetValue(string name) => TryParseFieldNumber(name, out byte id) 
    ? Mesg.GetFieldValue(id) 
    : Mesg.GetFieldValue(name);

  /// <summary>
  /// Parse e.g. "Field 253" and return 253
  /// </summary>
  private static bool TryParseFieldNumber(string field, out byte id)
  {
    id = 0;
    var match = fieldRegex().Match(field);
    return match.Success && byte.TryParse(match.Value, out id);
  }

  [GeneratedRegex("\\d+$")]
  private static partial Regex fieldRegex();
}