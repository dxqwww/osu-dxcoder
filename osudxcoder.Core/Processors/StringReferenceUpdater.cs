using System.Collections.Generic;
using System.Linq;
using dnlib.DotNet;
using osudxcoder.Shared;
using osudxcoder.Shared.Logger;

namespace osudxcoder.Core.Processors
{
    public class StringReferenceUpdater : IProcessor
    {
        private readonly ModuleDef _targetModule;
        private readonly List<DecodedAssembly<object>> _decodedAssemblyCache;

        public StringReferenceUpdater(ModuleDef module, List<DecodedAssembly<object>> decodedAssemblyCache)
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
            if (!method.HasBody || !method.Body.HasInstructions)
                return;

            foreach (var instruction in method.Body.Instructions)
            {
                if (instruction.Operand is not string str || !Constants.RegexObfuscated.IsMatch(str))
                    continue;
                
                if (_decodedAssemblyCache.FirstOrDefault(x => x.ObfuscatedName == str) is { Member: MethodDef } decodedAssembly)
                {
                    if (Utils.CliOptions.Verbose)
                        XLogger.Message($"Updating string reference {instruction.Operand}...");
                    
                    instruction.Operand = decodedAssembly.CleanName;
                }
            }
        }
    }
}