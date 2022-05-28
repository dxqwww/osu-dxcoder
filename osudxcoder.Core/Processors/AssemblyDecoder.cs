using System;
using System.Collections.Generic;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using osudxcoder.Shared;
using osudxcoder.Shared.Logger;
using MethodAttributes = dnlib.DotNet.MethodAttributes;
using MethodImplAttributes = dnlib.DotNet.MethodImplAttributes;

namespace osudxcoder.Core.Processors
{
    public class AssemblyDecoder : IProcessor
    {
        public readonly Dictionary<string, string> SourceMap = new();

        private readonly ModuleDef TargetAssembly;
        private readonly Func<string, string> Decrypt;

        private MethodDef DxqObfustacedCtor;

        public AssemblyDecoder(ModuleDef assembly, Func<string, string> decryptFunc)
        {
            TargetAssembly = assembly;
            Decrypt = decryptFunc;
        }

        public void Process()
        {
            if (!Utils.CliOptions.TypesOnly)
            {
                var attrRefType = TargetAssembly.CorLibTypes.GetTypeRef("System", "Attribute");
                var dxqObfTypeDef = TargetAssembly.FindNormal("dxqObfuscated");
                if (dxqObfTypeDef is null)
                {
                    dxqObfTypeDef = new TypeDefUser("", "dxqObfuscated", attrRefType);

                    TargetAssembly.Types.Add(dxqObfTypeDef);
                }

                DxqObfustacedCtor = dxqObfTypeDef.FindInstanceConstructors()
                    .FirstOrDefault(x =>
                        x.Parameters.Count == 1 && x.Parameters[0].Type == TargetAssembly.CorLibTypes.String);

                if (DxqObfustacedCtor is null)
                {
                    DxqObfustacedCtor = new MethodDefUser(".ctor",
                        MethodSig.CreateInstance(TargetAssembly.CorLibTypes.Void, TargetAssembly.CorLibTypes.String),
                        MethodImplAttributes.IL,
                        MethodAttributes.FamANDAssem | MethodAttributes.Family | MethodAttributes.HideBySig |
                        MethodAttributes.SpecialName | MethodAttributes.RTSpecialName)
                    {
                        Body = new CilBody
                        {
                            MaxStack = 1
                        }
                    };
                    
                    DxqObfustacedCtor.Body.Instructions.Add(OpCodes.Ldarg_0.ToInstruction());
                    DxqObfustacedCtor.Body.Instructions.Add(OpCodes.Call.ToInstruction(new MemberRefUser(TargetAssembly,
                        ".ctor", MethodSig.CreateInstance(TargetAssembly.CorLibTypes.Void), attrRefType)));
                    DxqObfustacedCtor.Body.Instructions.Add(OpCodes.Ret.ToInstruction());

                    dxqObfTypeDef.Methods.Add(DxqObfustacedCtor);
                }
                
                var customAttribute = new CustomAttribute(DxqObfustacedCtor);
                customAttribute.ConstructorArguments.Add(new CAArgument(TargetAssembly.CorLibTypes.String,
                    "dxqObfuscatedName"));
                TargetAssembly.CustomAttributes.Add(customAttribute);
            }
            
            DecodeRecursive(TargetAssembly.Types);
        }

        private void DecodeRecursive(IEnumerable<IFullName> members)
        {
            foreach (var fullName in members)
            {
                switch (fullName)
                {
                    case TypeDef t:
                        DecodeSingle(t);

                        foreach (var genericParam in t.GenericParameters)
                            DecodeSingle(genericParam);

                        DecodeRecursive(t.Events);
                        DecodeRecursive(t.Fields);
                        DecodeRecursive(t.Methods);
                        DecodeRecursive(t.NestedTypes);
                        DecodeRecursive(t.Properties);

                        foreach (var impl in t.Interfaces)
                            DecodeSingle(impl.Interface);

                        break;
                    case MethodDef m:
                        DecodeSingle(m);

                        foreach (var genericParam in m.GenericParameters)
                            DecodeSingle(genericParam);

                        foreach (var param in m.Parameters)
                            DecodeSingle(param);

                        break;
                    case FieldDef _:
                    case PropertyDef _:
                    case EventDef _:
                        DecodeSingle(fullName);
                        break;
                    default:
                        DecodeSingle(fullName);
                        break;
                }
            }
        }

        private void DecodeSingle(IFullName param)
        {
            if (!Constants.RegexObfuscated.IsMatch(param.Name))
                return;

            var cleanName = Decrypt(param.Name);
            
            if (Utils.CliOptions.Verbose)
                XLogger.Message($"Decrypted {cleanName}!");
            
            if (param is IHasCustomAttribute p && !Utils.CliOptions.TypesOnly)
            {
                var customAttribute = new CustomAttribute(DxqObfustacedCtor);

                if (p.CustomAttributes.All(x => x.TypeFullName != "dxqObfuscated"))
                {
                    if (Utils.CliOptions.Verbose)
                        XLogger.Message($"Applying attribute {param.Name}...");
                    
                    customAttribute.ConstructorArguments.Add(new CAArgument(TargetAssembly.CorLibTypes.String,
                        param.Name));
                    p.CustomAttributes.Add(customAttribute);
                }
            }
            
            if (param is TypeDef typeDef && cleanName.Contains("."))
            {
                typeDef.Namespace = cleanName.Substring(0, cleanName.LastIndexOf(".", StringComparison.Ordinal));
                typeDef.Name = cleanName.Substring(cleanName.LastIndexOf(".", StringComparison.Ordinal) + 1);
            }
            else if (param is TypeSpec spec)
                DecodeSingle(spec.ScopeType);
            else
                param.Name = cleanName;
        }

        private void DecodeSingle(IVariable param)
        {
            if (!Constants.RegexObfuscated.IsMatch(param.Name))
                return;

            var cleanName = Decrypt(param.Name);
            param.Name = cleanName;
        }
    }
}