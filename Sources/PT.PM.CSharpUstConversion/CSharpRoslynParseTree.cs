﻿using System.Collections.Generic;
using PT.PM.Common;
using Microsoft.CodeAnalysis;

namespace PT.PM.CSharpUstConversion
{
    public class CSharpRoslynParseTree : ParseTree
    {
        public readonly SyntaxTree SyntaxTree;

        public SyntaxTrivia[] Comments;

        public bool ParseError { get; set; }

        public override Language SourceLanguage => Language.CSharp;

        public CSharpRoslynParseTree()
        {
        }

        public CSharpRoslynParseTree(SyntaxTree syntaxTree)
        {
            SyntaxTree = syntaxTree;
        }
    }
}