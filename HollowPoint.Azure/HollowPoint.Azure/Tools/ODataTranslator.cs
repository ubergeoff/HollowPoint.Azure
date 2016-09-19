using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Web;


namespace HollowPoint.Azure.Tools
{
    public class ODataTranslator : ExpressionVisitor
    {
        private StringBuilder sb;
        private string _orderBy = string.Empty;
        private int? _skip = null;
        private int? _take = null;
        private string _whereClause = string.Empty;

        public int? Skip
        {
            get
            {
                return _skip;
            }
        }

        public int? Take
        {
            get
            {
                return _take;
            }
        }

        public string OrderBy
        {
            get
            {
                return _orderBy;
            }
        }

        public string WhereClause
        {
            get
            {
                return _whereClause;
            }
        }

        public ODataTranslator()
        {
        }

        public string Translate(Expression expression)
        {
            this.sb = new StringBuilder();
            this.Visit(expression);
            _whereClause = this.sb.ToString();
            return _whereClause;
        }

        private static Expression StripQuotes(Expression e)
        {
            while (e.NodeType == ExpressionType.Quote)
            {
                e = ((UnaryExpression)e).Operand;
            }
            return e;
        }

        protected override Expression VisitMethodCall(MethodCallExpression m)
        {
            if (m.Method.DeclaringType == typeof(Queryable) && m.Method.Name == "Where")
            {
                this.Visit(m.Arguments[0]);
                LambdaExpression lambda = (LambdaExpression)StripQuotes(m.Arguments[1]);
                this.Visit(lambda.Body);
                return m;
            }
            else if (m.Method.Name == "Take")
            {
                if (this.ParseTakeExpression(m))
                {
                    Expression nextExpression = m.Arguments[0];
                    return this.Visit(nextExpression);
                }
            }
            else if (m.Method.Name == "Skip")
            {
                if (this.ParseSkipExpression(m))
                {
                    Expression nextExpression = m.Arguments[0];
                    return this.Visit(nextExpression);
                }
            }
            //else if (m.Method.Name == "OrderBy")
            //{
            //    if (this.ParseOrderByExpression(m, "ASC"))
            //    {
            //        Expression nextExpression = m.Arguments[0];
            //        return this.Visit(nextExpression);
            //    }
            //}
            //else if (m.Method.Name == "OrderByDescending")
            //{
            //    if (this.ParseOrderByExpression(m, "DESC"))
            //    {
            //        Expression nextExpression = m.Arguments[0];
            //        return this.Visit(nextExpression);
            //    }
            //}

            throw new NotSupportedException(string.Format("The method '{0}' is not supported", m.Method.Name));
        }

        protected override Expression VisitUnary(UnaryExpression u)
        {
            switch (u.NodeType)
            {
                case ExpressionType.Not:
                    sb.Append(" NOT ");
                    this.Visit(u.Operand);
                    break;
                case ExpressionType.Convert:
                    this.Visit(u.Operand);
                    break;
                default:
                    throw new NotSupportedException(string.Format("The unary operator '{0}' is not supported", u.NodeType));
            }
            return u;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
        protected override Expression VisitBinary(BinaryExpression b)
        {
            sb.Append("(");
            this.Visit(b.Left);

            switch (b.NodeType)
            {
                case ExpressionType.And:
                    sb.Append(" and ");
                    break;

                case ExpressionType.AndAlso:
                    sb.Append(" and ");
                    break;

                case ExpressionType.Or:
                    sb.Append(" or ");
                    break;

                case ExpressionType.OrElse:
                    sb.Append(" or ");
                    break;

                case ExpressionType.Equal:
                    if (IsNullConstant(b.Right))
                    {
                        sb.Append(" IS ");
                    }
                    else
                    {
                        sb.Append(" eq ");
                    }
                    break;

                case ExpressionType.NotEqual:
                    if (IsNullConstant(b.Right))
                    {
                        sb.Append(" IS NOT ");
                    }
                    else
                    {
                        sb.Append(" ne ");
                    }
                    break;

                case ExpressionType.LessThan:
                    sb.Append(" lt ");
                    break;

                case ExpressionType.LessThanOrEqual:
                    sb.Append(" le ");
                    break;

                case ExpressionType.GreaterThan:
                    sb.Append(" gt ");
                    break;

                case ExpressionType.GreaterThanOrEqual:
                    sb.Append(" ge ");
                    break;

                default:
                    throw new NotSupportedException(string.Format("The binary operator '{0}' is not supported", b.NodeType));

            }

            this.Visit(b.Right);
            sb.Append(")");
            return b;
        }

        protected override Expression VisitConstant(ConstantExpression c)
        {
            IQueryable q = c.Value as IQueryable;

            if (q == null && c.Value == null)
            {
                sb.Append("NULL");
            }
            else if (q == null)
            {
                AppendByValueType(c.Value);
               
            }

            return c;
        }

        protected override Expression VisitMember(MemberExpression m)
        {

            // If the NodeType is a `Parameter`, we want to add the key as a DB Field name to our string collection
            // Otherwise, we want to add the key as a DB Parameter to our string collection
            if (m.Expression.NodeType == ExpressionType.Parameter)
            {
                sb.Append(m.Member.Name);
                return m;
            }
            else
            {
                //_strings.Add(string.Format("@{0}", key));

                // If the key is being added as a DB Parameter, then we have to also add the Parameter key/value pair to the collection
                // Because we're working off of Model Objects that should only contain Properties or Fields,
                // there should only be two options. PropertyInfo or FieldInfo... let's extract the VALUE accordingly
                var value = new object();
                if ((m.Member as PropertyInfo) != null)
                {
                    var exp = (MemberExpression)m.Expression;
                    var constant = (ConstantExpression)exp.Expression;
                    var fieldInfoValue = ((FieldInfo)exp.Member).GetValue(constant.Value);
                    value = ((PropertyInfo)m.Member).GetValue(fieldInfoValue, null);
                    sb.Append(value);

                    return m;
                }
                else if ((m.Member as FieldInfo) != null)
                {
                    var fieldInfo = m.Member as FieldInfo;
                    var constantExpression = m.Expression as ConstantExpression;

                    if (fieldInfo != null & constantExpression != null)
                    {
                        value = fieldInfo.GetValue(constantExpression.Value);
                        AppendByValueType(value);

                        return m;
                    }

                    throw new NotSupportedException(string.Format("The member '{0}' is not supported", m.Member.Name));

                }
                else
                {
                    //throw new InvalidMemberException();
                    throw new NotSupportedException(string.Format("The member '{0}' is not supported", m.Member.Name));
                }

                // Add the Parameter Key/Value pair.
                //Parameters.Add("@" + key, value);
            }
        }

        private void AppendByValueType(object value)
        {
            switch (Type.GetTypeCode(value.GetType()))
            {
                case TypeCode.Boolean:
                    sb.Append(((bool)value) ? 1 : 0);
                    break;

                case TypeCode.String:
                    sb.Append("'");
                    sb.Append(value);
                    sb.Append("'");
                    break;

                case TypeCode.DateTime:
                    sb.Append("'");
                    sb.Append(value);
                    sb.Append("'");
                    break;

                case TypeCode.Object:
                    throw new NotSupportedException(string.Format("The constant for '{0}' is not supported", value));

                default:
                    sb.Append(value);
                    break;
            }

        }

        protected bool IsNullConstant(Expression exp)
        {
            return (exp.NodeType == ExpressionType.Constant && ((ConstantExpression)exp).Value == null);
        }

        //private bool ParseOrderByExpression(MethodCallExpression expression, string order)
        //{
        //    UnaryExpression unary = (UnaryExpression)expression.Arguments[1];
        //    LambdaExpression lambdaExpression = (LambdaExpression)unary.Operand;

        //    lambdaExpression = (LambdaExpression)Evaluator.PartialEval(lambdaExpression);

        //    MemberExpression body = lambdaExpression.Body as MemberExpression;
        //    if (body != null)
        //    {
        //        if (string.IsNullOrEmpty(_orderBy))
        //        {
        //            _orderBy = string.Format("{0} {1}", body.Member.Name, order);
        //        }
        //        else
        //        {
        //            _orderBy = string.Format("{0}, {1} {2}", _orderBy, body.Member.Name, order);
        //        }

        //        return true;
        //    }

        //    return false;
        //}

        private bool ParseTakeExpression(MethodCallExpression expression)
        {
            ConstantExpression sizeExpression = (ConstantExpression)expression.Arguments[1];

            int size;
            if (int.TryParse(sizeExpression.Value.ToString(), out size))
            {
                _take = size;
                return true;
            }

            return false;
        }

        private bool ParseSkipExpression(MethodCallExpression expression)
        {
            ConstantExpression sizeExpression = (ConstantExpression)expression.Arguments[1];

            int size;
            if (int.TryParse(sizeExpression.Value.ToString(), out size))
            {
                _skip = size;
                return true;
            }

            return false;
        }
    }
}