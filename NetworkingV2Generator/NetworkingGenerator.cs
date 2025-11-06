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
            "NetworkingAttributes.g.cs",
            SourceText.From(SourceGenerationHelper.Attribute, Encoding.UTF8)));
        context.RegisterPostInitializationOutput(ctx => ctx.AddSource(
            "NetworkingSerializeAttribute.g.cs",
            SourceText.From(SourceGenerationHelper.SerializeDataAttribute, Encoding.UTF8)
        ));
        IncrementalValueProvider<ImmutableArray<PacketTarget>> packetsToGen = context.SyntaxProvider.ForAttributeWithMetadataName(
            "Networking_V2.PacketAttribute",
        predicate: (s, _) => true,
        transform: (ctx, _) => GetSemanticTargetForGeneration(ctx)).Where(static s => !s.IsNull())
        .Collect()
        .Select(static (items, _) =>
            items
                .OrderBy(static t => t.name, StringComparer.Ordinal)
                .ToImmutableArray()
            );;
        IncrementalValueProvider<ImmutableArray<SerializerTarget>> serializableFields = context.SyntaxProvider.ForAttributeWithMetadataName(
            "Networking_V2.SerializeDataAttribute",
            predicate: (s, _) => true,
            transform: (ctx, t) => GetSerializerTargetForGeneration(ctx)).Where(static s => !s.IsEmpty()).Collect()
            // Sort by alphabetical to force same order between systems
            .Select(static (items, _) =>
            items
                .OrderBy(static t => t.name, StringComparer.Ordinal)
                .ThenBy(static t => t.superclass, StringComparer.Ordinal)
                .ToImmutableArray()
            );
        var fullSet = serializableFields.Combine(packetsToGen);

        // StringBuilder sb = new();
        context.RegisterSourceOutput(fullSet, (spc, snippets) =>
        {
            StringBuilder sb = new();
            int type = 0;
            foreach (var packetType in snippets.Right)
            {
                var str = SourceGenerationHelper.Case;
                str = str.Replace("/*class*/", packetType.name);
                str = str.Replace("/*type*/", type.ToString());
                string source = SourceGenerationHelper.SerializerClass;
                if (packetType.generateConstructor)
                {
                    source = source.Replace("/*constructor*/", SourceGenerationHelper.Constructor);
                }
                if (packetType.generateSerializer)
                {
                    source = source.Replace("/*serializer*/", SourceGenerationHelper.SerializerFuncs);
                }
                if(packetType.generateSignal)
                {
                    source = source.Replace("/*signal*/", SourceGenerationHelper.Signal);
                }
                source = source.Replace("/*id*/", type.ToString());
                source = source.Replace("/*class*/", packetType.name);
                StringBuilder class_vars = new();
                StringBuilder class_var_inputs = new();
                StringBuilder serializers = new();
                StringBuilder deserializers = new();
                StringBuilder class_var_setters = new();
                sb.AppendLine(str);
                foreach (var serializerTarget in snippets.Left)
                {
                    if (serializerTarget.superclass != packetType.name)
                    {
                        continue; // Skip the serializers that don't match the class we are sorting over
                    }
                    class_var_inputs.Append($"{serializerTarget.type} {serializerTarget.name}, ");
                    class_var_setters.AppendLine($"this.{serializerTarget.name} = {serializerTarget.name};");
                    class_vars.Append($"{serializerTarget.name}, ");
                    serializers.AppendLine($"..{serializerTarget.name}.Serialize(),");
                    deserializers.AppendLine(SourceGenerationHelper.Deserializer.Replace("/*type*/", serializerTarget.type).Replace("/*name*/", serializerTarget.name));
                    // deserializers.AppendLine($"// {serializerTarget.superclass}, {packetType.Item1}");
                    // source += $"\n//{serializerTarget.superclass}, {packetType.Item1}";
                }
                if(class_var_inputs.Length > 2)
                {
                    class_var_inputs.Remove(class_var_inputs.Length - 2, 2);
                }
                if(class_vars.Length > 2)
                {
                    class_vars.Remove(class_vars.Length - 2, 2);
                }
                source = source.Replace("/*class_var_inputs*/", class_var_inputs.ToString());
                source = source.Replace("/*class_vars*/", class_vars.ToString());
                source = source.Replace("/*class_var_setters*/", class_var_setters.ToString());
                source = source.Replace("/*serializers*/", serializers.ToString());
                source = source.Replace("/*deserializers*/", deserializers.ToString());
                // source = source + $"\n/// {serializerTarget.superclass}, {packetType.Item1}";
                spc.AddSource($"{packetType.name}.g.cs", source);
                type += 1;
            }
            string joinedCases = sb.ToString();
            string template = SourceGenerationHelper.Net;
            string finalSource = template.Replace("/*CASE*/", joinedCases);
            // File.WriteAllText("~NetworkingV2_Packets.g.cs", finalSource);
            spc.AddSource("NetworkingV2_packets.g.cs", finalSource);
            // spc.AddSource("test.g.cs", $"//{snippets.Right.Length}");
        });
    }
    
    struct SerializerTarget
    {
        public SerializerTarget(){}
        public SerializerTarget(string type, string name, string superclass)
        {
            this.type = type;
            this.name = name;
            this.superclass = superclass;
        }
        public readonly string type = "";
        public readonly string name = "";
        public readonly string superclass = "";
        public bool IsEmpty()
        {
            return type == "" && name == "" && superclass == "";
        }
    }
    static SerializerTarget GetSerializerTargetForGeneration(GeneratorAttributeSyntaxContext context)
    {
        if (context.TargetSymbol is not IFieldSymbol variableSymbol)
        {
            return new();
        }
        if (variableSymbol.ContainingType is not INamedTypeSymbol classSymbol)
        {
            return new();
        }
        var typeSymbol = variableSymbol.Type;
        if (typeSymbol == null)
        {
            return new();
        }
        // From this point, we know what we have received is a valid field within the given class
        return new(typeSymbol.Name, variableSymbol.Name, classSymbol.Name);

    }
    struct PacketTarget
    {
        public PacketTarget() { }
        public PacketTarget(string name, bool generateSerializer, bool generateConstructor, bool generateSignal)
        {
            this.name = name;
            this.generateConstructor = generateConstructor;
            this.generateSerializer = generateSerializer;
            this.generateSignal = generateSignal;
        }
        public readonly string name = "";
        public readonly bool generateConstructor = false;
        public readonly bool generateSerializer = false;
        public readonly bool generateSignal = false;
        public bool IsNull()
        {
            return false;
        }
    }
    static PacketTarget GetSemanticTargetForGeneration(GeneratorAttributeSyntaxContext context)
    {
        if (context.TargetSymbol is not INamedTypeSymbol classSymbol){
            // return new("bada", true, true, true);
            return new();
        }

        foreach (var attributeData in classSymbol.GetAttributes()){

            if (attributeData.AttributeClass?.ToDisplayString() == "Networking_V2.PacketAttribute")
            {
                // if(!ImplementsInterface(classSymbol, "Networking_V2.IPacket")){
                //     return "//Bad Interface";
                // }
                bool generateConstructor = true;
                bool generateSerializer = true;
                bool generateSignal = true;
                if(attributeData.ConstructorArguments.Length > 0)
                {
                    if(attributeData.ConstructorArguments[0].Value is int flags)
                    {
                        generateConstructor = 0 != (flags & 2);
                        generateSerializer = 0 != (flags & 1);
                        generateSignal = 0 != (flags & 4);
                    }
                }
                return new(classSymbol.Name, generateSerializer, generateConstructor, generateSignal);
            }
        }

        // we didn't find the attribute we were looking for
        // return new("badb", true, true, true);
        return new();
    }   
    private static bool ImplementsInterface(INamedTypeSymbol symbol, string interfaceFullName)
    {
        return symbol.AllInterfaces.Any(i => i.ToDisplayString() == interfaceFullName);
    }


}





