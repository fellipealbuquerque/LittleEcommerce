using LittleEcommerce.Source.Application.Exceptions;
using LittleEcommerce.Source.Application.Interfaces.Messaging;
using LittleEcommerce.Source.Application.Services;
using LittleEcommerce.Source.Common.Enums;
using LittleEcommerce.Source.Common.Errors;
using LittleEcommerce.Source.Data.Entities;
using LittleEcommerce.Source.Data.Repositories.Interfaces;
using LittleEcommerce.UnitTests.Support;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Moq;

namespace LittleEcommerce.UnitTests.Outbox;

public class OutboxAdminServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 19, 12, 0, 0, TimeSpan.Zero);

    private readonly InlineUnitOfWork _unitOfWork = new();
    private readonly Mock<IOutboxRepository> _repository = new();
    private readonly Mock<IOutboxSignal> _signal = new();

    private OutboxAdminService CreateService() => new(
        _unitOfWork,
        _repository.Object,
        _signal.Object,
        new FakeTimeProvider(Now),
        NullLogger<OutboxAdminService>.Instance);

    private OutboxMessage Stored(OutboxStatus status)
    {
        var message = new OutboxMessage
        {
            Type = "email.welcome",
            Status = status,
            Attempts = 5,
            Payload = "{\"email\":\"ana@loja.com\"}",
            LastError = "SocketException: recusado"
        };

        _repository.Setup(r => r.GetAsync(message.Id, It.IsAny<CancellationToken>())).ReturnsAsync(message);
        return message;
    }

    [Fact]
    public async Task RetryAsync_ShouldRequeueDeadLetterAndWakeDispatcher()
    {
        var message = Stored(OutboxStatus.DeadLetter);

        var result = await CreateService().RetryAsync(message.Id);

        Assert.Equal(OutboxStatus.Pending, result.Status);
        Assert.Equal(0, message.Attempts);
        Assert.Equal(Now.UtcDateTime, message.NextAttemptAt);
        Assert.Equal("ana@loja.com", result.Payload.GetProperty("email").GetString());
        Assert.Equal(1, _unitOfWork.Commits);
        _signal.Verify(s => s.Notify(), Times.Once);
    }

    [Fact]
    public async Task RetryAsync_ShouldAllowResendingAlreadySentMessage()
    {
        var message = Stored(OutboxStatus.Sent);

        var result = await CreateService().RetryAsync(message.Id);

        Assert.Equal(OutboxStatus.Pending, result.Status);
    }

    [Fact]
    public async Task RetryAsync_ShouldRefuseMessageBeingProcessed()
    {
        var message = Stored(OutboxStatus.Processing);

        var ex = await Assert.ThrowsAsync<ConflictException>(() => CreateService().RetryAsync(message.Id));

        Assert.Equal(ErrorCodes.OutboxMessageProcessing, ex.Code);
        _signal.Verify(s => s.Notify(), Times.Never);
    }

    [Fact]
    public async Task RetryDeadLettersAsync_ShouldOnlyNotify_WhenSomethingWasRequeued()
    {
        _repository.Setup(r => r.RequeueDeadLettersAsync(null, Now.UtcDateTime, It.IsAny<CancellationToken>())).ReturnsAsync(0);

        var result = await CreateService().RetryDeadLettersAsync("  ");

        Assert.Equal(0, result.Requeued);
        _signal.Verify(s => s.Notify(), Times.Never);
    }

    [Fact]
    public async Task SummaryAsync_ShouldFillMissingStatusesWithZero()
    {
        _repository.Setup(r => r.CountByStatusAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<OutboxStatus, int> { [OutboxStatus.Sent] = 7, [OutboxStatus.DeadLetter] = 1 });

        var summary = await CreateService().SummaryAsync();

        Assert.Equal(0, summary.Pending);
        Assert.Equal(7, summary.Sent);
        Assert.Equal(1, summary.DeadLetter);
    }
}
