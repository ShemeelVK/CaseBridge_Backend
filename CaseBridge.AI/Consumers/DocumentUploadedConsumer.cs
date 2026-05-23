using CaseBridge_Contracts;
using CaseBridge_AI.Services;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Embeddings;
using Microsoft.SemanticKernel.Text;
using Minio;
using Minio.DataModel.Args;
using Qdrant.Client;
using Qdrant.Client.Grpc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CaseBridge_AI.Consumers
{
    public class DocumentUploadedConsumer : IConsumer<DocumentUploadedEvent>
    {
        private readonly IMinioClient _minioClient;
        private readonly Kernel _kernel;
        private readonly QdrantClient _qdrantClient;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly ILogger<DocumentUploadedConsumer> _logger;
        private readonly IConfiguration _configuration;

        // Constants for your storage
        private const string BucketName = "case-documents";
        private const string CollectionName = "CaseDocuments";

        public DocumentUploadedConsumer(
            IMinioClient minioClient,
            Kernel kernel,
            QdrantClient qdrantClient,
            IPublishEndpoint publishEndpoint,
            ILogger<DocumentUploadedConsumer> logger,
            IConfiguration configuration) // Injected Configuration for direct API access
        {
            _minioClient = minioClient;
            _kernel = kernel;
            _qdrantClient = qdrantClient;
            _publishEndpoint = publishEndpoint;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task Consume(ConsumeContext<DocumentUploadedEvent> context)
        {
            var msg = context.Message;
            _logger.LogInformation("Processing Document {DocumentId} for Case {CaseId}", msg.DocumentId, msg.CaseId);

            try
            {
                // Download PDF from MinIO into Memory
                var objectKey = msg.FileUrl.Split('/').Last();

                using var memoryStream = new MemoryStream();
                var getObjectArgs = new GetObjectArgs()
                    .WithBucket(BucketName)
                    .WithObject(objectKey) 
                    .WithCallbackStream((stream) => stream.CopyTo(memoryStream));

                await _minioClient.GetObjectAsync(getObjectArgs);
                
                if (memoryStream.Length == 0)
                {
                    _logger.LogError("Downloaded document {DocumentId} from MinIO is 0 bytes!", msg.DocumentId);
                    return; 
                }
                
                memoryStream.Position = 0;

                // Extract Text
                string extractedText = "";
                string extension = Path.GetExtension(objectKey).ToLowerInvariant();

                if (extension == ".pdf" || extension == ".docx" || extension == ".txt" || extension == ".csv")
                {
                    try
                    {
                        extractedText = DocumentExtractionService.ExtractText(memoryStream, extension);
                        _logger.LogInformation("Extracted {Length} characters from {Extension} document.", extractedText.Length, extension);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to parse document {DocumentId}.", msg.DocumentId);
                        return;
                    }
                }
                else
                {
                    _logger.LogWarning("Skipping document format {Extension}.", extension);
                    return;
                }

                _logger.LogInformation("Checking if vectors already exist for Document {DocumentId}...", msg.DocumentId);

                // Check collection existence FIRST — ScrollAsync will throw if collection doesn't exist yet
                var collectionExists = await _qdrantClient.CollectionExistsAsync(CollectionName);
                bool vectorsAlreadyExist = false;

                if (collectionExists)
                {
                    // Only run the dedup scroll if the collection is actually there
                    var existingPoints = await _qdrantClient.ScrollAsync(CollectionName,
                        filter: new Filter
                        {
                            Must = {
                                new Condition {
                                    Field = new FieldCondition {
                                        Key = "DocumentId",
                                        Match = new Match { Integer = msg.DocumentId }
                                    }
                                }
                            }
                        }, limit: 1);

                    vectorsAlreadyExist = existingPoints.Result.Any();
                }

                if (vectorsAlreadyExist)
                {
                    _logger.LogWarning("Vectors already exist for Document {DocumentId}. Skipping embedding generation to save API costs.", msg.DocumentId);
                    // Falls straight down to the summary generation below
                }
                else
                {
                    _logger.LogInformation("No existing vectors found. Starting chunking and embedding process...");

                    // Chunk the Text
                    var lines = TextChunker.SplitPlainTextLines(extractedText, maxTokensPerLine: 100);
                    var paragraphs = TextChunker.SplitPlainTextParagraphs(lines, maxTokensPerParagraph: 500, overlapTokens: 50);

                    // --- COHERE BATCH EMBEDDING (all paragraphs in ONE API call) ---
                    _logger.LogInformation("Sending batch embedding request to Cohere for {Count} chunks...", paragraphs.Count);

                    using (var httpClient = new HttpClient())
                    {
                        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_configuration["Cohere:ApiKey"]}");

                        var coherePayload = new
                        {
                            model = "embed-english-v3.0",
                            texts = paragraphs.ToArray(),
                            input_type = "search_document" // Tag as document for asymmetric search accuracy
                        };

                        var cohereResponse = await httpClient.PostAsJsonAsync("https://api.cohere.ai/v1/embed", coherePayload);

                        if (!cohereResponse.IsSuccessStatusCode)
                        {
                            var error = await cohereResponse.Content.ReadAsStringAsync();
                            _logger.LogError("Cohere Embedding API Failed: {Error}", error);
                            return; // Acknowledge message, stop processing
                        }

                        var cohereResult = await cohereResponse.Content.ReadFromJsonAsync<CohereEmbedResponseDto>();
                        if (cohereResult?.Embeddings == null || cohereResult.Embeddings.Count != paragraphs.Count)
                        {
                            _logger.LogError("Cohere returned unexpected number of embeddings.");
                            return;
                        }

                        var points = new List<PointStruct>();
                        for (int i = 0; i < paragraphs.Count; i++)
                        {
                            var point = new PointStruct
                            {
                                Id = (PointId)Guid.NewGuid(),
                                Vectors = cohereResult.Embeddings[i]
                            };
                            point.Payload.Add("CaseId", (long)msg.CaseId);
                            point.Payload.Add("DocumentId", (long)msg.DocumentId);
                            point.Payload.Add("Text", paragraphs[i]);
                            points.Add(point);
                        }

                        // Upsert all chunks into the Vector DB
                        if (points.Any())
                        {
                            // Reuse the already-fetched collectionExists flag — no redundant second check
                            if (!collectionExists)
                            {
                                try
                                {
                                    await _qdrantClient.CreateCollectionAsync(CollectionName, new VectorParams { Size = 1024, Distance = Distance.Cosine });
                                    _logger.LogInformation("Created Qdrant Collection: {CollectionName}", CollectionName);
                                }
                                catch (Grpc.Core.RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.AlreadyExists)
                                {
                                    // Race condition: another concurrent consumer created it first — safe to ignore
                                    _logger.LogInformation("Qdrant collection already exists (concurrent consumer won the race). Continuing upsert...");
                                }
                            }

                            await _qdrantClient.UpsertAsync(CollectionName, points);
                            _logger.LogInformation("Saved {Count} vector chunks to Qdrant for Case {CaseId}", points.Count, msg.CaseId);
                        }
                    }
                }

                // --- NATIVE HTTP GOOGLE AI STUDIO CALL (BYPASSING SEMANTIC KERNEL BUG) ---
                _logger.LogInformation("Sending direct summary request to Google AI Studio...");
                string summaryText = "Summary generation failed.";
                
                using (var httpClient = new HttpClient())
                {
                    var apiKey = _configuration["Gemini:ApiKey"];
                    var chatModel = _configuration["Gemini:ChatModel"] ?? "gemini-2.5-flash";
                    var apiUrl = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={apiKey}";

                    int maxSummaryLength = Math.Min(extractedText.Length, 15000);
                    string textForSummary = extractedText.Substring(0, maxSummaryLength);

                    string systemPrompt = $@"You are an expert legal assistant. Analyze the following document and provide a highly detailed, professional summary. 

                    Your response MUST be exactly three distinct paragraphs structured as follows:
                    Paragraph 1 (The Core Issue): Identify the main parties, the primary legal dispute, and the overarching purpose of the document.
                    Paragraph 2 (The Facts & Arguments): Outline the critical background facts, key allegations, and the primary legal or factual arguments presented.
                    Paragraph 3 (The Conclusion & Relief): Summarize the requested relief, the current procedural status, or the immediate next steps dictated by the document.

                    Document Text:
                    {textForSummary}";

                    // Build the exact JSON Google expects
                    var jsonPayload = new
                    {
                        contents = new[] {
                            new {
                                parts = new[] {
                                    new { text = systemPrompt }
                                }
                            }
                        },
                        safetySettings = new[]
                        {
                            // Lower thresholds to allow legal/crime case processing
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
                        _logger.LogError(" GOOGLE API REJECTED REQUEST \nStatus: {Status}\nDetails: {Details}", response.StatusCode, googleError);

                        throw new HttpRequestException($"Google API Error: {googleError}");
                    }
                    
                    var jsonResult = await response.Content.ReadFromJsonAsync<GeminiResponseDto>();
                    if (jsonResult?.Candidates != null && jsonResult.Candidates.Count > 0)
                    {
                        summaryText = jsonResult.Candidates[0].Content?.Parts?[0].Text ?? summaryText;
                    }
                }

                // Publish the final event back to the Cases service
                await _publishEndpoint.Publish(new CaseSummaryGeneratedEvent
                {
                    CaseId = msg.CaseId,
                    SummaryText = summaryText
                });

                _logger.LogInformation("Successfully published CaseSummaryGeneratedEvent for Case {CaseId}", msg.CaseId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process document {DocumentId} for Case {CaseId}", msg.DocumentId, msg.CaseId);
                throw;
            }
        }
    }

    // --- DTOs for Native Google AI Studio Response ---
    public class GeminiResponseDto
    {
        public List<GeminiCandidate>? Candidates { get; set; }
    }

    public class GeminiCandidate
    {
        public GeminiContent? Content { get; set; }
    }

    public class GeminiContent
    {
        public List<GeminiPart>? Parts { get; set; }
    }

    public class GeminiPart
    {
        public string? Text { get; set; }
    }

    // --- DTO for Cohere Embed API Response ---
    public class CohereEmbedResponseDto
    {
        [JsonPropertyName("embeddings")]
        public List<float[]> Embeddings { get; set; } = new();
    }
}