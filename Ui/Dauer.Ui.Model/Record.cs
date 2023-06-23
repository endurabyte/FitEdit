using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace Dauer.Ui.Model;

public class Record : ReactiveObject
{
  [Reactive] public int Index { get; set; }
  [Reactive] public int MessageNum { get; set; }
  [Reactive] public string Name { get; set; } = "";
  [Reactive] public string HR { get; set; } = "";
  [Reactive] public string Speed { get; set; } = "";
  [Reactive] public string Distance { get; set; } = "";
}
