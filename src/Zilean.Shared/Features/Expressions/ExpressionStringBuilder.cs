namespace Zilean.Shared.Features.Expressions;

/// <summary>
/// Renders a LINQ expression tree into a human-readable string representation.
/// </summary>
public class ExpressionStringBuilder : ExpressionVisitor
{
    private readonly StringBuilder _sb = new();

    /// <summary>
    /// Visits and renders an expression node into the internal string builder.
    /// </summary>
    /// <param name="node">The expression node to visit, or <c>null</c>.</param>
    /// <returns>The original expression node, or <c>null</c> if the input was null.</returns>
    public override Expression Visit(Expression? node)
    {
        if (node == null)
        {
            return null;
        }

        switch (node.NodeType)
        {
            case ExpressionType.Lambda:
                var lambda = (LambdaExpression)node;
                Visit(lambda.Body);
                break;
            case ExpressionType.MemberAccess:
                var member = (MemberExpression)node;
                Visit(member.Expression);
                _sb.Append($".{member.Member.Name}");
                break;
            case ExpressionType.Constant:
                var constant = (ConstantExpression)node;
                _sb.Append(constant.Value);
                break;
            case ExpressionType.Equal:
                var binaryEqual = (BinaryExpression)node;
                Visit(binaryEqual.Left);
                _sb.Append(" == ");
                Visit(binaryEqual.Right);
                break;
            case ExpressionType.NotEqual:
                var binaryNotEqual = (BinaryExpression)node;
                Visit(binaryNotEqual.Left);
                _sb.Append(" != ");
                Visit(binaryNotEqual.Right);
                break;
            case ExpressionType.AndAlso:
                var binaryAnd = (BinaryExpression)node;
                Visit(binaryAnd.Left);
                _sb.Append(" && ");
                Visit(binaryAnd.Right);
                break;
            case ExpressionType.OrElse:
                var binaryOr = (BinaryExpression)node;
                Visit(binaryOr.Left);
                _sb.Append(" || ");
                Visit(binaryOr.Right);
                break;
            case ExpressionType.Call:
                var methodCall = (MethodCallExpression)node;
                Visit(methodCall.Object);
                _sb.Append($".{methodCall.Method.Name}(");
                for (int i = 0; i < methodCall.Arguments.Count; i++)
                {
                    if (i > 0)
                    {
                        _sb.Append(", ");
                    }

                    Visit(methodCall.Arguments[i]);
                }

                _sb.Append(")");
                break;
            default:
                _sb.Append(node);
                break;
        }

        return node;
    }

    /// <summary>
    /// Returns the rendered string representation of the visited expression tree.
    /// </summary>
    /// <returns>The accumulated expression string.</returns>
    public override string ToString() => _sb.ToString();
}