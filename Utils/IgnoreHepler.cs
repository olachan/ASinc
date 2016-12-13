using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Aaf.Sinc.Utils
{
    public class IgnoreHepler
    {
        private const string IGNORE_FILE_PATH = "Config\\.asignore";
        private static List<string> Ignores = new List<string>();
        private static string pattern = string.Empty;
        
        static IgnoreHepler()
        {
            Ignores = File.ReadAllLines(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, IGNORE_FILE_PATH))
                .Where(l => !l.Trim().StartsWith("#") && !string.IsNullOrEmpty(l.Trim()))
                .Select(l => l.Trim().Replace("*",@"\w*").Replace("?",@"\w?").Replace("/",@"\/")).ToList();
            pattern = string.Join("|", Ignores);
            //for (var i = 0; i < lines.Length; i++)
            //{
            //    var line = lines[i].Trim();
            //    if (line.StartsWith("#")) continue;
            //    Ignores.Add(line);
            //}
        }

        public static bool IsMatch(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            path = path.Replace(@"\", "/");
            var rgx = new Regex(pattern, RegexOptions.IgnoreCase);
            return rgx.IsMatch(path);
            //for(var i = 0; i < Ignores.Count; i++)
            //{
            //    var rgx = new Regex(Ignores[i], RegexOptions.IgnoreCase);
            //    if(rgx.IsMatch(path)) return true;
            //}
            //return false;
        }
    }
}