using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NLRBScraper
{
    internal partial class CookieGenerator
    {
        public static string CreateCookie()
        {
            string token = "xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx";
            token = GeneratedRegEx().Replace(token, (match) =>
            {
                int r = new Random().Next(16);
                int v = match.Value == "x" ? r : (r & 0x3 | 0x8);
                return v.ToString("x");
            });
            return token;
        }

        [GeneratedRegex("[xy]")]
        private static partial Regex GeneratedRegEx();
    }
}
