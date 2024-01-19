using Autofac;
using FitEdit.Ui.Infra;

namespace FitEdit.Ui.Desktop;

public class DesktopCompositionRoot : CompositionRoot
{
  protected override async Task ConfigureAsync(ContainerBuilder builder)
  {
    await base.ConfigureAsync(builder);
  }
}