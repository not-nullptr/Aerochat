using Aerobool.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aerobool.Expressions
{
    public class TypecheckExpression : IExpression
    {
        public IExpression Left { get; set; }
        public IExpression Right { get; set; }

        public TypecheckExpression(IExpression left, IExpression right)
        {
            Left = left;
            Right = right;
        }

        public object Evaluate(object? context)
        {
            var left = Left.Evaluate(context);
            var right = Right.Evaluate(context);
            if (right is not Type) throw new ArgumentException("Right expression must return a type");
            return left.GetType().IsAssignableFrom((Type)right);
        }
    }
}
