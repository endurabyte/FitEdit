using Amazon;
using Amazon.S3;
using Amazon.S3.Transfer;
using System;
using System.IO;
using System.Threading.Tasks;

namespace BlazorApp.Shared.Repositories
{
    public class S3FileRepository : IFileRepository
    {
        private const string bucketName = "www.sltr.us";
        private static IAmazonS3 _client;

        public S3FileRepository()
        {
            _client = new AmazonS3Client(RegionEndpoint.USEast1);
        }

        public async Task SaveAsync(Stream input, string location, string name)
        {
            try
            {
                var util = new TransferUtility(_client);
                await util.UploadAsync(input, bucketName, $"{location}/{name}");
            }
            catch (AmazonS3Exception e)
            {
                Console.WriteLine(e);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}
