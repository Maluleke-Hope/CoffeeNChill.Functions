using System.Net;
using ST10467898CLDV6212POE.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace ST10467898CLDV6212POE.Functions
{
    public class StaffDocumentFunctions
    {
        private readonly IFileStorageService _fileService;

        public StaffDocumentFunctions(IFileStorageService fileService)
        {
            _fileService = fileService;
        }

        [Function("ListStaffDocuments")]
        public async Task<HttpResponseData> ListDocs([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "documents")] HttpRequestData req)
        {
            var docs = await _fileService.GetAllDocumentsAsync();
            var res = req.CreateResponse(HttpStatusCode.OK);
            await res.WriteAsJsonAsync(docs);
            return res;
        }
    }
}