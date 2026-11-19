using LittleEcommerce.Source.Common.Enums;
using LittleEcommerce.Source.Data.Entities;
using LittleEcommerce.Source.Data.Repositories.Interfaces;
using LittleEcommerce.Source.Infrastructure.Outbox;
using LittleEcommerce.Source.Shared.Options;
using LittleEcommerce.UnitTests.Support;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Moq;
using MsOptions = Microsoft.Extensions.Options.Options;

namespace LittleEcommerce.UnitTests.Outbox;

public class OutboxDispatcherTests
{
    private const string KnownType = "email.teste";
    private static readonly DateTimeOffset Now = new(2026, 11, 19, 12, 0, 0, TimeSpan.Zero);

    private readonly InlineUnitOfWork _unitOfWork = new();
    private readonly Mock<IOutboxRepository> _repository = new();
    private readonly Mock<IOutboxMessageHandler> _handler = new();
    private readonly OutboxOptions _settings = new() { MaxAttempts = 3, BaseRetryDelaySeconds = 30, MaxRetryDelayMinutes = 60, BatchSize = 10, LeaseSeconds = 120 };

    public OutboxDispatcherTests()
    {
        _handler.SetupGet(h => h.Type).Returns(KnownType);
    }

    private OutboxDispatcher CreateDispatcher() => new(
        _unitOfWork,
        _repository.Object,
        [_handler.Object],
        MsOptions.Create(_settings),
        new FakeTimeProvider(Now),
        NullLogger<OutboxDispatcher>.Instance);

    private OutboxMessage Claimed(int attempts = 1, string type = KnownType)
    {
        var message = new OutboxMessage
        {
            Type = type,
            Status = OutboxStatus.Processing,
            Attempts = attempts,
            LockedUntil = Now.UtcDateTime.AddMinutes(2)
        };

        _repository.Setup(r => r.ClaimDueAsync(Now.UtcDateTime, TimeSpan.FromSeconds(120), 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync([message]);

        return message;
    }

    [Fact]
    public async Task DispatchAsync_ShouldMarkAsSent_WhenHandlerSucceeds()
    {
        var message = Claimed();

        var processed = await CreateDispatcher().DispatchAsync(CancellationToken.None);

        Assert.Equal(1, processed);
        Assert.Equal(OutboxStatus.Sent, message.Status);
        Assert.Equal(Now.UtcDateTime, message.ProcessedAt);
        Assert.Null(message.LockedUntil);
        Assert.Null(message.LastError);
    }

    [Fact]
    public async Task DispatchAsync_ShouldScheduleRetryWithBackoff_WhenHandlerFails()
    {
        var message = Claimed(attempts: 2);
        _handler.Setup(h => h.HandleAsync(message, It.IsAny<CancellationToken>())).ThrowsAsync(new IOException("smtp fora"));

        await CreateDispatcher().DispatchAsync(CancellationToken.None);

        Assert.Equal(OutboxStatus.Pending, message.Status);
        Assert.Equal(Now.UtcDateTime.AddSeconds(60), message.NextAttemptAt);
        Assert.Contains("smtp fora", message.LastError);
        Assert.Null(message.LockedUntil);
    }

    [Fact]
    public async Task DispatchAsync_ShouldDeadLetter_WhenAttemptsAreExhausted()
    {
        var message = Claimed(attempts: 3);
        _handler.Setup(h => h.HandleAsync(message, It.IsAny<CancellationToken>())).ThrowsAsync(new IOException("smtp fora"));

        await CreateDispatcher().DispatchAsync(CancellationToken.None);

        Assert.Equal(OutboxStatus.DeadLetter, message.Status);
    }

    [Fact]
    public async Task DispatchAsync_ShouldDeadLetterImmediately_WhenTypeHasNoHandler()
    {
        var message = Claimed(type: "tipo.desconhecido");

        await CreateDispatcher().DispatchAsync(CancellationToken.None);

        Assert.Equal(OutboxStatus.DeadLetter, message.Status);
        Assert.Contains("tipo.desconhecido", message.LastError);
    }

    [Fact]
    public async Task DispatchAsync_ShouldLeaveMessageLocked_WhenShutdownInterruptsSend()
    {
        var message = Claimed();
        using var shutdown = new CancellationTokenSource();
        _handler.Setup(h => h.HandleAsync(message, It.IsAny<CancellationToken>()))
            .Returns(async (OutboxMessage _, CancellationToken token) =>
            {
                await shutdown.CancelAsync();
                token.ThrowIfCancellationRequested();
            });

        await CreateDispatcher().DispatchAsync(shutdown.Token);

        Assert.Equal(OutboxStatus.Processing, message.Status);
        Assert.NotNull(message.LockedUntil);
    }

    [Theory]
    [InlineData(1, 30)]
    [InlineData(2, 60)]
    [InlineData(3, 120)]
    [InlineData(10, 3600)]
    public void RetryDelay_ShouldGrowExponentiallyUpToTheCap(int attempts, int expectedSeconds)
    {
        Assert.Equal(TimeSpan.FromSeconds(expectedSeconds), OutboxDispatcher.RetryDelay(attempts, _settings));
    }
}
