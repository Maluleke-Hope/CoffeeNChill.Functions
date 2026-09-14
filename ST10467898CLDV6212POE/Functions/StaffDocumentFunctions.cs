using System.Net;
using System.Text.Json;
using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using ST10467898CLDV6212POE.DTOs;

namespace ST10467898CLDV6212POE.Functions;

public class StaffDocumentFunctions
{
    private readonly ShareServiceClient _shareServiceClient;
    private readonly ILogger<StaffDocumentFunctions> _logger;

    private const string ShareName = "staffdocuments";

    public StaffDocumentFunctions(
        ShareServiceClient shareServiceClient,
        ILoggerFactory loggerFactory)
    {
        _shareServiceClient = shareServiceClient;
        _logger = loggerFactory.CreateLogger<StaffDocumentFunctions>();
    }

    // Uploads a staff document to Azure File Share.
    [Function("UploadStaffDocument")]
    public async Task<HttpResponseData> UploadStaffDocument(
        [HttpTrigger(
            AuthorizationLevel.Anonymous,
            "post",
            Route = "staff-documents")]
        HttpRequestData req)
    {
        try
        {
            var request =
                await JsonSerializer.DeserializeAsync<UploadStaffDocumentRequest>(
                    req.Body,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

            if (request == null)
            {
                return await CreateErrorResponse(
                    req,
                    HttpStatusCode.BadRequest,
                    "Invalid request.");
            }

            if (string.IsNullOrWhiteSpace(request.StaffName))
            {
                return await CreateErrorResponse(
                    req,
                    HttpStatusCode.BadRequest,
                    "StaffName is required.");
            }

            if (string.IsNullOrWhiteSpace(request.FileName))
            {
                return await CreateErrorResponse(
                    req,
                    HttpStatusCode.BadRequest,
                    "FileName is required.");
            }

            if (string.IsNullOrWhiteSpace(request.FileContentBase64))
            {
                return await CreateErrorResponse(
                    req,
                    HttpStatusCode.BadRequest,
                    "FileContentBase64 is required.");
            }

            byte[] fileBytes;

            try
            {
                fileBytes =
                    Convert.FromBase64String(request.FileContentBase64);
            }
            catch (FormatException)
            {
                return await CreateErrorResponse(
                    req,
                    HttpStatusCode.BadRequest,
                    "Invalid Base64 file content.");
            }

            ShareClient shareClient =
                _shareServiceClient.GetShareClient(ShareName);

            await shareClient.CreateIfNotExistsAsync();

            ShareDirectoryClient directoryClient =
                shareClient.GetDirectoryClient(request.StaffName);

            await directoryClient.CreateIfNotExistsAsync();

            ShareFileClient fileClient =
                directoryClient.GetFileClient(request.FileName);

            using MemoryStream stream =
                new MemoryStream(fileBytes);

            await fileClient.CreateAsync(stream.Length);

            stream.Position = 0;

            await fileClient.UploadAsync(stream);

            _logger.LogInformation(
                "Staff document uploaded: {FileName}",
                request.FileName);

            var response =
                req.CreateResponse(HttpStatusCode.Created);

            await response.WriteAsJsonAsync(new
            {
                message = "Staff document uploaded successfully.",
                staffName = request.StaffName,
                fileName = request.FileName
            });

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error uploading staff document.");

            return await CreateErrorResponse(
                req,
                HttpStatusCode.InternalServerError,
                ex.Message);
        }
    }

    // Retrieves a staff document from Azure File Share.
    [Function("GetStaffDocument")]
    public async Task<HttpResponseData> GetStaffDocument(
        [HttpTrigger(
            AuthorizationLevel.Anonymous,
            "get",
            Route = "staff-documents/{staffName}/{fileName}")]
        HttpRequestData req,
        string staffName,
        string fileName)
    {
        try
        {
            ShareClient shareClient =
                _shareServiceClient.GetShareClient(ShareName);

            ShareDirectoryClient directoryClient =
                shareClient.GetDirectoryClient(staffName);

            ShareFileClient fileClient =
                directoryClient.GetFileClient(fileName);

            if (!await fileClient.ExistsAsync())
            {
                return await CreateErrorResponse(
                    req,
                    HttpStatusCode.NotFound,
                    "Staff document not found.");
            }

            ShareFileDownloadInfo download =
                await fileClient.DownloadAsync();

            var response =
                req.CreateResponse(HttpStatusCode.OK);

            response.Headers.Add(
                "Content-Type",
                "application/octet-stream");

            response.Headers.Add(
                "Content-Disposition",
                $"attachment; filename=\"{fileName}\"");

            await download.Content.CopyToAsync(
                response.Body);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error retrieving staff document.");

            return await CreateErrorResponse(
                req,
                HttpStatusCode.InternalServerError,
                ex.Message);
        }
    }

    private static async Task<HttpResponseData> CreateErrorResponse(
        HttpRequestData req,
        HttpStatusCode statusCode,
        string message)
    {
        var response = req.CreateResponse(statusCode);

        await response.WriteAsJsonAsync(new
        {
            error = message
        });

        return response;
    }
}