namespace HollowPoint.Azure.Configuration;

public class AzureTableOptions
{
    public string ConnectionString { get; set; } = string.Empty;
    public bool EnableCaching { get; set; } = true;
    public TimeSpan CacheDuration { get; set; } = TimeSpan.FromDays(1);
}
