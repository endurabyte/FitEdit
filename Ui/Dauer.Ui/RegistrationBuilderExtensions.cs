using Autofac.Builder;

namespace Dauer.Ui;

public static class RegistrationBuilderExtensions
{
  public static void SingletonIf<T>(this IRegistrationBuilder<object, T, SingleRegistrationStyle> x, bool singleton)
  {
    if (singleton) { x.SingleInstance(); }
  }
}
