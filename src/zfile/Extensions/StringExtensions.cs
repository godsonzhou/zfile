using System;
using System.Text.RegularExpressions;

namespace zfile
{
    /// <summary>
    /// Extension methods for string operations
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// Checks if a string matches a wildcard pattern
        /// </summary>
        /// <param name="str">The string to check</param>
        /// <param name="wildcard">The wildcard pattern</param>
        /// <returns>True if the string matches the pattern</returns>
        public static bool MatchesWildcard(this string str, string wildcard)
        {
            if (string.IsNullOrEmpty(str) || string.IsNullOrEmpty(wildcard))
                return false;

            // Convert the wildcard to a regex pattern
            string pattern = "^" + Regex.Escape(wildcard)
                .Replace("\\*", ".*")
                .Replace("\\?", ".") + "$";

            return Regex.IsMatch(str, pattern, RegexOptions.IgnoreCase);
        }
    }
}
