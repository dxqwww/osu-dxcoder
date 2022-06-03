using System;
using System.Collections.Generic;
using System.Linq;
using dnlib.DotNet;
using osudxcoder.Shared;
using osudxcoder.Shared.Logger;

namespace osudxcoder.Core.Processors
{
    public class AssemblyReferenceUpdater : IProcessor
    {
        private readonly ModuleDef _targetModule;
        private readonly List<DecodedAssembly<object>> _decodedAssemblyCache;

        public AssemblyReferenceUpdater(ModuleDef module, List<DecodedAssembly<object>> decodedAssemblyCache)
        {
            _targetModule = module;
            _decodedAssemblyCache = decodedAssemblyCache;
        }

        public void Process() => UpdateRecursive(_targetModule.Types);

        private void UpdateRecursive(IEnumerable<ITypeOrMethodDef> members)
        {
            foreach (var member in members)
            {
                switch (member)
                {
                    case TypeDef t:
                        UpdateRecursive(t.Methods);
                        UpdateRecursive(t.NestedTypes);

                        break;
                    case MethodDef m:
                        UpdateSingle(m);
                        break;
                }
            }
        }

        private void UpdateSingle(MethodDef method)
        {
            foreach (var methodOverride in method.Overrides)
            {
                var baseMethod = methodOverride.MethodDeclaration;

                if (Constants.RegexObfuscated.IsMatch(baseMethod.Name) &&
                    _decodedAssemblyCache.FirstOrDefault(x => x.ObfuscatedName == baseMethod.Name) is { Member: MethodDef } decodedAssembly)
                {
                    baseMethod.Name = decodedAssembly.CleanName;
                }
            }
            
            if (!method.HasBody || !method.Body.HasInstructions)
                return;

            foreach (var instruction in method.Body.Instructions)
            {
                if (instruction.Operand is not IFullName param || !Constants.RegexObfuscated.IsMatch(param.Name))
                    continue;
                
                if (_decodedAssemblyCache.FirstOrDefault(x => x.ObfuscatedName == param.Name) is { Member: MethodDef } decodedAssembly)
                {
                    if (Utils.CliOptions.Verbose)
                        XLogger.Message($"Updating assembly reference {param.Name}...");

                    param.Name = decodedAssembly.CleanName;
                }
            }
        }
    }
}