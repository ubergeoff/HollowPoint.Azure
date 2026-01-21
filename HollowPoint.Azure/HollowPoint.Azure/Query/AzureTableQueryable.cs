using Azure.Data.Tables;
using System.Collections;
using System.Linq.Expressions;

namespace HollowPoint.Azure.Query;

public class AzureTableQueryable<T> : IOrderedQueryable<T> where T : class, ITableEntity, new()
{
    private readonly TableClient _tableClient;
    private readonly Expression _expression;
    private readonly AzureTableQueryProvider _provider;

    public AzureTableQueryable(TableClient tableClient)
    {
        _tableClient = tableClient ?? throw new ArgumentNullException(nameof(tableClient));
        _provider = new AzureTableQueryProvider(tableClient);
        _expression = Expression.Constant(this);
    }

    public AzureTableQueryable(TableClient tableClient, Expression expression)
    {
        _tableClient = tableClient ?? throw new ArgumentNullException(nameof(tableClient));
        _provider = new AzureTableQueryProvider(tableClient);
        _expression = expression ?? throw new ArgumentNullException(nameof(expression));
    }

    public Type ElementType => typeof(T);
    public Expression Expression => _expression;
    public IQueryProvider Provider => _provider;

    public IEnumerator<T> GetEnumerator()
    {
        return _provider.Execute<IEnumerable<T>>(_expression).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
