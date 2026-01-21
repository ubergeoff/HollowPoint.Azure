using Azure.Data.Tables;
using HollowPoint.Azure.Tools;
using System.Linq.Expressions;
using System.Reflection;

namespace HollowPoint.Azure.Query;

public class AzureTableQueryProvider : IQueryProvider
{
    private readonly TableClient _tableClient;

    public AzureTableQueryProvider(TableClient tableClient)
    {
        _tableClient = tableClient ?? throw new ArgumentNullException(nameof(tableClient));
    }

    public IQueryable CreateQuery(Expression expression)
    {
        Type elementType = expression.Type.GetGenericArguments()[0];
        try
        {
            return (IQueryable)Activator.CreateInstance(
                typeof(AzureTableQueryable<>).MakeGenericType(elementType),
                new object[] { _tableClient, expression })!;
        }
        catch (TargetInvocationException ex)
        {
            throw ex.InnerException ?? ex;
        }
    }

    public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
    {
        // Validate TElement satisfies constraints at runtime
        var elementType = typeof(TElement);
        if (!elementType.IsClass || !typeof(ITableEntity).IsAssignableFrom(elementType))
        {
            throw new InvalidOperationException(
                $"Type {elementType.Name} must be a class that implements ITableEntity");
        }

        if (elementType.GetConstructor(Type.EmptyTypes) == null)
        {
            throw new InvalidOperationException(
                $"Type {elementType.Name} must have a parameterless constructor");
        }

        return (IQueryable<TElement>)Activator.CreateInstance(
            typeof(AzureTableQueryable<>).MakeGenericType(elementType),
            new object[] { _tableClient, expression })!;
    }

    public object? Execute(Expression expression)
    {
        return Execute<object>(expression);
    }

    public TResult Execute<TResult>(Expression expression)
    {
        // Translate LINQ expression to OData filter using existing ODataTranslator
        var translator = new ODataTranslator();
        string filter = translator.Translate(expression);

        // Get element type from expression
        Type elementType = GetElementType(expression.Type);

        // Execute query with Azure SDK using reflection to handle generic method
        var queryMethod = typeof(TableClient).GetMethod(nameof(TableClient.Query))!
            .MakeGenericMethod(elementType);

        // Call TableClient.Query<T>(filter, maxPerPage, select, cancellationToken)
        var results = queryMethod.Invoke(
            _tableClient,
            new object?[] { filter, translator.Take, null, default(CancellationToken) }) as IEnumerable<object>;

        // Handle Skip if present
        if (translator.Skip.HasValue && results != null)
        {
            results = results.Skip(translator.Skip.Value);
        }

        // Convert to appropriate return type
        if (typeof(TResult).IsGenericType)
        {
            var genericTypeDef = typeof(TResult).GetGenericTypeDefinition();
            if (genericTypeDef == typeof(IEnumerable<>) || genericTypeDef == typeof(IQueryable<>))
            {
                return (TResult)results!;
            }
        }

        // Default: convert to list
        var list = results?.ToList() ?? new List<object>();
        return (TResult)(object)list;
    }

    private static Type GetElementType(Type type)
    {
        if (type.IsGenericType)
        {
            var args = type.GetGenericArguments();
            if (args.Length > 0)
            {
                return args[0];
            }
        }

        // Fallback: try to find IEnumerable<T>
        var enumerableType = type.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));

        if (enumerableType != null)
        {
            return enumerableType.GetGenericArguments()[0];
        }

        throw new InvalidOperationException($"Cannot determine element type from {type.Name}");
    }
}
