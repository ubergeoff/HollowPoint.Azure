using HollowPoint.Azure.Query;
using HollowPoint.Azure.Tools;
using Azure.Data.Tables;

namespace HollowPoint.Azure.Extensions;

public static class QueryableExtension
{
    /// <summary>
    /// Converts an IQueryable from DbTable.Query() to an OData filter string.
    /// Only works with queries created by HollowPoint.Azure DbTable.
    /// </summary>
    public static string ToODataString<T>(this IQueryable<T> query) where T : class, ITableEntity, new()
    {
        if (query is AzureTableQueryable<T> azureQuery)
        {
            var translator = new ODataTranslator();
            return translator.Translate(azureQuery.Expression);
        }

        throw new NotSupportedException(
            "ToODataString only works with IQueryable instances returned from DbTable<T>.Query(). " +
            "The provided query is not an AzureTableQueryable.");
    }
}
