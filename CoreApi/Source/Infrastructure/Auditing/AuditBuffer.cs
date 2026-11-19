namespace LittleEcommerce.Source.Infrastructure.Auditing;

public sealed class AuditBuffer(IAuditWriter writer)
{
    private List<AuditEntry> _staged = [];
    private readonly List<AuditEntry> _awaitingCommit = [];

    public void Stage(List<AuditEntry> entries) => _staged = entries;

    public void DiscardStaged() => _staged = [];

    public void Saved(bool insideTransaction)
    {
        _awaitingCommit.AddRange(_staged);
        _staged = [];

        if (!insideTransaction)
            Publish();
    }

    public void Publish()
    {
        foreach (var entry in _awaitingCommit)
            writer.Enqueue(entry);

        _awaitingCommit.Clear();
    }

    public void Discard()
    {
        _staged = [];
        _awaitingCommit.Clear();
    }
}
