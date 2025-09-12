using ChatQueue.API.Models.Chat;
using ChatQueue.Application.Interfaces.Services;
using ChatQueue.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace ChatQueue.API.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chatService;
        private readonly ILogger<ChatController> _logger;

        public ChatController(IChatService chatService, ILogger<ChatController> logger)
        {
            _chatService = chatService;
            _logger = logger;
        }

        [HttpPost("create")]
        public async Task<ActionResult<ChatSessionResponse>> Create(CancellationToken ct)
        {
            try
            {
                _logger.LogInformation("Received request to create a new chat session.");
                var session = await _chatService.CreateChatAsync(ct);
                _logger.LogInformation("Successfully created chat session with ID: {SessionId}", session.Id);
                return Ok(new ChatSessionResponse { CreatedAt = session.CreatedAt, Id = session.Id, Status = session.Status.ToString() });
            }
            catch (QueueFullException ex)
            {
                _logger.LogWarning(ex, "Queue full: {Message}", ex.Message);
                return StatusCode(StatusCodes.Status429TooManyRequests, new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during chat creation.");
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = "An error occurred." });
            }
        }

        [HttpPost("{sessionId:guid}/poll")]
        public async Task<IActionResult> Poll(Guid sessionId, CancellationToken ct)
        {
            try
            {
                _logger.LogInformation("Received poll request for chat session with ID: {SessionId}", sessionId);
                if (!await _chatService.PollAsync(sessionId , ct))
                {
                    _logger.LogWarning("Session not found or inactive for session ID: {SessionId}", sessionId);
                    return NotFound(new { error = "Session not found or inactive." });
                }
                else
                {
                    _logger.LogInformation("Polling succeeded for session ID: {SessionId}", sessionId);
                    return Ok(new { message = "poll ok" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while polling chat session with ID: {SessionId}", sessionId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = "An error occurred while polling the chat session." });
            }
        }
    }
}
