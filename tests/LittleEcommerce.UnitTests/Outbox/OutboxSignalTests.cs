using System.Diagnostics;
using LittleEcommerce.Source.Infrastructure.Outbox;

namespace LittleEcommerce.UnitTests.Outbox;

public class OutboxSignalTests
{
    [Fact]
    public async Task WaitAsync_ShouldReturnRightAway_WhenNotified()
    {
        var signal = new OutboxSignal();
        signal.Notify();
        var watch = Stopwatch.StartNew();

        await signal.WaitAsync(TimeSpan.FromSeconds(10), CancellationToken.None);

        Assert.True(watch.Elapsed < TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task WaitAsync_ShouldReturnAfterTimeout_WhenNothingHappens()
    {
        var watch = Stopwatch.StartNew();

        await new OutboxSignal().WaitAsync(TimeSpan.FromMilliseconds(150), CancellationToken.None);

        Assert.True(watch.Elapsed >= TimeSpan.FromMilliseconds(100));
    }

    [Fact]
    public async Task Notify_ShouldCoalesceBurstsIntoOneWakeUp()
    {
        var signal = new OutboxSignal();
        signal.Notify();
        signal.Notify();
        signal.Notify();

        await signal.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken.None);
        var watch = Stopwatch.StartNew();
        await signal.WaitAsync(TimeSpan.FromMilliseconds(150), CancellationToken.None);

        Assert.True(watch.Elapsed >= TimeSpan.FromMilliseconds(100));
    }

    [Fact]
    public async Task WaitAsync_ShouldThrow_WhenHostIsStopping()
    {
        using var stopping = new CancellationTokenSource();
        await stopping.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new OutboxSignal().WaitAsync(TimeSpan.FromSeconds(5), stopping.Token));
    }
}
