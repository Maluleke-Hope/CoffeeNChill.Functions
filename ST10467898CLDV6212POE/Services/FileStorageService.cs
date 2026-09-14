using Azure.Storage.Files.Shares;
using ST10467898CLDV6212POE.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace ST10467898CLDV6212POE.Services
{
    public class FileStorageService : IFileStorageService
    {
        private readonly ShareClient _shareClient;
        private const string ShareName = "staff-docs";

        public FileStorageService(IConfiguration configuration)
        {
            string conn = configuration["AzureWebJobsStorage"] 
                ?? throw new InvalidOperationException("Missing AzureWebJobsStorage connection string.");
            _shareClient = new ShareClient(conn, ShareName);
            _shareClient.CreateIfNotExists();
        }

        public async Task<bool> UploadDocumentAsync(IFormFile file)
        {
            var directory = _shareClient.GetRootDirectoryClient();
            var fileClient = directory.GetFileClient(file.FileName);
            using var stream = file.OpenReadStream();
            await fileClient.CreateAsync(stream.Length);
            await fileClient.UploadRangeAsync(new Azure.HttpRange(0, stream.Length), stream);
            return true;
        }

        public async Task<Stream?> DownloadDocumentAsync(string fileName)
        {
            var directory = _shareClient.GetRootDirectoryClient();
            var fileClient = directory.GetFileClient(fileName);
            if (!await fileClient.ExistsAsync()) return null;
            var download = await fileClient.DownloadAsync();
            return download.Value.Content;
        }

        public async Task<bool> DeleteDocumentAsync(string fileName)
        {
            var directory = _shareClient.GetRootDirectoryClient();
            var fileClient = directory.GetFileClient(fileName);
            return await fileClient.DeleteIfExistsAsync();
        }

        public async Task<List<string>> GetAllDocumentsAsync()
        {
            var docs = new List<string>();
            var directory = _shareClient.GetRootDirectoryClient();
            await foreach (var item in directory.GetFilesAndDirectoriesAsync())
            {
                if (!item.IsDirectory) docs.Add(item.Name);
            }
            return docs;
        }
    }
}