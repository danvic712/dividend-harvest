using System.Collections.Concurrent;
using System.Data.Common;
using DividendHarvest.Domain.Contracts;
using Microsoft.EntityFrameworkCore;

namespace DividendHarvest.Infrastructure.Repositories;

internal sealed class EFUow(DividendHarvestDbContext dbContext) : IUow
{
    private readonly ConcurrentDictionary<Type, Lazy<object>> repositories = new();

    public IRepository<TEntity> Get<TEntity>()
        where TEntity : class
    {
        var entityType = typeof(TEntity);
        var repository = repositories.GetOrAdd(
            entityType,
            _ => new Lazy<object>(
                () => new EFRepository<TEntity>(dbContext),
                isThreadSafe: true));

        return (IRepository<TEntity>)repository.Value;
    }

    public Task<int> CommitAsync(CancellationToken cancellationToken = default)
        => dbContext.SaveChangesAsync(cancellationToken);

    public Task<bool> CanConnectAsync(CancellationToken cancellationToken = default)
        => dbContext.Database.CanConnectAsync(cancellationToken);

    public async Task EnsureCreatedAsync(CancellationToken cancellationToken = default)
    {
        await dbContext.Database.EnsureCreatedAsync(cancellationToken);
        if (!dbContext.Database.IsSqlite())
        {
            return;
        }

        var connection = dbContext.Database.GetDbConnection();
        var shouldCloseConnection = connection.State != System.Data.ConnectionState.Open;
        if (shouldCloseConnection)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            await EnsureColumnAsync(
                connection,
                "portfolios",
                "currency_code",
                "TEXT NOT NULL DEFAULT 'CNY'",
                cancellationToken);
            await EnsureColumnAsync(
                connection,
                "recommendation_snapshots",
                "observed_price_zone_code",
                "TEXT NULL",
                cancellationToken);
            await EnsureColumnAsync(
                connection,
                "recommendation_snapshots",
                "price_zone_confirmed",
                "INTEGER NOT NULL DEFAULT 0",
                cancellationToken);
            await EnsureUniqueCashLedgerSourceIndexAsync(connection, cancellationToken);
        }
        finally
        {
            if (shouldCloseConnection)
            {
                await connection.CloseAsync();
            }
        }
    }

    private static async Task EnsureColumnAsync(
        DbConnection connection,
        string tableName,
        string columnName,
        string columnDefinition,
        CancellationToken cancellationToken)
    {
        await using var columnCommand = connection.CreateCommand();
        columnCommand.CommandText = $"PRAGMA table_info(\"{tableName}\")";
        await using var reader = await columnCommand.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            if (string.Equals(reader.GetString(1), columnName, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
        }

        await reader.CloseAsync();
        await using var alterCommand = connection.CreateCommand();
        alterCommand.CommandText =
            $"ALTER TABLE \"{tableName}\" ADD COLUMN \"{columnName}\" {columnDefinition}";
        await alterCommand.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task EnsureUniqueCashLedgerSourceIndexAsync(
        DbConnection connection,
        CancellationToken cancellationToken)
    {
        await using var indexCommand = connection.CreateCommand();
        indexCommand.CommandText =
            "CREATE UNIQUE INDEX IF NOT EXISTS "
            + "uq_cash_ledger_entries_portfolio_id_source_record_id "
            + "ON cash_ledger_entries (portfolio_id, source_record_id) "
            + "WHERE source_record_id IS NOT NULL";
        await indexCommand.ExecuteNonQueryAsync(cancellationToken);
    }
}
