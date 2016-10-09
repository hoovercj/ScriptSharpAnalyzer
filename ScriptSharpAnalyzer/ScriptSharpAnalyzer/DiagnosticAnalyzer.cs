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
        // You can change these strings in the Resources.resx file. If you do not want your analyzer to be localize-able, you can use regular strings for Title and MessageFormat.
        // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/Localizing%20Analyzers.md for more on localization

        public const string AutoPropDiagnosticId = "ScriptSharp1338";
        public const string LambdaDiagnosticId = "ScriptSharp1339";
        public const string InterfaceInheritanceDiagnosticId = "ScriptSharp1340";
        public const string ObjectInitializerDiagnosticId = "ScriptSharp1341";
        public const string QueryExpressionDiagnosticId = "ScriptSharp1342";

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get
            {
                return ImmutableArray.Create(
                    CreateRule(AutoPropDiagnosticId),
                    CreateRule(LambdaDiagnosticId),
                    CreateRule(InterfaceInheritanceDiagnosticId),
                    CreateRule(ObjectInitializerDiagnosticId),
                    CreateRule(QueryExpressionDiagnosticId)
                );
            }
        }

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
            string id = default(string);
            switch (node.RawKind)
            {
                // Disallow auto properties
                case (int)SyntaxKind.GetAccessorDeclaration:
                case (int)SyntaxKind.SetAccessorDeclaration:
                    var hasBlock = node.ChildNodes().Any(syntaxNode => syntaxNode.IsKind(SyntaxKind.Block));
                    if (!hasBlock)
                    {
                        id = AutoPropDiagnosticId;
                        message = "Auto-properties/properties without a block.";
                    }
                    break;
                // Disallow lamdba expressions
                case (int)SyntaxKind.ParenthesizedLambdaExpression:
                case (int)SyntaxKind.SimpleLambdaExpression:
                    id = LambdaDiagnosticId;
                    message = "Lambda expressions. Use a delegate instead.";
                    break;
                // Disallow interfaces inheriting from other interfaces
                case (int)SyntaxKind.InterfaceDeclaration:
                    var inherits = node.ChildNodes().Any(syntaxNode => syntaxNode.IsKind(SyntaxKind.BaseList));
                    if (inherits)
                    {
                        id = InterfaceInheritanceDiagnosticId;
                        message = "Interfaces that inherit from other interfaces.";
                    }
                    break;
                // Disallow object and collection initializers
                case (int)SyntaxKind.ObjectInitializerExpression:
                case (int)SyntaxKind.CollectionInitializerExpression:
                    id = ObjectInitializerDiagnosticId;
                    message = "Object and Collection initializers.";
                    break;
                // Disallow query expressions (such as LINQ)
                case (int)SyntaxKind.QueryExpression:
                    id = QueryExpressionDiagnosticId;
                    message = "Query expressions (such as LINQ)";
                    break;
                default:
                    break;
            }

            if (!String.IsNullOrEmpty(message) && !String.IsNullOrEmpty(id))
            {
                var diagnostic = Diagnostic.Create(CreateRule(id), node.GetLocation(), message);
                context.ReportDiagnostic(diagnostic);
            }
        }

        private static DiagnosticDescriptor CreateRule(string id)
        {
            return new DiagnosticDescriptor(
                    id: id,
                    title: "ScriptSharp",
                    messageFormat: "ScriptSharp doesn't allow {0}",
                    category: "ScriptSharp",
                    defaultSeverity: DiagnosticSeverity.Error,
                    isEnabledByDefault: true);
        }
    }
}
