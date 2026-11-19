using LittleEcommerce.Source.Infrastructure.Auditing;
using Moq;

namespace LittleEcommerce.UnitTests.Auditing;

public class AuditBufferTests
{
    private readonly Mock<IAuditWriter> _writer = new();

    private static List<AuditEntry> Entries(int count) =>
        Enumerable.Range(0, count).Select(i => new AuditEntry { Action = $"a{i}" }).ToList();

    [Fact]
    public void Saved_ShouldPublishImmediately_WhenOutsideTransaction()
    {
        var buffer = new AuditBuffer(_writer.Object);
        buffer.Stage(Entries(2));

        buffer.Saved(insideTransaction: false);

        _writer.Verify(w => w.Enqueue(It.IsAny<AuditEntry>()), Times.Exactly(2));
    }

    [Fact]
    public void Saved_ShouldWaitForCommit_WhenInsideTransaction()
    {
        var buffer = new AuditBuffer(_writer.Object);
        buffer.Stage(Entries(1));
        buffer.Saved(insideTransaction: true);
        buffer.Stage(Entries(2));
        buffer.Saved(insideTransaction: true);

        _writer.Verify(w => w.Enqueue(It.IsAny<AuditEntry>()), Times.Never);

        buffer.Publish();

        _writer.Verify(w => w.Enqueue(It.IsAny<AuditEntry>()), Times.Exactly(3));
    }

    [Fact]
    public void Discard_ShouldDropEverything_WhenTransactionRollsBack()
    {
        var buffer = new AuditBuffer(_writer.Object);
        buffer.Stage(Entries(2));
        buffer.Saved(insideTransaction: true);

        buffer.Discard();
        buffer.Publish();

        _writer.Verify(w => w.Enqueue(It.IsAny<AuditEntry>()), Times.Never);
    }

    [Fact]
    public void DiscardStaged_ShouldDropOnlyFailedSave()
    {
        var buffer = new AuditBuffer(_writer.Object);
        buffer.Stage(Entries(1));
        buffer.Saved(insideTransaction: true);
        buffer.Stage(Entries(5));

        buffer.DiscardStaged();
        buffer.Publish();

        _writer.Verify(w => w.Enqueue(It.IsAny<AuditEntry>()), Times.Once);
    }
}
