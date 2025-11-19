namespace PdfChatApp.Models
{
    public class ChatResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Error { get; set; }
        public List<ChatMessage> History { get; set; } = new();
    }
}
