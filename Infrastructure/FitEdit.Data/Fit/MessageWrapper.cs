#nullable enable
using FitEdit.Model;
using Dynastream.Fit;

namespace FitEdit.Data.Fit;

public class MessageWrapper(Mesg mesg) : HasProperties
{
  public Mesg Mesg { get; set; } = mesg;
  public bool IsNamed => Mesg.Name != "unknown";

  public void SetFieldValue(string name, object? value, bool pretty)
  {
    Mesg.SetFieldValue(name, value, pretty);
    NotifyPropertyChanged(nameof(Mesg));
  }

  public object? GetFieldValue(string name, bool prettify) => Mesg.GetFieldValue(name, prettify);
}