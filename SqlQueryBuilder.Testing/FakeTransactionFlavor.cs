using SqlQueryBuilder.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SqlQueryBuilder.Testing;

public sealed class FakeTransactionFlavor : ISqlTransactionFlavor {
    // --- Implementation of the fake ---
    private readonly FakeSqlFlavor _rootSqlFlavor;

    public List<string> PendingQueries { get; } = new List<string>();

    public object? NextSingleResult {
        set => _rootSqlFlavor.NextSingleResult = value;
    }

    public IReadOnlyList<object?>? NextResult {
        get => _rootSqlFlavor.NextResult;
        set => _rootSqlFlavor.NextResult = value;
    }

    public FakeTransactionFlavor(FakeSqlFlavor rootSqlFlavor) {
        _rootSqlFlavor = rootSqlFlavor;
    }

    // --- Implementation of the interface ---
    public Task<bool> ExecuteAsync(string query, IDictionary<string, object?> parameters) {
        return Task.FromResult(Execute(query, parameters));
    }
    public bool Execute(string query, IDictionary<string, object?> parameters) {
        PendingQueries.Add(query);
        return true;
    }

    public Task<IReadOnlyList<T>> ExecuteWithResultsAsync<T>(string query, IDictionary<string, object?> parameters) {
        return Task.FromResult(ExecuteWithResults<T>(query, parameters));
    }
    public IReadOnlyList<T> ExecuteWithResults<T>(string query, IDictionary<string, object?> parameters) {
        PendingQueries.Add(query);
        return NextResult?.Cast<T>().ToList().AsReadOnly() ?? new List<T>().AsReadOnly();
    }

    public Task<ISqlTransactionFlavor> BeginTransactionAsync() {
        throw new InvalidOperationException("The transaction is already started.");
    }
    public ISqlTransactionFlavor BeginTransaction() {
        throw new InvalidOperationException("The transaction is already started.");
    }

    public void Dispose() { } // No need to do anything special here

    public Task CommitAsync() {
        Commit();
        return Task.CompletedTask;
    }
    public void Commit() => _rootSqlFlavor.ExecutedQueries.AddRange(PendingQueries);

    public Task RollbackAsync() {
        Rollback();
        return Task.CompletedTask;
    }
    public void Rollback() => PendingQueries.Clear();

    public string WrapFieldName(string fieldName) => _rootSqlFlavor.WrapFieldName(fieldName);

    public (string queryPart, object?[] parameters) Skip(long skipOffset) => _rootSqlFlavor.Skip(skipOffset);
    public (string queryPart, object?[] parameters) Take(int takeLimit) => _rootSqlFlavor.Take(takeLimit);
}
