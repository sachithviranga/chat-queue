using ChatQueue.Application.Interfaces.Services;
using ChatQueue.Domain.Entities;
using ChatQueue.Domain.Interfaces;
using Microsoft.AspNetCore.Http;
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
        public async Task<ActionResult<ChatSession>> Create(CancellationToken ct)
        {
            try
            {
                _logger.LogInformation("Received request to create a new chat session.");
                var session = await _chatService.CreateChatAsync(ct);
                _logger.LogInformation("Successfully created chat session with ID: {SessionId}", session.Id);
                return Ok(session);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating chat session.");
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = "An error occurred while creating the chat session." });
            }
        }

        [HttpPost("{sessionId:guid}/poll")]
        public IActionResult Poll(Guid sessionId)
        {
            try
            {
                _logger.LogInformation("Polling chat session with ID: {SessionId}", sessionId);
                _chatService.Poll(sessionId);
                _logger.LogInformation("Polling successful for session ID: {SessionId}", sessionId);
                return Ok(new { message = "poll ok" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while polling chat session {SessionId}.", sessionId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = "An error occurred while polling the chat session." });
            }
        }
    }
}
