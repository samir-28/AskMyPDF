namespace PdfChatApp.Models
{
    public class PdfUploadResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Error { get; set; }
        public string? SessionId { get; set; }
        public int PageCount { get; set; }
        public string? FileName { get; set; }
    }
}
