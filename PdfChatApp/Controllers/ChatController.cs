using Microsoft.AspNetCore.Mvc;
using PdfChatApp.Models;
using PdfChatApp.Services;

namespace PdfChatApp.Controllers;

public class ChatController : Controller
{
    private readonly IPdfProcessingService _pdfProcessing;
    private readonly IOllamaService _ollama;
    private readonly IChatService _chatService;
    private readonly ILogger<ChatController> _logger;

    public ChatController(
        IPdfProcessingService pdfProcessing,
        IOllamaService ollama,
        IChatService chatService,
        ILogger<ChatController> logger)
    {
        _pdfProcessing = pdfProcessing;
        _ollama = ollama;
        _chatService = chatService;
        _logger = logger;
    }

    public IActionResult Index(string? sessionId)
    {
        if (!string.IsNullOrEmpty(sessionId))
        {
            var session = _chatService.GetSession(sessionId);
            if (session != null)
            {
                ViewBag.SessionId = sessionId;
                ViewBag.FileName = session.FileName;
                return View(session);
            }
        }

        return View(new ChatSession());
    }

    [HttpPost]
    [RequestSizeLimit(52428800)] 
    public async Task<IActionResult> Upload(IFormFile pdfFile)
    {
        try
        {
            if (pdfFile == null || pdfFile.Length == 0)
            {
                return Json(new PdfUploadResult
                {
                    Success = false,
                    Error = "Please select a PDF file to upload."
                });
            }

            if (!pdfFile.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                return Json(new PdfUploadResult
                {
                    Success = false,
                    Error = "Only PDF files are supported."
                });
            }

            if (pdfFile.Length > 50 * 1024 * 1024) 
            {
                return Json(new PdfUploadResult
                {
                    Success = false,
                    Error = "File size must be less than 50MB."
                });
            }

            // Check if Ollama is available
            var isOllamaAvailable = await _ollama.IsAvailableAsync();
            if (!isOllamaAvailable)
            {
                return Json(new PdfUploadResult
                {
                    Success = false,
                    Error = "AI service is not available. Please ensure Ollama is running."
                });
            }

            // Extract text from PDF
            await using var stream = pdfFile.OpenReadStream();
            var (content, pageCount) = await _pdfProcessing.ExtractTextFromPdfAsync(stream);

            if (string.IsNullOrWhiteSpace(content))
            {
                return Json(new PdfUploadResult
                {
                    Success = false,
                    Error = "Could not extract text from PDF. The PDF may be empty or image-based."
                });
            }

            // Create chat session
            var sessionId = _chatService.CreateSession(content, pdfFile.FileName, pageCount);

            return Json(new PdfUploadResult
            {
                Success = true,
                Message = $"PDF uploaded successfully! {pageCount} pages processed.",
                SessionId = sessionId,
                PageCount = pageCount,
                FileName = pdfFile.FileName
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading PDF");
            return Json(new PdfUploadResult
            {
                Success = false,
                Error = $"An error occurred: {ex.Message}"
            });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Ask([FromBody] ChatRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return Json(new ChatResponse
                {
                    Success = false,
                    Error = "Please enter a question."
                });
            }

            if (string.IsNullOrWhiteSpace(request.SessionId))
            {
                return Json(new ChatResponse
                {
                    Success = false,
                    Error = "Session expired. Please upload the PDF again."
                });
            }

            var session = _chatService.GetSession(request.SessionId);
            if (session == null)
            {
                return Json(new ChatResponse
                {
                    Success = false,
                    Error = "Session not found. Please upload the PDF again."
                });
            }

      
            var userMessage = new ChatMessage
            {
                Role = "user",
                Content = request.Message,
                Timestamp = DateTime.UtcNow
            };
            _chatService.AddMessage(request.SessionId, userMessage);

         
            var response = await _ollama.GenerateResponseAsync(request.Message, session.PdfContent);

          
            if (!IsResponseAppropriate(response, request.Message, session.PdfContent))
            {
                response = "I don't have that information in the uploaded PDF document.";
            }

            
            var assistantMessage = new ChatMessage
            {
                Role = "assistant",
                Content = response,
                Timestamp = DateTime.UtcNow
            };
            _chatService.AddMessage(request.SessionId, assistantMessage);

            
            var updatedSession = _chatService.GetSession(request.SessionId);

            return Json(new ChatResponse
            {
                Success = true,
                Message = response,
                History = updatedSession?.History ?? new List<ChatMessage>()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing question");
            return Json(new ChatResponse
            {
                Success = false,
                Error = $"An error occurred: {ex.Message}"
            });
        }
    }

    private bool IsResponseAppropriate(string response, string question, string pdfContent)
    {
        
        if (response.Contains("don't have that information", StringComparison.OrdinalIgnoreCase) ||
            response.Contains("not in the PDF", StringComparison.OrdinalIgnoreCase) ||
            response.Contains("not mentioned", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var offTopicKeywords = new[]
        {
            "weather", "current date", "who is the president", "latest news",
            "recipe for", "how to make", "movie recommendation", "song lyrics",
            "sports score", "stock price"
        };

        var lowerQuestion = question.ToLowerInvariant();
        foreach (var keyword in offTopicKeywords)
        {
            if (lowerQuestion.Contains(keyword))
            {
               
                if (!pdfContent.ToLowerInvariant().Contains(keyword))
                {
                    return false;
                }
            }
        }

        return true;
    }

    [HttpPost]
    public IActionResult ClearSession([FromBody] ChatRequest request)
    {
        try
        {
            if (!string.IsNullOrEmpty(request.SessionId))
            {
                
                _logger.LogInformation("Clearing session: {SessionId}", request.SessionId);
            }

            return Json(new { success = true, message = "Session cleared successfully." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing session");
            return Json(new { success = false, error = ex.Message });
        }
    }
}