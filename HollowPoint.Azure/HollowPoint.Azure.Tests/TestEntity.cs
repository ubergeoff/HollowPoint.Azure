using Azure;
using Azure.Data.Tables;

namespace HollowPoint.Azure.Tests;

public class TestEntity : ITableEntity
{
    public string PartitionKey { get; set; } = string.Empty;
    public string RowKey { get; set; } = string.Empty;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public string Title { get; set; } = string.Empty;
    public int Count { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime DateAdded { get; set; } = DateTime.UtcNow;
}
