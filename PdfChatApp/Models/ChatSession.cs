using System.Threading.Tasks;

namespace PdfChatApp.Models
{
    public class ChatSession
    {
        public string SessionId { get; set; } = Guid.NewGuid().ToString();
        public string PdfContent { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public List<ChatMessage> History { get; set; } = new();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastActivity { get; set; } = DateTime.UtcNow;
     }
 
}
