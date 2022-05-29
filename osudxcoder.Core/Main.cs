using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using CommandLine;
using dnlib.DotNet;
using osudxcoder.Core.Processors;
using osudxcoder.Shared;
using osudxcoder.Shared.Logger;

namespace osudxcoder.Core
{
    public static class Main
    {
        #region Static Fields (private)

        private static ModuleDef OsuAssembly;
        private static Assembly OsuWAssembly;
        
        private static AssemblyDecoder AssemblyDecoder;
        
        #endregion

        #region Static Methods (public)  
        
        
        public static int DllMain(string dummy)
        {
            var rawArgs = ArgsHelper.ReadArgsFromFile();
            ArgsHelper.DeleteArgsFile();
            
            Entry(rawArgs.Split(' '));
            return 0;
        }

        #endregion
        
        #region Static Methods (private)
        
        private static void Entry(string[] args)
        {
            Parser.Default.ParseArguments<CliOptions>(args)
                .WithParsed(o => Utils.CliOptions = o);
            
            AllocConsole();

            XLogger.Message("Loading assemblies...");
            
            OsuAssembly = ModuleDefMD.Load(Utils.GetStablePath());
            OsuWAssembly = AppDomain.CurrentDomain
                .GetAssemblies()
                .FirstOrDefault(x => Regex.IsMatch(x.GetName().Name,
                    "^[0-9a-fA-F]{8}-([0-9a-fA-F]{4}-){3}[0-9a-fA-F]{12}$", RegexOptions.Compiled));

            if (OsuWAssembly is null)
            {
                XLogger.Error("Cannot get assembly for decrypt symbols!");
                return;
            }
            
            XLogger.Info($"Loaded {OsuAssembly.Types.Count} types");
            
            var getClearNameMethod = OsuWAssembly.GetType("A.B").GetMethod("X");
            
            if (getClearNameMethod is null)
            {
                XLogger.Error("Cannot get method for decrypt symbols!");
                return;
            }
            
            var param = Expression.Parameter(typeof(string));
            var call = Expression.Call(null, getClearNameMethod, param);
            var getClearNameCall = Expression.Lambda<Func<string, string>>(call, param);
            
            var getClearName = getClearNameCall.Compile();

            XLogger.Message("Decrypting...");
            AssemblyDecoder = new AssemblyDecoder(OsuAssembly, getClearName);
            AssemblyDecoder.Process();

            var outputFile = string.IsNullOrWhiteSpace(Utils.CliOptions.Output)
                ? $"{Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, $"{Constants.ProcessName}-decoded.exe")}"
                : Utils.CliOptions.Output;
            
            XLogger.Info("Decrypting is done!");
            
            XLogger.Message("Writing new assembly...");
            OsuAssembly.Write(outputFile);
            XLogger.Info($"Written to {outputFile}");
            
            XLogger.System($"Press any key to kill {Constants.ProcessName} process");
            Console.ReadKey(true);
            
            Process.GetCurrentProcess().Kill();
        }
        
        #endregion
        
        #region DllImports (private)
        
        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AllocConsole();
        
        #endregion
    }
}