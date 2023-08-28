using Autofac.Builder;

namespace Dauer.Ui.Infra;

public static class RegistrationBuilderExtensions
{
  public static void SingletonIf<T>(this IRegistrationBuilder<object, T, SingleRegistrationStyle> x, bool singleton)
  {
    if (singleton) { x.SingleInstance(); }
  }
}
