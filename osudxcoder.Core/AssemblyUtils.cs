using System.Collections.Generic;
using System.Linq;
using dnlib.DotNet;

namespace osudxcoder.Core
{
    public static class AssemblyUtils
    {
        public static IEnumerable<MethodDef> GetMethodsRecursive(ModuleDef module) =>
            module.Types.SelectMany(GetMethodsRecursive);
        public static IEnumerable<MethodDef> GetMethodsRecursive(TypeDef type)
        {
            foreach (var m in type.Methods)
                yield return m;

            foreach (var nt in type.NestedTypes)
            foreach (var m in GetMethodsRecursive(nt))
                yield return m;
        }
    }
}