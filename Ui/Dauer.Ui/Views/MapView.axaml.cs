using Avalonia.Controls;
using Dauer.Ui.ViewModels;
using ReactiveUI;

namespace Dauer.Ui.Views;

public partial class MapView : UserControl
{
  public MapView()
  {
    InitializeComponent();

    this.ObservableForProperty(x => x.DataContext).Subscribe(e =>
    {
      if (DataContext is IMapViewModel map)
      {
        map.Map = MapControl;
      }
    });
  }
}