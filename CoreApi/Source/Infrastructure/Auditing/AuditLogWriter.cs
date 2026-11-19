using System.Threading.Channels;
using LittleEcommerce.Source.Infrastructure.Mongo;
using LittleEcommerce.Source.Shared.Options;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace LittleEcommerce.Source.Infrastructure.Auditing;

public sealed class AuditLogWriter : BackgroundService, IAuditWriter
{
    private const int DuplicateKey = 11000;
    private const int ShutdownAttempts = 3;
    private static readonly TimeSpan MaxBackoff = TimeSpan.FromSeconds(30);

    private readonly IMongoDatabase _database;
    private readonly IMongoCollection<AuditEntry> _collection;
    private readonly AuditLoggingOptions _options;
    private readonly ILogger<AuditLogWriter> _logger;
    private readonly Channel<AuditEntry> _channel;
    private bool _indexesReady;

    public AuditLogWriter(IMongoDatabase database, IOptions<AuditLoggingOptions> options, ILogger<AuditLogWriter> logger)
    {
        _database = database;
        _options = options.Value;
        _logger = logger;
        _collection = database.GetCollection<AuditEntry>(_options.Collection);
        _channel = Channel.CreateBounded<AuditEntry>(new BoundedChannelOptions(Math.Max(100, _options.QueueCapacity))
        {
            SingleReader = true,
            FullMode = BoundedChannelFullMode.Wait
        });
    }

    public void Enqueue(AuditEntry entry)
    {
        if (!_options.Enabled || _channel.Writer.TryWrite(entry))
            return;

        _logger.LogError(
            "Registro de auditoria descartado (fila cheia ou encerrada) | Kind: {Kind} | Action: {Action} | EntityType: {EntityType} | EntityId: {EntityId} | ActorId: {ActorId}",
            entry.Kind,
            entry.Action,
            entry.EntityType,
            entry.EntityId,
            entry.Actor?.UserId);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _channel.Writer.TryComplete();
        await base.StopAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var batch = new List<AuditEntry>(_options.BatchSize);

        while (await _channel.Reader.WaitToReadAsync(CancellationToken.None))
        {
            while (batch.Count < _options.BatchSize && _channel.Reader.TryRead(out var entry))
                batch.Add(entry);

            await PersistAsync(batch, stoppingToken);
            batch.Clear();
        }
    }

    private async Task PersistAsync(List<AuditEntry> batch, CancellationToken stoppingToken)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await EnsureIndexesAsync();
                await _collection.InsertManyAsync(batch, new InsertManyOptions { IsOrdered = false });
                return;
            }
            catch (MongoBulkWriteException ex) when (ex.WriteErrors.All(e => e.Code == DuplicateKey))
            {
                return;
            }
            catch (Exception ex)
            {
                if (stoppingToken.IsCancellationRequested && attempt >= ShutdownAttempts)
                {
                    _logger.LogError(ex, "{Count} registros de auditoria perdidos no desligamento", batch.Count);
                    return;
                }

                _logger.LogWarning(ex, "Falha ao gravar {Count} registros de auditoria (tentativa {Attempt})", batch.Count, attempt);
                await DelayAsync(attempt, stoppingToken);
            }
        }
    }

    private static async Task DelayAsync(int attempt, CancellationToken stoppingToken)
    {
        var delay = TimeSpan.FromMilliseconds(Math.Min(MaxBackoff.TotalMilliseconds, 250 * Math.Pow(2, attempt)));

        try
        {
            await Task.Delay(delay, stoppingToken);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task EnsureIndexesAsync()
    {
        if (_indexesReady)
            return;

        await MongoIndexes.EnsureTtlAsync(_database, _collection, "timestamp", _options.RetentionDays);

        var keys = Builders<AuditEntry>.IndexKeys;

        await _collection.Indexes.CreateManyAsync(
        [
            new CreateIndexModel<AuditEntry>(
                keys.Ascending("kind").Descending("timestamp"),
                new CreateIndexOptions { Name = "kind_timestamp" }),
            new CreateIndexModel<AuditEntry>(
                keys.Ascending("actor.userId").Descending("timestamp"),
                new CreateIndexOptions { Name = "actor_timestamp", Sparse = true }),
            new CreateIndexModel<AuditEntry>(
                keys.Ascending("entityType").Ascending("entityId").Descending("timestamp"),
                new CreateIndexOptions { Name = "entity_timestamp", Sparse = true }),
            new CreateIndexModel<AuditEntry>(
                keys.Ascending("correlationId"),
                new CreateIndexOptions { Name = "correlation_id", Sparse = true })
        ]);

        _indexesReady = true;
    }
}
