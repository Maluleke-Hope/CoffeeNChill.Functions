using Microsoft.AspNetCore.Http;

namespace ST10467898CLDV6212POE.Interfaces
{
    public interface IFileStorageService
    {
        Task<bool> UploadDocumentAsync(IFormFile file);
        Task<Stream?> DownloadDocumentAsync(string fileName);
        Task<bool> DeleteDocumentAsync(string fileName);
        Task<List<string>> GetAllDocumentsAsync();
    }
}