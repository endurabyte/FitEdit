using Autofac.Builder;

namespace FitEdit.Ui.Infra;

public static class RegistrationBuilderExtensions
{
  public static void SingletonIf<T>(this IRegistrationBuilder<object, T, SingleRegistrationStyle> x, bool singleton)
  {
    if (singleton) { x.SingleInstance(); }
  }
}
