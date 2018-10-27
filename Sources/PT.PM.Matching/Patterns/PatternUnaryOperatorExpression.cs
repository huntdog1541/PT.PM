﻿using PT.PM.Common.Nodes;
using PT.PM.Common.Nodes.Expressions;
using PT.PM.Common.Nodes.Tokens;
using PT.PM.Common.Nodes.Tokens.Literals;

namespace PT.PM.Matching.Patterns
{
    public class PatternUnaryOperatorExpression : PatternUst
    {
        public PatternUst Expression { get; set; }

        public PatternUnaryOperatorLiteral Operator { get; set; }

        public override string ToString()
        {
            UnaryOperator op = Operator.UnaryOperator;
            if (UnaryOperatorLiteral.PrefixTextUnaryOperator.ContainsValue(op))
            {
                string spaceOrEmpty = op == UnaryOperator.Delete || op == UnaryOperator.TypeOf ||
                                      op == UnaryOperator.Await || op == UnaryOperator.Void
                    ? " "
                    : "";
                return $"{Operator}{spaceOrEmpty}{Expression}";
            }

            if (UnaryOperatorLiteral.PostfixTextUnaryOperator.ContainsValue(op))
            {
                return $"{Expression}{Operator}";
            }

            return $"{Operator} {Expression}";
        }

        public override MatchContext Match(Ust ust, MatchContext context)
        {
            var unaryOperatorExpression = ust as UnaryOperatorExpression;
            if (unaryOperatorExpression == null)
            {
                return context.Fail();
            }
            
            MatchContext newContext = Expression.Match(unaryOperatorExpression.Expression, context);
            if (!newContext.Success)
            {
                return newContext;
            }

            newContext = Operator.Match(unaryOperatorExpression.Operator, newContext);

            return newContext.AddUstIfSuccess(ust);
        }
    }
}
