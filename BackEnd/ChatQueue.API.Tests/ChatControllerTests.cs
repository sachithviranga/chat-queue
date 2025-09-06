using ChatQueue.API.Controllers;
using ChatQueue.Application.Interfaces.Services;
using ChatQueue.Domain.Entities;
using ChatQueue.Domain.Enums;
using ChatQueue.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace ChatQueue.API.Tests
{
    public class ChatControllerTests
    {
        private readonly Mock<IChatService> _chatService = new();
        private readonly Mock<IDateTimeProvider> _clock = new();

        private  ChatController _chatController => new(_chatService.Object);


        [Fact]
        public async Task Create_ReturnsOk_WithSessionFromService()
        {
            // Arrange
            var now = new DateTime(2025, 09, 06, 10, 00, 00, DateTimeKind.Local);
            _clock.Setup(c => c.Now).Returns(now);

            var expectedSession = new ChatSession(Guid.NewGuid(), now, ChatSessionStatus.Queued);

            _chatService
                .Setup(s => s.CreateChatAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedSession);

            // Act
            var result = await _chatController.Create(CancellationToken.None);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var session = Assert.IsType<ChatSession>(okResult.Value);
            Assert.Equal(expectedSession.Id, session.Id);
        }

    }
}
