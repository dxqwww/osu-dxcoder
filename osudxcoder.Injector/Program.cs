using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using CommandLine;
using HoLLy.ManagedInjector;
using osudxcoder.Shared;
using osudxcoder.Shared.CLI;
using osudxcoder.Shared.Logger;

namespace osudxcoder.Injector
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            Parser.Default.ParseArguments<CliOptions>(args)
                .WithParsed(o =>
                {
                    Utils.CliOptions = o;
                    ArgsHelper.SaveArgsToFile(args);
                });

            if (Utils.CliOptions is null)
                return;
            
            if (string.IsNullOrWhiteSpace(Utils.CliOptions.Input))
            {
                Console.WriteLine(Utils.CliOptions.GetHelp());
                return;
            }
            
            XLogger.Message($"Trying to find {Constants.ProcessName}...");
            
            var targetProcess = Process.GetProcessesByName(Constants.ProcessName).FirstOrDefault();

#if DEBUG
            if (targetProcess is not null)
            {
                XLogger.Warning($"{Constants.ProcessName} is already running! Closing it...");
                targetProcess.Kill();
            }
            else
            {
                XLogger.Message($"No running {Constants.ProcessName} found!");
            }

            XLogger.Message($"Starting {Constants.ProcessName}...");
            Thread.Sleep(3000);
            targetProcess = Process.Start(Utils.GetStablePath());
            Thread.Sleep(3000);
#endif

            while (targetProcess is null)
                targetProcess = Process.GetProcessesByName(Constants.ProcessName).FirstOrDefault();

            XLogger.Info($"{Constants.ProcessName} is found!");
            XLogger.System($"Press any key to inject dll into {Constants.ProcessName}");

            Console.ReadKey(true);
            
            XLogger.Message("Starting injection...");
            XLogger.Info($"PID: {targetProcess.Id}");
            
            var process = new InjectableProcess((uint)targetProcess.Id);

            var processStatus = process.GetStatus();
            Debug.Assert(processStatus == ProcessStatus.Ok);

            XLogger.Info($"Process status: {Enum.GetName(typeof(ProcessStatus), processStatus)}");
            
            var processArch = process.GetArchitecture();
            Debug.Assert(processArch == ProcessArchitecture.NetFrameworkV4);
            
            XLogger.Info($"Process architecture: {Enum.GetName(typeof(ProcessArchitecture), processArch)}");
            
            XLogger.Message("Injecting...");
            process.Inject(typeof(Core.Main).Assembly.Location, "osudxcoder.Core.Main", "DllMain");

            XLogger.Info("Successfully injected!");
            XLogger.System($"Press any key to close this window");

            Console.ReadKey(true);
        }
    }
}