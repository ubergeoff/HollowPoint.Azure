using HollowPoint.Azure.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace HollowPoint.Azure.Extensions
{
    public static class QueryableExtension
    {
        /// <summary>
        /// For an Entity Framework IQueryable, returns the SQL and Parameters.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="query"></param>
        /// <returns></returns>
        public static string ToODataString<T>(this IQueryable<T> query)
        {
            //Microsoft.WindowsAzure.Storage.Table.TableQuery<T>           

            var internalQueryFields = query.GetType().GetFields(
                       BindingFlags.Instance |
                       BindingFlags.NonPublic);

            var internalQueryField = internalQueryFields.Where(f => f.Name.Equals("queryExpression")).FirstOrDefault();
            var internalQuery = internalQueryField.GetValue(query);

            var type = internalQuery.GetType();

            var objectQueryFields = internalQuery.GetType().GetFields(
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);

            var objectQueryField = objectQueryFields.Where(f => f.Name.Equals("_arguments")).FirstOrDefault();

            var item = objectQueryField.GetValue(internalQuery) as IReadOnlyCollection<Expression>;

            var quote = item.Where(t => t.NodeType == ExpressionType.Quote).FirstOrDefault() as UnaryExpression;

            var translator = new ODataTranslator();
            return translator.Translate(quote.Operand as LambdaExpression);
        }

    }
}
