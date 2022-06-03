using System;
using CommandLine;
using CommandLine.Text;

namespace osudxcoder.Shared.CLI
{
    public class CliOptions
    {
        [Value(0, Required = true)]
        public string Input { get; set; }
        
        [Option('o', "output", HelpText = "Path of the output file")]
        public string Output { get; set; }
        
        [Option('v', "verbose", HelpText = "Prints more output")]
        public bool Verbose { get; set; }

        [Option("attributes", HelpText = "Applies attributes with original assembly name to decrypted assemblies")]
        public bool EnableAttributes { get; set; }
        
        [Option("string-refs", HelpText = "Decrypts strings references")]
        public bool StringsRefFix { get; set; }
        
        [Option("type-refs", HelpText = "Decrypts types references")]
        public bool TypesRefFix { get; set; }
        
        [Option("string-fix", HelpText = "Decrypts all strings in assembly")]
        public bool StringFix { get; set; }
        
        public string GetHelp()
        {
            var helpText = new HelpText
            {
                Heading = new HeadingInfo("osu!dxcoder", "1.3"),
                Copyright = new CopyrightInfo("dxqwww", 2022),
                AdditionalNewLineAfterOption = false,
                AddDashesToOption = true,
                MaximumDisplayWidth = Console.BufferWidth
            };
            
            helpText.AddPreOptionsLine("\nExample usage:");
            helpText.AddPreOptionsLine("\t— osudxcoder.Injector osu!.exe");
            helpText.AddPreOptionsLine("\t— osudxcoder.Injector osu!.exe -v -o C:\\Users\\dxq\\Desktop");
            return helpText;
        }
    }
}