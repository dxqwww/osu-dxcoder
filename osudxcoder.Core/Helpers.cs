using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using EazDecodeLib;
using osudxcoder.Shared;
using osudxcoder.Shared.Logger;

namespace osudxcoder.Core
{
    public static class Helpers
    {
        public static Func<string, string> GetAssemblyDecryptMethod(Assembly wAssembly)
        {
            var decryptMethod = wAssembly.GetType("A.B").GetMethod("X");

            if (decryptMethod is null)
            {
                XLogger.Error("Couldn't find method for decrypt symbols!");
                XLogger.Debug("Trying to use legacy decrypt method...");

                if (string.IsNullOrEmpty(Utils.CliOptions.OsuVersion))
                {
                    XLogger.Error("--osu-version should be supplied!");
                    throw new Exception();
                }
                
                var cryptoHelper = new CryptoHelper($"3f21fioh321fip231-{Utils.CliOptions.OsuVersion}");

                return cryptoHelper.Decrypt;
            }

            var decryptParam = Expression.Parameter(typeof(string));
            var decryptCall = Expression.Call(null, decryptMethod, decryptParam);
            var decryptDelegate = Expression.Lambda<Func<string, string>>(decryptCall, decryptParam);
            
            return decryptDelegate.Compile();
        }

        public static MethodDef GetStringDecryptMethod(ModuleDef module) => 
            AssemblyUtils.GetMethodsRecursive(module).SingleOrDefault(IsStringDecryptMethod);

        public static bool IsStringDecryptMethod(MethodDef method)
        {
            if (!method.IsStatic || !method.IsAssembly || method.IsPrivate || method.IsPublic)
                return false;

            if (method.MethodSig.Params.Count != 1 || method.MethodSig.Params[0] != Main.OsuModule.CorLibTypes.Int32 ||
                method.ReturnType != Main.OsuModule.CorLibTypes.String)
                return false;
                
            if (!method.HasBody || !method.Body.HasInstructions)
                return false;

            return method.Body.Instructions.Any(i =>
                i.OpCode.Code == Code.Call && i.Operand is MethodDef m && m.MethodSig.Params.Count == 2 &&
                m.MethodSig.Params[0] == Main.OsuModule.CorLibTypes.Int32 &&
                m.MethodSig.Params[1] == Main.OsuModule.CorLibTypes.Boolean);
        }
    }
}