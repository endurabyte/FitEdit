using Autofac;
using FitEdit.Ui.Infra;

namespace FitEdit.Ui.Android;

public class AndroidCompositionRoot : CompositionRoot
{
  protected override async Task ConfigureAsync(ContainerBuilder builder)
  {
    await base.ConfigureAsync(builder);
  }
}
