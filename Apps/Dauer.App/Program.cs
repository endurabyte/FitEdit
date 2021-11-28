using Dauer.Infrastructure;

namespace Dauer.App
{
  public class Program
  {
    public static async Task Main(string[] args)
    {
      ICompositionRoot root = new CompositionRoot();

      var app = new AppCompositionRoot(root).App;

      await app
        .ExecuteAsync(args)
        .ConfigureAwait(false);
    }
  }
}