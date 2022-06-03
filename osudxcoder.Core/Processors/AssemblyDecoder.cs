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
    public class DecodedAssembly<T>
    {
        public string ObfuscatedName { get; }
        public string CleanName { get; }
        public T Member { get; }

        public DecodedAssembly(string obfuscatedName, string cleanName, T member)
        {
            ObfuscatedName = obfuscatedName;
            CleanName = cleanName;
            Member = member;
        }
    }

    public class AssemblyDecoder : IProcessor
    {
        public readonly List<DecodedAssembly<object>> DecodedAssemblyCache;

        private readonly ModuleDef _targetModule;
        private readonly Func<string, string> _decrypt;

        private MethodDef _dxqObfuscatedCtor;

        public AssemblyDecoder(ModuleDef module, Func<string, string> decryptMethod)
        {
            DecodedAssemblyCache = new List<DecodedAssembly<object>>();
            
            _targetModule = module;
            _decrypt = decryptMethod;
        }

        public void Process()
        {
            if (Utils.CliOptions.EnableAttributes)
            {
                var attrRefType = _targetModule.CorLibTypes.GetTypeRef("System", "Attribute");
                var dxqObfTypeDef = _targetModule.FindNormal("dxqObfuscated");
                if (dxqObfTypeDef is null)
                {
                    dxqObfTypeDef = new TypeDefUser("", "dxqObfuscated", attrRefType);

                    _targetModule.Types.Add(dxqObfTypeDef);
                }

                _dxqObfuscatedCtor = dxqObfTypeDef.FindInstanceConstructors()
                    .FirstOrDefault(x =>
                        x.Parameters.Count == 1 && x.Parameters[0].Type == _targetModule.CorLibTypes.String);

                if (_dxqObfuscatedCtor is null)
                {
                    _dxqObfuscatedCtor = new MethodDefUser(".ctor",
                        MethodSig.CreateInstance(_targetModule.CorLibTypes.Void, _targetModule.CorLibTypes.String),
                        MethodImplAttributes.IL,
                        MethodAttributes.FamANDAssem | MethodAttributes.Family | MethodAttributes.HideBySig |
                        MethodAttributes.SpecialName | MethodAttributes.RTSpecialName)
                    {
                        Body = new CilBody
                        {
                            MaxStack = 1
                        }
                    };
                    
                    _dxqObfuscatedCtor.Body.Instructions.Add(OpCodes.Ldarg_0.ToInstruction());
                    _dxqObfuscatedCtor.Body.Instructions.Add(OpCodes.Call.ToInstruction(new MemberRefUser(_targetModule,
                        ".ctor", MethodSig.CreateInstance(_targetModule.CorLibTypes.Void), attrRefType)));
                    _dxqObfuscatedCtor.Body.Instructions.Add(OpCodes.Ret.ToInstruction());

                    dxqObfTypeDef.Methods.Add(_dxqObfuscatedCtor);
                }
                
                var customAttribute = new CustomAttribute(_dxqObfuscatedCtor);
                customAttribute.ConstructorArguments.Add(new CAArgument(_targetModule.CorLibTypes.String,
                    "dxqObfuscatedName"));
                _targetModule.CustomAttributes.Add(customAttribute);
            }
            
            DecodeRecursive(_targetModule.Types);
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

            if (Utils.CliOptions.Verbose)
                XLogger.Message($"Decrypting assembly {param.Name}!");
            
            var cleanName = _decrypt(param.Name);
            
            if (string.IsNullOrWhiteSpace(cleanName))
                return;

            DecodedAssemblyCache.Add(new DecodedAssembly<object>(param.Name, cleanName, param));

            if (param is IHasCustomAttribute p && Utils.CliOptions.EnableAttributes)
            {
                var customAttribute = new CustomAttribute(_dxqObfuscatedCtor);

                if (p.CustomAttributes.All(x => x.TypeFullName != "dxqObfuscated"))
                {
                    if (Utils.CliOptions.Verbose)
                        XLogger.Message($"Applying attribute {param.Name}...");
                    
                    customAttribute.ConstructorArguments.Add(new CAArgument(_targetModule.CorLibTypes.String,
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

            if (Utils.CliOptions.Verbose)
                XLogger.Message($"Decrypting parameter {param.Name}...");
            
            var cleanName = _decrypt(param.Name);

            if (string.IsNullOrWhiteSpace(cleanName))
                return;
            
            DecodedAssemblyCache.Add(new DecodedAssembly<object>(param.Name, cleanName, param));
            
            param.Name = cleanName;
        }
    }
}