using Aerobool.Interface;

namespace Aerobool.Expressions
{
    public class EqualsExpression : IExpression
    {
        public IExpression Left { get; }
        public IExpression Right { get; }

        public EqualsExpression(IExpression left, IExpression right)
        {
            Left = left;
            Right = right;
        }

        public object Evaluate(object? context)
        {
            return Left.Evaluate(context) == Right.Evaluate(context);
        }
    }
}
