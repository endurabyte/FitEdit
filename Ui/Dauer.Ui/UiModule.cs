using Autofac;
using Dauer.Model;
using Dauer.Ui.ViewModels;

namespace Dauer.Ui;

public class UiModule : Module
{
  private readonly bool isMobile_;

  public UiModule(bool isMobile)
  {
    isMobile_ = isMobile;
  }

  protected override void Load(ContainerBuilder builder)
  {
    builder.RegisterType<MapViewModel>().As<IMapViewModel>()
      .WithParameter("tileSource", TileSource.Jawg);

    builder.RegisterType<MainViewModel>().As<IMainViewModel>()
      .WithParameter("isCompact", isMobile_);
    base.Load(builder);
  }
}