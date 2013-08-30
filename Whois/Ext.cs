using System;

namespace Whois
{
    internal static class Ext
    {
        public static string ToStringExt(this Tuple<string, string> tuple)
        {
            return string.Format("{0}{1}", tuple.Item1, tuple.Item2);
        }
    }
}