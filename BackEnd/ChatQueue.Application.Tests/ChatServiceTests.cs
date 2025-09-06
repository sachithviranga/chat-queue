using ChatQueue.Application.Interfaces.Repositories;
using ChatQueue.Application.Services;
using ChatQueue.Domain.Configuration;
using ChatQueue.Domain.Entities;
using ChatQueue.Domain.Enums;
using ChatQueue.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace ChatQueue.Application.Tests.Services
{
    public class ChatServiceTests
    {
        private readonly Mock<IChatQueueService> _queueMock = new();
        private readonly Mock<ISessionQueueRepository> _sessionQueueRepoMock = new();
        private readonly Mock<ITeamRepository> _teamRepoMock = new();
        private readonly Mock<IPollingRepository> _pollingRepoMock = new();
        private readonly Mock<IDateTimeProvider> _clockMock = new();
        private readonly ChatConfiguration _cfg = new() { AgentBaseConcurrency = 10, QueueMultiplier = 1.5 };
        private readonly Mock<ILogger<ChatService>> _loggerMock = new();

        private ChatService chatService =>
            new(_queueMock.Object, _sessionQueueRepoMock.Object, _teamRepoMock.Object, _pollingRepoMock.Object, _clockMock.Object, _cfg , _loggerMock.Object);


        [Fact]
        public async Task CreateChatAsync_ReturnsRefused_WhenNoEligibleTeams()
        {
            // Arrange
            var now = DateTime.Now;
            _clockMock.Setup(c => c.Now).Returns(now);
            _teamRepoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            // Act
            var result = await chatService.CreateChatAsync();

            // Assert
            Assert.Equal(ChatSessionStatus.Refused, result.Status);
            Assert.Equal(now, result.CreatedAt);
        }

        [Fact]
        public async Task CreateChatAsync_EnqueuesSession_WhenQueueEmpty()
        {
            // Arrange
            var now = DateTime.Now;
            _clockMock.Setup(c => c.Now).Returns(now);
            _teamRepoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(ChatServiceTestData.Teams);
            _queueMock.Setup(q => q.Count).Returns(0);
            _sessionQueueRepoMock.Setup(r => r.Count()).Returns(0);

            // Act
            var result = await chatService.CreateChatAsync();

            // Assert
            _queueMock.Verify(q => q.Enqueue(It.IsAny<ChatSession>()), Times.Once);
            Assert.Equal(ChatSessionStatus.Queued, result.Status);
            Assert.Equal(now, result.CreatedAt);
        }

        [Fact]
        public async Task CreateChatAsync_Refuses_WhenQueueFull_AndNoOverflow()
        {
            // Arrange
            var now = new DateTime(2025, 09, 06, 10, 00, 00, DateTimeKind.Local);
            _clockMock.Setup(c => c.Now).Returns(now);
            _teamRepoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(ChatServiceTestData.Teams);
            _queueMock.Setup(q => q.Count).Returns(0);
            _sessionQueueRepoMock.Setup(r => r.Count()).Returns(ChatServiceTestData.MaxQueueLimit);
            _teamRepoMock.Setup(r => r.GetOverflowAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync((Team?)null);

            // Act
            var result = await chatService.CreateChatAsync();

            // Assert
            Assert.Equal(ChatSessionStatus.Refused, result.Status);
            Assert.Equal(now, result.CreatedAt);
        }

        [Fact]
        public async Task CreateChatAsync_UsesOverflow_WhenQueueFull_AndOverflowAvailable()
        {
            //Arrange
            var now = DateTime.Now;
            _clockMock.Setup(c => c.Now).Returns(now);
            _teamRepoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(ChatServiceTestData.Teams);
            _queueMock.SetupSequence(q => q.Count).Returns(0);
            _sessionQueueRepoMock.Setup(r => r.Count()).Returns(ChatServiceTestData.MaxQueueLimit);
            _teamRepoMock.Setup(r => r.GetOverflowAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(ChatServiceTestData.OverflowTeam);

            // Act
            var result = await chatService.CreateChatAsync();

            //Assert
            _queueMock.Verify(q => q.Enqueue(It.IsAny<ChatSession>()), Times.Once);
            Assert.Equal(ChatSessionStatus.Queued, result.Status);
            Assert.Equal(now, result.CreatedAt);
        }

        [Fact]
        public void Poll_UpdatesPolling()
        {
            // Arrange
            var now = DateTime.Now;
            _clockMock.Setup(c => c.Now).Returns(now);
            var sessionId = Guid.NewGuid();

            // Act
            chatService.Poll(sessionId);

            // Assert
            _pollingRepoMock.Verify(p => p.UpdatePoll(sessionId, now), Times.Once);
        }
    }
}
