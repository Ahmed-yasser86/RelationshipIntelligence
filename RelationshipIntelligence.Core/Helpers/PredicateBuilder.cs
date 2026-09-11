using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace ContactsManger.Core.Helpers
{

    
        public static class PredicateBuilder
        {
            public static Expression<Func<T, bool>> True<T>() => f => true;

            public static Expression<Func<T, bool>> And<T>(
                this Expression<Func<T, bool>> expr1, Expression<Func<T, bool>> expr2)
            {
                var parameter = Expression.Parameter(typeof(T));

                var leftVisitor = new ReplaceParameterVisitor(expr1.Parameters[0], parameter);
                var left = leftVisitor.Visit(expr1.Body);

                var rightVisitor = new ReplaceParameterVisitor(expr2.Parameters[0], parameter);
                var right = rightVisitor.Visit(expr2.Body);

                return Expression.Lambda<Func<T, bool>>(Expression.AndAlso(left!, right!), parameter);
            }

            private class ReplaceParameterVisitor : ExpressionVisitor
            {
                private readonly ParameterExpression _oldParameter;
                private readonly ParameterExpression _newParameter;

                public ReplaceParameterVisitor(ParameterExpression oldParameter, ParameterExpression newParameter)
                {
                    _oldParameter = oldParameter;
                    _newParameter = newParameter;
                }

                protected override Expression VisitParameter(ParameterExpression node)
                    => node == _oldParameter ? _newParameter : base.VisitParameter(node);
            }
        }
    }

