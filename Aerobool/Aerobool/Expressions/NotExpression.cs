using Aerobool.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aerobool.Expressions
{
    public class NotExpression : IExpression
    {
        public IExpression Expression;

        public NotExpression(IExpression expression)
        {
            Expression = expression;
        }

        public object Evaluate(object? context)
        {
            var res = Expression.Evaluate(context);
            if (res is not bool) throw new ArgumentException("Expression must return a boolean value");
            return !(bool)res;
        }
    }
}
