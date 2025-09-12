using ChatQueue.Application.Interfaces.Repositories;
using ChatQueue.Application.Services;
using ChatQueue.Domain.Configuration;
using ChatQueue.Domain.Entities;
using ChatQueue.Domain.Enums;
using ChatQueue.Domain.Exceptions;
using ChatQueue.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace ChatQueue.Application.Tests
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
            _sessionQueueRepoMock.Setup(r => r.CountAsync(It.IsAny<CancellationToken>())).ReturnsAsync(0);

            // Act
            var result = await chatService.CreateChatAsync();

            // Assert
            _queueMock.Verify(q => q.Enqueue(It.IsAny<ChatSession>()), Times.Once);
            Assert.Equal(ChatSessionStatus.Queued, result.Status);
            Assert.Equal(now, result.CreatedAt);
        }

        [Fact]
        public async Task CreateChatAsync_UsesOverflow_WhenQueueFull_AndOverflowAvailable_ShiftMoring()
        {
            //Arrange
            var now = new DateTime(2025, 09, 06, 10, 00, 00, DateTimeKind.Local);
            _clockMock.Setup(c => c.Now).Returns(now);
            _teamRepoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(ChatServiceTestData.Teams);
            _queueMock.SetupSequence(q => q.Count).Returns(0);
            _sessionQueueRepoMock.Setup(r => r.CountAsync(It.IsAny<CancellationToken>())).ReturnsAsync(ChatServiceTestData.MaxQueueLimit);
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
        public async Task CreateChatAsync_ShouldThrowQueueFullException_WhenQueueFull_AndNoOverflow_ShiftMoring()
        {
            // Arrange
            var now = new DateTime(2025, 09, 06, 10, 00, 00, DateTimeKind.Local);
            _clockMock.Setup(c => c.Now).Returns(now);
            _teamRepoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(ChatServiceTestData.Teams);
            _queueMock.Setup(q => q.Count).Returns(0);
            _sessionQueueRepoMock.Setup(r => r.CountAsync(It.IsAny<CancellationToken>())).ReturnsAsync(ChatServiceTestData.MaxQueueLimit);
            _teamRepoMock.Setup(r => r.GetOverflowAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync((Team?)null);

            // Act & Assert
            await Assert.ThrowsAsync<QueueFullException>(() =>
                chatService.CreateChatAsync(CancellationToken.None)
            );
        }

        [Fact]
        public async Task CreateChatAsync_ShouldThrowQueueFullException_WhenQueueFull_ShiftNight()
        {
            // Arrange
            var now = new DateTime(2025, 09, 06, 20, 00, 00, DateTimeKind.Local);
            _clockMock.Setup(c => c.Now).Returns(now);
            _teamRepoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(ChatServiceTestData.Teams);
            _queueMock.Setup(q => q.Count).Returns(0);
            _sessionQueueRepoMock.Setup(r => r.CountAsync(It.IsAny<CancellationToken>())).ReturnsAsync(ChatServiceTestData.MaxQueueLimit);


            // Act & Assert
            await Assert.ThrowsAsync<QueueFullException>(() =>
                chatService.CreateChatAsync(CancellationToken.None)
            );
        }

        [Fact]
        public async Task PollAsync_ReturnsFalse_WhenSessionDoesNotExist()
        {
            // Arrange
            var sessionId = Guid.NewGuid();
            _sessionQueueRepoMock.Setup(r => r.IsExistAsync(sessionId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            // Act
            var result = await chatService.PollAsync(sessionId);

            // Assert
            Assert.False(result);
            _pollingRepoMock.Verify(p => p.UpdatePollAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task PollAsync_ReturnsTrue_AndUpdatesPoll_WhenSessionExists()
        {
            // Arrange
            var sessionId = Guid.NewGuid();
            var now = DateTime.UtcNow;
            _sessionQueueRepoMock.Setup(r => r.IsExistAsync(sessionId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            _clockMock.Setup(c => c.Now).Returns(now);


            // Act
            var result = await chatService.PollAsync(sessionId);

            // Assert
            Assert.True(result);
            _pollingRepoMock.Verify(p => p.UpdatePollAsync(sessionId, now, It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
