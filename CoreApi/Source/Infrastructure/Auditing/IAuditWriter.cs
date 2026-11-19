namespace LittleEcommerce.Source.Infrastructure.Auditing;

public interface IAuditWriter
{
    void Enqueue(AuditEntry entry);
}
