using CaseBridge_AI.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CaseBridge_AI.Controllers
{
    public class ChatMessageDto
    {
        public string Role { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
    }

    public class ChatRequest
    {
        public int CaseId { get; set; }
        public string? CaseDetails { get; set; }
        public List<ChatMessageDto> History { get; set; } = new();
    }

    [ApiController]
    [Route("api/aichat")]
    public class AiChatController : ControllerBase
    {
        private readonly IAiChatService _aiChatService;
        private readonly ILogger<AiChatController> _logger;

        public AiChatController(IAiChatService aiChatService, ILogger<AiChatController> logger)
        {
            _aiChatService = aiChatService;
            _logger = logger;
        }

        [HttpPost("ask")]
        public async Task<IActionResult> AskQuestion([FromBody] ChatRequest request)
        {
            if (request.History == null || !request.History.Any())
                return BadRequest("Chat history cannot be empty.");

            try
            {
                var answer = await _aiChatService.AskQuestionAsync(request.CaseId, request.History, request.CaseDetails);
                return Ok(new { Answer = answer });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing AI chat request for CaseId {CaseId}", request.CaseId);
                return StatusCode(500, "An error occurred while communicating with the AI service.");
            }
        }
    }
}
