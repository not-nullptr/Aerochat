using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aerobool.Interface
{
    public interface IExpression
    {
        public object Evaluate(object? context);
    }
}
