using System;
using System.IO;
using Microsoft.Win32;

namespace osudxcoder.Shared
{
    public static class Utils
    {
        public static CliOptions CliOptions;
        
        public static string GetStablePath()
        {
            var reg = Registry.ClassesRoot.OpenSubKey("osu\\DefaultIcon");

            if (reg is null)
                throw new Exception("Cannot find target path, make sure you run game at least once!");

            var path = reg.GetValue(null).ToString();

            return path.Substring(1, path.Length - 4);
        }
    }
}