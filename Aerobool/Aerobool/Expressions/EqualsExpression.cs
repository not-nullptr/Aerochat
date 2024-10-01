using Aerobool.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aerobool.Expressions
{
    public class EqualsExpression(IExpression left, IExpression right) : IExpression
    {
        public IExpression Left { get; set; } = left;
        public IExpression Right { get; set; } = right;

        public object Evaluate(object? context)
        {
            return Left.Evaluate(context) == Right.Evaluate(context);
        }
    }
}
