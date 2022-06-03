using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using CommandLine;
using dnlib.DotNet;
using osudxcoder.Core.Processors;
using osudxcoder.Shared;
using osudxcoder.Shared.CLI;
using osudxcoder.Shared.Logger;

namespace osudxcoder.Core
{
    public static class Main
    {
        #region Static Fields (public)

        public static ModuleDef OsuModule;

        #endregion
        
        #region Static Fields (private)
        
        private static Assembly OsuAssembly;
        private static Assembly OsuWAssembly;

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
            AppDomain.CurrentDomain.UnhandledException += GlobalUnhandledExceptionHandler;
            
            Parser.Default.ParseArguments<CliOptions>(args)
                .WithParsed(o => Utils.CliOptions = o);
            
            AllocConsole();
            
            XLogger.Message("Loading assemblies...");
            
            OsuModule = ModuleDefMD.Load(Utils.CliOptions.Input);
            
            OsuAssembly = Assembly.LoadFrom(Utils.CliOptions.Input);
            OsuWAssembly = AppDomain.CurrentDomain
                .GetAssemblies()
                .FirstOrDefault(x => Regex.IsMatch(x.GetName().Name,
                    "^[0-9a-fA-F]{8}-([0-9a-fA-F]{4}-){3}[0-9a-fA-F]{12}$", RegexOptions.Compiled));

            if (OsuWAssembly is null)
            {
                XLogger.Error("Cannot get assembly for decrypt symbols!");
                return;
            }
            
            XLogger.Info($"Loaded {OsuModule.Types.Count} types");

            XLogger.Message("Decrypting assemblies...");
            var assemblyDecoder = new AssemblyDecoder(OsuModule, Helpers.GetAssemblyDecryptMethod(OsuWAssembly));
            assemblyDecoder.Process();

            if (Utils.CliOptions.TypesRefFix)
            {
                XLogger.Message("Updating assembly references...");
                var assemblyReferenceUpdater = new AssemblyReferenceUpdater(OsuModule, assemblyDecoder.DecodedAssemblyCache);
                assemblyReferenceUpdater.Process();
            }

            if (Utils.CliOptions.StringsRefFix)
            {
                XLogger.Message("Updating strings references...");
                var stringReferenceUpdater = new StringReferenceUpdater(OsuModule, assemblyDecoder.DecodedAssemblyCache);
                stringReferenceUpdater.Process();
            }

            if (Utils.CliOptions.StringFix)
            {
                XLogger.Message("Decrypting strings...");

                var stringDecoder =
                    new StringDecoder(OsuAssembly, OsuModule, assemblyDecoder.DecodedAssemblyCache, Helpers.GetStringDecryptMethod(OsuModule));
                stringDecoder.Process();
            }

            var outputFile = string.IsNullOrWhiteSpace(Utils.CliOptions.Output)
                ? $"{Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, $"{Constants.ProcessName}-decoded.exe")}"
                : Utils.CliOptions.Output;
            
            XLogger.Info("Decrypting is done!");
            
            XLogger.Message("Writing new assembly...");
            OsuModule.Write(outputFile);
            XLogger.Info($"Written to {outputFile}");
            
            XLogger.System($"Press any key to kill {Constants.ProcessName} process");
            Console.ReadKey(true);
            
            Process.GetCurrentProcess().Kill();
        }

        private static void GlobalUnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = (Exception)e.ExceptionObject;
            XLogger.Error($"{exception.Message}");
        }
        
        #endregion
        
        #region DllImports (private)
        
        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AllocConsole();
        
        #endregion
    }
}