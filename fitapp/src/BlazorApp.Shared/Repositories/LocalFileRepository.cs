using System.IO;
using System.Threading.Tasks;

namespace BlazorApp.Shared.Repositories
{
    public class LocalFileRepository : IFileRepository
    {
        public async Task SaveAsync(Stream stream, string location, string name)
        {
            Directory.CreateDirectory(location);

            using var outStream = new FileStream(Path.Combine(location, name), FileMode.Create);
            await stream.CopyToAsync(outStream);
        }
    }
}
