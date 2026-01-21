using Azure;
using Azure.Data.Tables;

namespace HollowPoint.Azure;

public class BaseTableEntity : ITableEntity
{
    // ITableEntity required properties
    public string PartitionKey { get; set; } = string.Empty;
    public string RowKey { get; set; } = string.Empty;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    // Custom properties (preserved from original)
    public DateTime DateAdded { get; set; } = DateTime.UtcNow;
    public string Title { get; set; } = string.Empty;
    public bool IsDeleted { get; set; }

    public override string ToString() => Title;
}
