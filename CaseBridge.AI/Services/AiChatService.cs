using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Embeddings;
using Qdrant.Client;
using Qdrant.Client.Grpc;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CaseBridge_AI.Services
{
    public interface IAiChatService
    {
        Task<string> AskQuestionAsync(int caseId, List<CaseBridge_AI.Controllers.ChatMessageDto> history, string? caseDetails = null);
    }

    public class AiChatService : IAiChatService
    {
        private readonly Kernel _kernel;
        private readonly QdrantClient _qdrantClient;
        private readonly ILogger<AiChatService> _logger;
        private readonly IConfiguration _configuration;
        private const string CollectionName = "CaseDocuments";

        public AiChatService(Kernel kernel, QdrantClient qdrantClient, ILogger<AiChatService> logger, IConfiguration configuration)
        {
            _kernel = kernel;
            _qdrantClient = qdrantClient;
            _logger = logger;
            _configuration = configuration;

        }

        public async Task<string> AskQuestionAsync(int caseId, List<CaseBridge_AI.Controllers.ChatMessageDto> history, string? caseDetails = null)
        {
            try
            {
                // 1. Grab the latest question from the user
                var latestMessage = history.LastOrDefault(m => m.Role.Equals("user", StringComparison.OrdinalIgnoreCase))?.Text;
                if (string.IsNullOrWhiteSpace(latestMessage)) return "No valid user question found.";

                // 2. Convert the question into a vector using Cohere (search_query for asymmetric accuracy)
                float[] questionVector = Array.Empty<float>();
                using (var httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_configuration["Cohere:ApiKey"]}");
                    var coherePayload = new
                    {
                        model = "embed-english-v3.0",
                        texts = new[] { latestMessage },
                        input_type = "search_query" // Asymmetric: query vs document gives better relevance
                    };
                    var cohereResponse = await httpClient.PostAsJsonAsync("https://api.cohere.ai/v1/embed", coherePayload);
                    var cohereResult = await cohereResponse.Content.ReadFromJsonAsync<CohereEmbedResponseDtoChat>();
                    questionVector = cohereResult?.Embeddings?[0] ?? Array.Empty<float>();
                }

                var contextBuilder = new StringBuilder();

                var collectionExists = await _qdrantClient.CollectionExistsAsync(CollectionName);
                if (collectionExists)
                {
                    var condition = new Condition
                    {
                        Field = new FieldCondition
                        {
                            Key = "CaseId",
                            Match = new Match { Integer = (long)caseId } // Qdrant stores all integers as int64 internally
                        }
                    };

                    var searchFilter = new Filter();
                    searchFilter.Must.Add(condition);

                    // 3. Attempt standard Vector Search
                    var searchResults = await _qdrantClient.SearchAsync(
                        collectionName: CollectionName,
                        vector: questionVector,
                        filter: searchFilter,
                        limit: 5,
                        payloadSelector: true
                    );

                    // 4. THE FALLBACK: If Vector Search fails, grab the documents directly!
                    if (searchResults.Count == 0)
                    {
                        _logger.LogWarning("🚨 Vector search returned 0 results. Bypassing vectors and grabbing documents directly via ScrollAsync...");

                        var scrollResults = await _qdrantClient.ScrollAsync(
                            collectionName: CollectionName,
                            filter: searchFilter,
                            limit: 5, // Grabs the first 5 chunks of the document
                            payloadSelector: true
                        );

                        foreach (var point in scrollResults.Result)
                        {
                            if (point.Payload.TryGetValue("Text", out var textValue))
                            {
                                contextBuilder.AppendLine(textValue.StringValue);
                                contextBuilder.AppendLine("---");
                            }
                        }
                    }
                    else
                    {
                        // Vector search worked, use those results
                        foreach (var result in searchResults)
                        {
                            if (result.Payload.TryGetValue("Text", out var textValue))
                            {
                                contextBuilder.AppendLine(textValue.StringValue);
                                contextBuilder.AppendLine("---");
                            }
                        }
                    }
                }

                // Prepare the context string
                string documentContext = contextBuilder.Length > 0
                    ? contextBuilder.ToString()
                    : "No documents have been uploaded or processed for this case yet.";

                // 5. Build the System Instructions
                string systemPrompt = $@"
                    You are CaseBridge AI, a highly intelligent, professional, and friendly legal assistant helping a lawyer analyze a specific case.

                    Case Metadata & Details (Overview):
                    {caseDetails ?? "No metadata provided."}

                    Context from Case Documents (Relevant to the latest question):
                    {documentContext}
                    
                    CRITICAL INSTRUCTIONS:
                    1. BE DIRECT AND CONCISE. Do not write a comprehensive overview unless explicitly asked.
                    2. DO NOT REPEAT YOURSELF. You have access to the chat history. Do not re-explain the background of the case if you already did.
                    3. Conversational Greetings: Respond warmly. Do NOT complain about missing context.
                    4. Legal Assistance: You are encouraged to provide legal strategy, practical advice, and suggestions on how to resolve the case using your general legal knowledge.
                    5. Context Usage: Ground your answers in the 'Context from Case Documents' where applicable, but do not restrict yourself to it. If the context is missing specific answers, rely on your broader legal expertise to guide the user.";

                // 6. Format the React history
                var formattedContents = history.TakeLast(4).Select(h => new
                {
                    role = h.Role.ToLower() == "model" ? "model" : "user",
                    parts = new[] { new { text = h.Text } }
                }).ToArray();

                // --- NATIVE HTTP GOOGLE AI STUDIO CALL ---
                using (var httpClient = new HttpClient())
                {
                    var apiKey = _configuration["Gemini:ApiKey"];
                    var chatModel = _configuration["Gemini:ChatModel"] ?? "gemini-2.5-flash";
                    var apiUrl = $"https://generativelanguage.googleapis.com/v1beta/models/{chatModel}:generateContent?key={apiKey}";

                    var jsonPayload = new
                    {
                        systemInstruction = new { parts = new[] { new { text = systemPrompt } } },
                        contents = formattedContents,
                        safetySettings = new[]
                        {
                            new { category = "HARM_CATEGORY_HARASSMENT", threshold = "BLOCK_ONLY_HIGH" },
                            new { category = "HARM_CATEGORY_HATE_SPEECH", threshold = "BLOCK_ONLY_HIGH" },
                            new { category = "HARM_CATEGORY_SEXUALLY_EXPLICIT", threshold = "BLOCK_ONLY_HIGH" },
                            new { category = "HARM_CATEGORY_DANGEROUS_CONTENT", threshold = "BLOCK_ONLY_HIGH" }
                        }
                    };

                    var response = await httpClient.PostAsJsonAsync(apiUrl, jsonPayload);

                    if (!response.IsSuccessStatusCode)
                    {
                        string googleError = await response.Content.ReadAsStringAsync();
                        _logger.LogError("🛑 GOOGLE API REJECTED CHAT REQUEST 🛑 \nStatus: {Status}\nDetails: {Details}", response.StatusCode, googleError);
                        return "I'm sorry, I encountered an error communicating with the AI gateway.";
                    }

                    var jsonResult = await response.Content.ReadFromJsonAsync<GeminiChatResponseDto>();
                    return jsonResult?.Candidates?[0]?.Content?.Parts?[0]?.Text ?? "I'm sorry, I could not generate an answer.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing AI chat request for CaseId {CaseId}", caseId);
                throw;
            }
        }
    }

    public class GeminiChatResponseDto
    {
        public List<GeminiChatCandidate>? Candidates { get; set; }
    }

    public class GeminiChatCandidate
    {
        public GeminiChatContent? Content { get; set; }
    }

    public class GeminiChatContent
    {
        public List<GeminiChatPart>? Parts { get; set; }
    }

    public class GeminiChatPart
    {
        public string? Text { get; set; }
    }

    // --- DTO for Cohere Embed API Response (Chat Service) ---
    public class CohereEmbedResponseDtoChat
    {
        [JsonPropertyName("embeddings")]
        public List<float[]>? Embeddings { get; set; }
    }
}
