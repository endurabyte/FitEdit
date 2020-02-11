using System.IO;
using System.Threading.Tasks;

namespace BlazorApp.Shared.Repositories
{
    public interface IFileRepository
    {
        Task SaveAsync(Stream stream, string location, string name);
    }
}
