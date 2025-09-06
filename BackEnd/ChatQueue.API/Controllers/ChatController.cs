using ChatQueue.Application.Interfaces.Services;
using ChatQueue.Domain.Entities;
using ChatQueue.Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ChatQueue.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chatService;

        public ChatController(IChatService chatService) => _chatService = chatService;


        [HttpPost("create")]
        public async Task<ActionResult<ChatSession>> Create(CancellationToken ct)
        {
            var session = await _chatService.CreateChatAsync(ct);
            return Ok(session);
        }

        [HttpPost("{sessionId:guid}/poll")]
        public IActionResult Poll(Guid sessionId)
        {
            _chatService.Poll(sessionId);
            return Ok(new { message = "poll ok" });
        }
    }
}
