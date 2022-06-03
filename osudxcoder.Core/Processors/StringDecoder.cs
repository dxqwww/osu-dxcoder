using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using osudxcoder.Shared;
using osudxcoder.Shared.Logger;

namespace osudxcoder.Core.Processors
{
    public class StringDecoder : IProcessor
    {
        public readonly Dictionary<int, string> DecodedStringsCache;
        
        private readonly List<DecodedAssembly<object>> _decodedAssemblyCache;
        
        private readonly ModuleDef _targetModule;
        private readonly MethodDef _decryptMethod;

        private readonly Func<int, string> _decrypt;

        public StringDecoder(Assembly assembly, ModuleDef module, List<DecodedAssembly<object>> decodedAssemblyCache, MethodDef decryptMethod)
        {
            DecodedStringsCache = new Dictionary<int, string>();
            
            _targetModule = module;
            _decodedAssemblyCache = decodedAssemblyCache;
            _decryptMethod = decryptMethod;

            if (decryptMethod is null)
                throw new Exception("Couldn't find method to decrypt strings!");
            
            var decryptMethodFlags = BindingFlags.Default;
            decryptMethodFlags |= decryptMethod.IsPublic ? BindingFlags.Public : BindingFlags.NonPublic;
            decryptMethodFlags |= decryptMethod.IsStatic ? BindingFlags.Static : BindingFlags.Instance;

            var decodedCachedAssembly =
                _decodedAssemblyCache.FirstOrDefault(x => x.CleanName == decryptMethod.DeclaringType.FullName);

            if (decodedCachedAssembly is null)
                throw new Exception("Cannot get Eazfuscator type to decrypt strings!");
            
            var cachedMethod = _decodedAssemblyCache.FirstOrDefault(x =>
                x.Member is MethodDef m && m.DeclaringType.FullName == decryptMethod.DeclaringType.FullName &&
                m.Name == decryptMethod.Name);

            if (cachedMethod is null)
                throw new Exception("Cannot get method to decrypt strings!");
            
            var type = assembly.GetType(decodedCachedAssembly.ObfuscatedName);
            var method = type.GetMethod(cachedMethod.ObfuscatedName, decryptMethodFlags, null, new[] { typeof(int) }, null);
            
            if (method is null)
                throw new Exception("Cannot get method to decrypt strings through reflection!");
            
            var methodParam = Expression.Parameter(typeof(int));
            var methodCall = Expression.Call(null, method, methodParam);
            var methodDelegate = Expression.Lambda<Func<int, string>>(methodCall, methodParam);
            
            _decrypt = methodDelegate.Compile();
        }

        public void Process() => DecodeRecursive(_targetModule.Types);

        private void DecodeRecursive(IEnumerable<ITypeOrMethodDef> members)
        {
            foreach (var member in members)
            {
                switch (member)
                {
                    case TypeDef t:
                        DecodeRecursive(t.Methods);
                        DecodeRecursive(t.NestedTypes);
                        
                        break;
                    case MethodDef m:
                        try
                        {
                            DecodeSingle(m);
                        }
                        catch (Exception e)
                        {
                            XLogger.Error(e.Message);
                        }
                        break;
                }
            }
        }

        private void DecodeSingle(MethodDef method)
        {
            if (!method.HasBody || !method.Body.HasInstructions)
                return;

            for (var i = 1; i < method.Body.Instructions.Count; i++)
            {
                var prev = method.Body.Instructions[i - 1];
                var curr = method.Body.Instructions[i];

                if (!prev.IsLdcI4() || curr.Operand is not MethodDef md ||
                    md.MDToken != _decryptMethod.MDToken) 
                    continue;
                
                var encryptedValue = prev.GetLdcI4Value();
                if (!DecodedStringsCache.ContainsKey(encryptedValue))
                {
                    if (Utils.CliOptions.Verbose)
                        XLogger.Message($"Decrypting string value {encryptedValue}!");
                    
                    DecodedStringsCache[encryptedValue] = _decrypt(encryptedValue);
                }

                prev.OpCode = OpCodes.Nop;
                curr.OpCode = OpCodes.Ldstr;
                curr.Operand = DecodedStringsCache[encryptedValue];
            }
        }
    }
}