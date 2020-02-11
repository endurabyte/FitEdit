using SharpCompress.Common;
using SharpCompress.Writers;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace BlazorApp.Shared.Repositories
{
    public class MultiSinkFileRepository : IMultiSinkFileRepository
    {
        private readonly List<IFileRepository> _repos;
        private readonly bool _zip;

        public MultiSinkFileRepository(IFileRepository localRepo, IFileRepository s3Repo, bool zip = true)
        {
            _repos = new List<IFileRepository> { localRepo, s3Repo };
            _zip = zip;
        }

        public async Task SaveAsync(Stream stream, string location, string name)
        {
            var zipped = _zip ? TarGzipFile(stream, name) : stream;
            using var reader = new StreamReader(zipped);

            foreach (var repo in _repos)
            {
                reader.BaseStream.Position = 0;

                await repo
                    .SaveAsync(reader.BaseStream, location, $"{name}.tar.gz")
                    .ConfigureAwait(false);
            }
        }

        private static Stream TarGzipFile(Stream input, string name)
        {
            var ms = new MemoryStream();
            var opts = new WriterOptions(SharpCompress.Common.CompressionType.GZip) { LeaveStreamOpen = true };
            using var writer = WriterFactory.Open(ms, ArchiveType.Tar, opts);
            writer.Write(name, input);

            return ms;
        }
    }
}
