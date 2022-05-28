using System.Text.RegularExpressions;

namespace osudxcoder.Shared
{
    public static class Constants
    {
        public const string ProcessName = "osu!";
        public static readonly Regex RegexObfuscated = new("^#=[a-zA-Z0-9_$]+={0,2}$", RegexOptions.Compiled);
    }
}