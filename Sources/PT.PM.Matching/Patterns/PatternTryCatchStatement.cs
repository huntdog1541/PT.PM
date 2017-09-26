﻿using PT.PM.Common;
using PT.PM.Common.Nodes;
using PT.PM.Common.Nodes.Statements;
using PT.PM.Common.Nodes.Statements.TryCatchFinally;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PT.PM.Matching.Patterns
{
    public class PatternTryCatchStatement : Statement, IPatternUst
    {
        public List<PatternBase> ExceptionTypes { get; set; }

        public bool IsCatchBodyEmpty { get; set; }

        public PatternTryCatchStatement()
        {
            ExceptionTypes = new List<PatternBase>();
            IsCatchBodyEmpty = true;
        }

        public PatternTryCatchStatement(IEnumerable<PatternBase> exceptionTypes, bool isCatchBodyEmpty,
            TextSpan textSpan)
            : base(textSpan)
        {
            ExceptionTypes = exceptionTypes?.ToList()
                ?? throw new ArgumentNullException("exceptionTypes");
            IsCatchBodyEmpty = isCatchBodyEmpty;
        }

        public override Ust[] GetChildren() => ArrayUtils<Ust>.EmptyArray;

        public override string ToString() => $"try catch {{ }}";

        public MatchingContext Match(Ust ust, MatchingContext context)
        {
            if (ust?.Kind != UstKind.TryCatchStatement)
            {
                return context.Fail();
            }

            var otherTryCatch = (TryCatchStatement)ust;
            if (otherTryCatch.CatchClauses == null)
            {
                return context.Fail();
            }
            else
            {
                bool result = otherTryCatch.CatchClauses.Any(catchClause =>
                {
                    if (IsCatchBodyEmpty && catchClause.Body.Statements.Any())
                    {
                        return false;
                    }

                    return !ExceptionTypes.Any() || ExceptionTypes.Any(type => type.Match(catchClause.Type, context).Success);
                });

                return context.Change(result);
            }
        }
    }
}
