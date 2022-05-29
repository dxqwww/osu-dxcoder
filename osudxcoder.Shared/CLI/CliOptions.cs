using System;
using CommandLine;
using CommandLine.Text;

namespace osudxcoder.Shared
{
    public class CliOptions
    {
        [Value(0)]
        public string Input { get; set; }
        
        [Option('o', "output", HelpText = "Path of the output file")]
        public string Output { get; set; }
        
        [Option('v', "verbose", HelpText = "Prints more output")]
        public bool Verbose { get; set; }

        [Option('t', "types-only", HelpText = "Only types will be renamed without applying attributes to them")]
        public bool TypesOnly { get; set; }
        
        public string GetHelp()
        {
            var helpText = new HelpText
            {
                Heading = new HeadingInfo("osu!dxcoder", "1.1"),
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