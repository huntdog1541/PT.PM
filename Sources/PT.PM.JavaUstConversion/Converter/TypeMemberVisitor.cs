﻿using PT.PM.Common.Nodes;
using PT.PM.Common.Nodes.Expressions;
using PT.PM.Common.Nodes.Tokens;
using PT.PM.Common.Nodes.Statements;
using PT.PM.Common.Nodes.TypeMembers;
using PT.PM.JavaUstConversion.Parser;
using Antlr4.Runtime.Tree;
using System.Collections.Generic;
using System.Linq;
using PT.PM.AntlrUtils;

namespace PT.PM.JavaUstConversion.Converter
{
    public partial class JavaAntlrUstConverterVisitor
    {
        public UstNode VisitClassBodyDeclaration(JavaParser.ClassBodyDeclarationContext context)
        { 
            EntityDeclaration result;
            var block = context.block();
            if (block != null)
            {
                var blockStatement = (BlockStatement)Visit(block);
                result = new StatementDeclaration(blockStatement, context.GetTextSpan(), FileNode);
            }
            else
            {
                result = (EntityDeclaration)Visit(context.memberDeclaration());
            }
            return result;
        }

        public UstNode VisitInterfaceBodyDeclaration(JavaParser.InterfaceBodyDeclarationContext context)
        {
            var result = VisitInterfaceMemberDeclaration(context.interfaceMemberDeclaration());
            return result;
        }

        public UstNode VisitConstructorBody(JavaParser.ConstructorBodyContext context)
        {
            var result = VisitBlock(context.block());
            return result;
        }

        public UstNode VisitInterfaceMemberDeclaration(JavaParser.InterfaceMemberDeclarationContext context)
        {
            return (EntityDeclaration)Visit(context.GetChild(0));
        }

        public UstNode VisitMemberDeclaration(JavaParser.MemberDeclarationContext context)
        {
            return (EntityDeclaration)Visit(context.GetChild(0));
        }

        public UstNode VisitInterfaceMethodDeclaration(JavaParser.InterfaceMethodDeclarationContext context)
        {
            JavaParser.TypeTypeContext type = context.typeType();
            ITerminalNode child0Terminal = context.GetChild<ITerminalNode>(0);
            ITerminalNode identifier = context.Identifier();
            JavaParser.FormalParametersContext formalParameters = context.formalParameters();
            JavaParser.MethodBodyContext methodBody = null;

            MethodDeclaration result = ConvertMethodDeclaration(type, child0Terminal, identifier, formalParameters, methodBody,
                context.GetTextSpan());
            return result;
        }

        public UstNode VisitMethodDeclaration(JavaParser.MethodDeclarationContext context)
        {
            JavaParser.TypeTypeContext type = context.typeType();
            ITerminalNode child0Terminal = context.GetChild<ITerminalNode>(0);
            ITerminalNode identifier = context.Identifier();
            JavaParser.FormalParametersContext formalParameters = context.formalParameters();
            JavaParser.MethodBodyContext methodBody = context.methodBody();

            MethodDeclaration result = ConvertMethodDeclaration(type, child0Terminal, identifier, formalParameters, methodBody,
                context.GetTextSpan());
            return result;
        }

        public UstNode VisitGenericMethodDeclaration(JavaParser.GenericMethodDeclarationContext context)
        {
            return VisitChildren(context);
        }

        public UstNode VisitFieldDeclaration(JavaParser.FieldDeclarationContext context)
        {
            var type = VisitTypeType(context.typeType());
            AssignmentExpression[] varInits = context.variableDeclarators().variableDeclarator()
                .Select(varDec => (AssignmentExpression)Visit(varDec))
                .Where(varDec => varDec != null).ToArray();

            var result = new FieldDeclaration(varInits, context.GetTextSpan(), FileNode);
            return result;
        }

        public UstNode VisitConstructorDeclaration(JavaParser.ConstructorDeclarationContext context)
        {
            var id = (IdToken)Visit(context.Identifier());
            IEnumerable<ParameterDeclaration> parameters;
            JavaParser.FormalParameterListContext formalParameterList = context.formalParameters().formalParameterList();
            if (formalParameterList == null)
                parameters = Enumerable.Empty<ParameterDeclaration>();
            else
                parameters = formalParameterList.formalParameter()
                    .Select(param => (ParameterDeclaration)Visit(param))
                    .Where(p => p != null).ToArray();

            var body = (BlockStatement)Visit(context.constructorBody());

            var constructorDelaration = new ConstructorDeclaration(id, parameters, body, context.GetTextSpan(), FileNode);
            return constructorDelaration;
        }

        public UstNode VisitGenericConstructorDeclaration(JavaParser.GenericConstructorDeclarationContext context)
        {
            return VisitChildren(context);
        }

        public UstNode VisitVariableDeclarators(JavaParser.VariableDeclaratorsContext context)
        {
            return VisitShouldNotBeVisited(context);
        }

        public UstNode VisitVariableDeclarator(JavaParser.VariableDeclaratorContext context)
        {
            var id = (IdToken)Visit(context.variableDeclaratorId());
            JavaParser.VariableInitializerContext variableInitializer = context.variableInitializer();
            Expression initializer = variableInitializer != null ? 
                (Expression)Visit(variableInitializer) : null;
            
            var result = new AssignmentExpression(id, initializer, context.GetTextSpan(), FileNode);
            return result;
        }

        public UstNode VisitVariableDeclaratorId(JavaParser.VariableDeclaratorIdContext context)
        {
            var result = (IdToken)Visit(context.Identifier());
            return result;
        }

        public UstNode VisitVariableInitializer(JavaParser.VariableInitializerContext context)
        {
            var result = (Expression)Visit(context.GetChild(0));
            return result;
        }

        public UstNode VisitMethodBody(JavaParser.MethodBodyContext context)
        {
            var result = (BlockStatement)Visit(context.block());
            return result;
        }

        public UstNode VisitFormalParameters(JavaParser.FormalParametersContext context)
        {
            return VisitChildren(context);
        }

        public UstNode VisitFormalParameterList(JavaParser.FormalParameterListContext context)
        {
            return VisitChildren(context);
        }

        public UstNode VisitFormalParameter(JavaParser.FormalParameterContext context)
        {
            var type = (TypeToken)Visit(context.typeType());
            var id = (IdToken)Visit(context.variableDeclaratorId());

            var result = new ParameterDeclaration(type, id, context.GetTextSpan(), FileNode);
            return result;
        }
    }
}
