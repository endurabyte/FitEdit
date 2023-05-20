using ReactiveUI;

namespace Dauer.Ui.Models;

public class Record : ReactiveObject
{
  private int index_;
  private int messageNum_;
  private string name_ = "";

  public int Index { get => index_; set => this.RaiseAndSetIfChanged(ref index_, value); }
  public int MessageNum { get => messageNum_; set => this.RaiseAndSetIfChanged(ref messageNum_, value); }
  public string Name { get => name_; set => this.RaiseAndSetIfChanged(ref name_, value); }

}
