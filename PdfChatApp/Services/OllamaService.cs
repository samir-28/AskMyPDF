

using System.Text;
using System.Text.Json;

namespace PdfChatApp.Services;

public interface IOllamaService
{
    Task<string> GenerateResponseAsync(string prompt, string context);
    Task<bool> IsAvailableAsync();
}

public class OllamaService : IOllamaService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<OllamaService> _logger;
    private readonly string _modelName;

    public OllamaService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<OllamaService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _modelName = configuration["Ollama:ModelName"] ?? "llama3.2";
    }

    public async Task<bool> IsAvailableAsync()
    {
        try
        {
            var client = _httpClientFactory.CreateClient("Ollama");
            var response = await client.GetAsync("/api/tags");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ollama service is not available");
            return false;
        }
    }

    public async Task<string> GenerateResponseAsync(string prompt, string context)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("Ollama");

            
            var truncatedContext = context.Length > 6000
                ? "..." + context.Substring(context.Length - 6000)
                : context;

            var systemPrompt = $@"You are a PDF Document Assistant. Your ONLY job is to answer questions based STRICTLY on the provided PDF content.

CRITICAL RULES:
1. ONLY use information from the PDF content below
2. If the answer is NOT in the PDF, you MUST respond EXACTLY with: ""I don't have that information in the uploaded PDF document.""
3. DO NOT use any external knowledge, training data, or general information
4. DO NOT make assumptions or provide general answers
5. DO NOT answer questions about topics not mentioned in the PDF

PDF DOCUMENT CONTENT:
---START OF PDF---
{truncatedContext}
---END OF PDF---

RESPONSE FORMATTING GUIDELINES:
- Use clear paragraphs with proper spacing
- Use bullet points (•) for lists
- Use numbered lists (1., 2., 3.) for steps or sequences
- Use **bold** for emphasis on key terms
- Keep responses well-structured and easy to read
- If referencing specific sections, mention the page or section
- Break long responses into digestible paragraphs

Remember: If the information is not in the PDF content above, you MUST say you don't have that information. Do not make up or infer answers.";

            var requestBody = new
            {
                model = _modelName,
                prompt = $"User question: {prompt}\n\nProvide a well-formatted answer based ONLY on the PDF content. If not found in PDF, say you don't have that information.",
                system = systemPrompt,
                stream = false,
                options = new
                {
                    temperature = 0.1,  
                    top_p = 0.5,        
                    top_k = 20,         
                    num_predict = 800,  
                    stop = new[] { "User question:", "Human:", "Question:" }  
                }
            };

            var jsonContent = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await client.PostAsync("/api/generate", content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Ollama API error: {StatusCode} - {Error}", response.StatusCode, errorContent);
                throw new HttpRequestException($"Ollama API returned {response.StatusCode}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var jsonResponse = JsonDocument.Parse(responseContent);

            var answer = jsonResponse.RootElement.GetProperty("response").GetString() ??
                        "I encountered an error processing your request.";

            answer = answer.Trim();

            if (!string.IsNullOrEmpty(answer) &&
                !answer.Contains("don't have that information", StringComparison.OrdinalIgnoreCase) &&
                !answer.Contains("not in the PDF", StringComparison.OrdinalIgnoreCase))
            {
                
                var isLikelyFromPdf = ContainsRelevantTerms(answer, truncatedContext);

                if (!isLikelyFromPdf && !IsQuestionAboutPdfStructure(prompt))
                {
                    answer = "I don't have that information in the uploaded PDF document.";
                }
            }

            return answer;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating response from Ollama");
            throw new InvalidOperationException("Failed to generate response. Please ensure Ollama is running.", ex);
        }
    }

    private bool ContainsRelevantTerms(string answer, string pdfContent)
    {
        var commonWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "the", "is", "at", "which", "on", "a", "an", "and", "or", "but", "in", "with", "to", "for",
            "of", "as", "by", "from", "this", "that", "these", "those", "it", "be", "are", "was", "were",
            "been", "being", "have", "has", "had", "do", "does", "did", "will", "would", "should", "could",
            "may", "might", "can", "must", "shall", "i", "you", "he", "she", "we", "they", "what", "when",
            "where", "why", "how", "document", "pdf", "information", "uploaded"
        };

        var answerWords = answer.Split(new[] { ' ', '.', ',', '!', '?', '\n', '\r' },
            StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length > 3 && !commonWords.Contains(w))
            .Take(10);  
        var pdfLower = pdfContent.ToLowerInvariant();
        var matchCount = answerWords.Count(word =>
            pdfLower.Contains(word.ToLowerInvariant()));

    
        return matchCount >= Math.Max(2, answerWords.Count() * 0.3);
    }

    private bool IsQuestionAboutPdfStructure(string question)
    {
        var structureKeywords = new[] { "summarize", "summary", "about", "topic", "main point",
            "overview", "describe", "explain the document", "what is this document" };

        var lowerQuestion = question.ToLowerInvariant();
        return structureKeywords.Any(keyword => lowerQuestion.Contains(keyword));
    }
}