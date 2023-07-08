using Autofac;
using Avalonia.Controls.ApplicationLifetimes;

namespace Dauer.Ui;

public static class ContainerBuilderExtensions
{
  public static ContainerBuilder AddDauer(this ContainerBuilder builder, IApplicationLifetime? lifetime)
  {
    builder.RegisterModule(new DauerModule(lifetime));
    return builder;
  }
}