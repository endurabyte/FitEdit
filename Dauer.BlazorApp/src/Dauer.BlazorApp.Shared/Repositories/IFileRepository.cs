using System.IO;
using System.Threading.Tasks;

namespace Dauer.BlazorApp.Shared.Repositories
{
    public interface IFileRepository
    {
        Task SaveAsync(Stream stream, string location, string name);
    }
}
