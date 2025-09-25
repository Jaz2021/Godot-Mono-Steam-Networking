using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
namespace NetworkingV2Generator;

[Generator]
public class NetworkingGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var descriptor = new DiagnosticDescriptor(
                "TEST001",
                "Generated Source Preview",
                "{0}",
                "SourceGen",
                DiagnosticSeverity.Info,
                isEnabledByDefault: true
            );
        
        context.RegisterPostInitializationOutput(ctx => ctx.AddSource(
            "EnumExtensionsAttribute.g.cs", 
            SourceText.From(SourceGenerationHelper.Attribute, Encoding.UTF8)));
        IncrementalValueProvider<ImmutableArray<string>> packetsToGen = context.SyntaxProvider.ForAttributeWithMetadataName(
            "Networking_V2.PacketAttribute",
        predicate: (s, _) => true,
        transform: (ctx, _) => GetSemanticTargetForGeneration(ctx)).Where(static s => s is not null)
        .Collect();
        StringBuilder sb = new();
        context.RegisterSourceOutput(packetsToGen, (spc, snippets) => {
            string joinedCases = string.Join("\n", snippets);
            string template = SourceGenerationHelper.Net;
            string finalSource = template.Replace("/*CASE*/", joinedCases);
            // File.WriteAllText("~NetworkingV2_Packets.g.cs", finalSource);
            spc.AddSource("NetworkingV2_packets.g.cs", finalSource);
        });
    }
    
    static string GetSemanticTargetForGeneration(GeneratorAttributeSyntaxContext context)
    {
        if (context.TargetSymbol is not INamedTypeSymbol classSymbol){
            return "";
        }

        foreach (var attributeData in classSymbol.GetAttributes()){

            if (attributeData.AttributeClass?.ToDisplayString() == "Networking_V2.PacketAttribute" && attributeData.ConstructorArguments.Length == 1 && attributeData.ConstructorArguments[0].Kind == TypedConstantKind.Primitive && attributeData.ConstructorArguments[0].Value is byte id)
            {
                // if(!ImplementsInterface(classSymbol, "Networking_V2.IPacket")){
                //     return "//Bad Interface";
                // }
                var str = SourceGenerationHelper.Case;
                str = str.Replace("/*class*/", classSymbol.Name);
                str = str.Replace("/*type*/", id.ToString());
                return str;
            }
        }

        // we didn't find the attribute we were looking for
        return "";
    }   
    private static bool ImplementsInterface(INamedTypeSymbol symbol, string interfaceFullName)
    {
        return symbol.AllInterfaces.Any(i => i.ToDisplayString() == interfaceFullName);
    }


}





