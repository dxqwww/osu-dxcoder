using System.IO;

namespace osudxcoder.Shared
{
    public static class ArgsHelper
    {
        public static string ReadArgsFromFile() => File.ReadAllText(TempArgsPath);
        
        public static void SaveArgsToFile(string [] args) =>
            File.WriteAllText(TempArgsPath, string.Join(" ", args));

        public static void DeleteArgsFile() => File.Delete(TempArgsPath);

        private static readonly string TempArgsPath = $"{Path.GetTempPath()}\\osudxcoder-args";
    }
}