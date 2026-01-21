using Azure;
using Azure.Data.Tables;

namespace HollowPoint.Azure.Interfaces;

public interface IDbTable<T> where T : class, ITableEntity, new()
{
    // Synchronous CRUD
    void Add(T entity);
    void Delete(T entity);
    void Update(T entity);
    T? Get(string partitionKey, string rowKey);
    List<T> GetAllByPartitionKey(string partitionKey);

    // Asynchronous CRUD
    Task<Response> AddAsync(T entity);
    Task<Response> UpdateAsync(T entity);
    Task<Response> DeleteAsync(T entity);
    Task<T?> GetAsync(string partitionKey, string rowKey);

    // Query operations
    IQueryable<T> Query();
}
