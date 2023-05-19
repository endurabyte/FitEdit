using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Dauer.Model
{
  public class PropertyChangedBase : INotifyPropertyChanged
  {
    public event PropertyChangedEventHandler PropertyChanged;

    public void NotifyPropertyChanged(params string[] propertyNames)
    {
      foreach (string name in propertyNames)
      {
        NotifyPropertyChanged(name);
      }
    }

    public void NotifyPropertyChanged([CallerMemberName] string propertyName = default)
      => NotifyPropertyChanged(this, new PropertyChangedEventArgs(propertyName));

    public void NotifyPropertyChanged(object sender, PropertyChangedEventArgs args)
      => PropertyChanged?.Invoke(sender, args);
  }
}
