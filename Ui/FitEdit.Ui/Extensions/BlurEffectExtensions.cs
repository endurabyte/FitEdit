using Avalonia.Media;

namespace FitEdit.Ui.Extensions;

public static class BlurEffectExtensions
{
  public static BlurEffect WithRadius(this BlurEffect effect, double radius)
  {
    effect.Radius = radius;
    return effect;
  }
}
