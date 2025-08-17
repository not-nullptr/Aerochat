using Aerobool.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aerobool.Expressions
{
    public class ValueExpression : IExpression
    {
        public object Value { get; set; }

        public ValueExpression(object value)
        {
            Value = value;
        }

        public object Evaluate(object? context)
        {
            return Value;
        }
    }
}
