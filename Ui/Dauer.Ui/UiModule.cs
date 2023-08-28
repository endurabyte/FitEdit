using Autofac;
using Dauer.Model;
using Dauer.Ui.ViewModels;

namespace Dauer.Ui;

public class UiModule : Module
{
  protected override void Load(ContainerBuilder builder)
  {
    builder.RegisterType<MapViewModel>().As<IMapViewModel>()
      .WithParameter("tileSource", TileSource.Jawg);

    base.Load(builder);
  }
}