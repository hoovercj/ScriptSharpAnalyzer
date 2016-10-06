using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ScriptSharp
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ScriptSharpAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "ScriptSharp";

        // You can change these strings in the Resources.resx file. If you do not want your analyzer to be localize-able, you can use regular strings for Title and MessageFormat.
        // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/Localizing%20Analyzers.md for more on localization

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            id: "ScriptSharp1337",
            title: "ScriptSharp",
            messageFormat:"ScriptSharp doesn't allow {0}",
            category: "ScriptSharp",
            defaultSeverity: DiagnosticSeverity.Error, 
            isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeSyntaxNode, 
                SyntaxKind.GetAccessorDeclaration, SyntaxKind.SetAccessorDeclaration, 
                SyntaxKind.ParenthesizedLambdaExpression, SyntaxKind.SimpleLambdaExpression,
                SyntaxKind.InterfaceDeclaration,
                SyntaxKind.ObjectInitializerExpression, SyntaxKind.CollectionInitializerExpression,
                SyntaxKind.QueryExpression);
        }

        private static void AnalyzeSyntaxNode(SyntaxNodeAnalysisContext context)
        {
            var node = context.Node;
            var root = node.SyntaxTree.GetRoot();

            // check if System.Runtime.CompilerServices have been imported as an indicator of ScriptSharp files
            // This would not be necessary if the analyzer was included in the ScriptSharp projets directly
            // but it allows the analyzer to be added to visual studio locally without affecting project files
            var scriptSharp = root.DescendantNodes()
                .OfType<UsingDirectiveSyntax>()
                .Any(directive => directive.Name.ToString() == "System.Runtime.CompilerServices");
            if (!scriptSharp)
            {
                return;
            }

            string message = default(string);
            switch (node.RawKind)
            {
                // Disallow auto properties
                case (int)SyntaxKind.GetAccessorDeclaration:
                case (int)SyntaxKind.SetAccessorDeclaration:
                    var hasBlock = node.ChildNodes().Any(syntaxNode => syntaxNode.IsKind(SyntaxKind.Block));
                    if (!hasBlock)
                    {
                        message = "Auto-properties/properties without a block.";
                    }
                    break;
                // Disallow lamdba expressions
                case (int)SyntaxKind.ParenthesizedLambdaExpression:
                case (int)SyntaxKind.SimpleLambdaExpression:
                    message = "Lambda expressions. Use a delegate instead.";
                    break;
                // Disallow interfaces inheriting from other interfaces
                case (int)SyntaxKind.InterfaceDeclaration:
                    var inherits = node.ChildNodes().Any(syntaxNode => syntaxNode.IsKind(SyntaxKind.BaseList));
                    if (inherits)
                    {
                        message = "Interfaces that inherit from other interfaces.";
                    }
                    break;
                // Disallow object and collection initializers
                case (int)SyntaxKind.ObjectInitializerExpression:
                case (int)SyntaxKind.CollectionInitializerExpression:
                    message = "Object and Collection initializers.";
                    break;
                // Disallow query expressions (such as LINQ)
                case (int)SyntaxKind.QueryExpression:
                    message = "Query expressions (such as LINQ)";
                    break;
                default:
                    break;
            }

            if (!String.IsNullOrEmpty(message))
            {
                var diagnostic = Diagnostic.Create(Rule, node.GetLocation(), message);
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
