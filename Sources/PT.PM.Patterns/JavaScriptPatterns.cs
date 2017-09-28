﻿using PT.PM.Common;
using PT.PM.Matching.Patterns;
using System.Collections.Generic;
using static PT.PM.Common.Language;

namespace PT.PM.Patterns.PatternsRepository
{
    public partial class DefaultPatternRepository
    {
        public IEnumerable<PatternRootUst> CreateJavaScriptPatterns()
        {
            var patterns = new List<PatternRootUst>();

            patterns.Add(new PatternRootUst
            {
                Key = patternIdGenerator.NextId(),
                DebugInfo = "AttributesCodeInsideElementEvent",
                Languages = new HashSet<Language>() { JavaScript },
                Node = new PatternAssignmentExpression
                {
                    Left = new PatternMemberReferenceExpression
                    {
                        Target = new PatternAnyExpression(),
                        Name = new PatternIdRegexToken("^on")
                    },
                    Right = new PatternArbitraryDepthExpression
                    (
                        new PatternOr
                        (
                            new PatternMemberReferenceExpression
                            {
                                Target = new PatternIdToken("document"),
                                Name = new PatternIdRegexToken("^(URL|referrer|cookie)$")
                            },

                            new PatternOr
                            (
                                new PatternMemberReferenceExpression
                                {
                                    Target = new PatternMemberReferenceExpression
                                    {
                                        Target = new PatternIdToken("document"),
                                        Name = new PatternIdToken("location")
                                    },
                                    Name = new PatternIdRegexToken("^(pathname|href|search|hash)$")
                                },

                                new PatternOr
                                (
                                    new PatternMemberReferenceExpression
                                    {
                                        Target = new PatternIdToken("window"),
                                        Name = new PatternIdToken("name")
                                    },
                                    new PatternMemberReferenceExpression
                                    {
                                        Target = new PatternMemberReferenceExpression
                                        {
                                            Target = new PatternMemberReferenceExpression
                                            {
                                                Target = new PatternMemberReferenceExpression
                                                {
                                                    Target = new PatternIdToken("window"),
                                                    Name = new PatternIdRegexToken("^(top|frames)$")
                                                },
                                                Name = new PatternIdToken("document")
                                            },
                                            Name = new PatternIdRegexToken()
                                        },
                                        Name = new PatternIdRegexToken()
                                    }
                                )
                            )
                        )
                    )
                }
            });

            return patterns;
        }
    }
}
