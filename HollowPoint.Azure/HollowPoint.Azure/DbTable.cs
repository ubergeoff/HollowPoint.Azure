using Azure;
using Azure.Data.Tables;
using HollowPoint.Azure.Configuration;
using HollowPoint.Azure.Interfaces;
using HollowPoint.Azure.Query;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace HollowPoint.Azure;

public partial class DbTable<T> : IDbTable<T> where T : class, ITableEntity, new()
{
    private readonly TableServiceClient _serviceClient;
    private readonly IMemoryCache _cache;
    private readonly AzureTableOptions _options;
    private TableClient? _tableClient;
    private string? _typeName;

    // Constructor for DI (only way to instantiate)
    public DbTable(
        TableServiceClient serviceClient,
        IMemoryCache cache,
        IOptions<AzureTableOptions> options)
    {
        _serviceClient = serviceClient ?? throw new ArgumentNullException(nameof(serviceClient));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    private TableClient Table
    {
        get
        {
            if (_tableClient == null)
            {
                _tableClient = GetTableClient(TypeName);
            }
            return _tableClient;
        }
    }

    private string TypeName
    {
        get
        {
            if (string.IsNullOrEmpty(_typeName))
            {
                string type = GetType().ToString();
                // Inline Helpers.QuickSplit logic
                var parts = type.Split(new[] { '.', '[', ']' }, StringSplitOptions.RemoveEmptyEntries);
                _typeName = parts[^1]; // C# 8+ index from end
            }
            return _typeName;
        }
    }

    private TableClient GetTableClient(string tableName)
    {
        var tableClient = _serviceClient.GetTableClient(tableName);

        if (EnsureTableExists(tableName, tableClient))
        {
            return tableClient;
        }

        throw new InvalidOperationException($"Unable to create Azure table '{tableName}'");
    }

    private bool EnsureTableExists(string tableName, TableClient tableClient)
    {
        if (!_options.EnableCaching)
        {
            tableClient.CreateIfNotExists();
            return true;
        }

        string cacheKey = $"table_exists_{tableName}";

        return _cache.GetOrCreate(cacheKey, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = _options.CacheDuration;

            try
            {
                tableClient.CreateIfNotExists();
                return true;
            }
            catch (RequestFailedException ex) when (ex.Status == 409)
            {
                // 409 Conflict = table already exists
                return true;
            }
        })!;
    }

    private static void ValidateEntity(T entity)
    {
        if (string.IsNullOrEmpty(entity.PartitionKey))
            throw new ArgumentNullException(nameof(entity.PartitionKey), "PartitionKey is required");

        if (string.IsNullOrEmpty(entity.RowKey))
            throw new ArgumentNullException(nameof(entity.RowKey), "RowKey is required");
    }

    // Synchronous CRUD operations
    public void Add(T entity)
    {
        ValidateEntity(entity);
        Table.AddEntity(entity);
    }

    public void Delete(T entity)
    {
        Table.DeleteEntity(entity.PartitionKey, entity.RowKey, entity.ETag);
    }

    public void Update(T entity)
    {
        ValidateEntity(entity);
        Table.UpdateEntity(entity, entity.ETag, TableUpdateMode.Replace);
    }

    public T? Get(string partitionKey, string rowKey)
    {
        try
        {
            var response = Table.GetEntity<T>(partitionKey, rowKey);
            return response.Value;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    public List<T> GetAllByPartitionKey(string partitionKey)
    {
        string filter = $"PartitionKey eq '{partitionKey}'";
        return Table.Query<T>(filter).ToList();
    }

    // Asynchronous CRUD operations
    public async Task<Response> AddAsync(T entity)
    {
        ValidateEntity(entity);
        return await Table.AddEntityAsync(entity);
    }

    public async Task<Response> UpdateAsync(T entity)
    {
        ValidateEntity(entity);
        return await Table.UpdateEntityAsync(entity, entity.ETag, TableUpdateMode.Replace);
    }

    public async Task<Response> DeleteAsync(T entity)
    {
        return await Table.DeleteEntityAsync(entity.PartitionKey, entity.RowKey, entity.ETag);
    }

    public async Task<T?> GetAsync(string partitionKey, string rowKey)
    {
        try
        {
            var response = await Table.GetEntityAsync<T>(partitionKey, rowKey);
            return response.Value;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    // Query operation - returns custom IQueryable
    public IQueryable<T> Query()
    {
        return new AzureTableQueryable<T>(Table);
    }
}
