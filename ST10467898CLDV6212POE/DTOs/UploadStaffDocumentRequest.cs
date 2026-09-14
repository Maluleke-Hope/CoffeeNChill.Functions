namespace ST10467898CLDV6212POE.DTOs;

public class UploadStaffDocumentRequest
{
    // Name of the staff member.
    public string StaffName { get; set; } = string.Empty;

    // Name of the document being uploaded.
    public string FileName { get; set; } = string.Empty;

    // The document content encoded as Base64.
    public string FileContentBase64 { get; set; } = string.Empty;
}