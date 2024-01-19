using Autofac;
using FitEdit.Model;
using FitEdit.Ui.ViewModels;

namespace FitEdit.Ui;

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