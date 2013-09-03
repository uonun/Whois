using System;
using System.Collections.Generic;

namespace Whois.DomainGenerator
{
    public class Generator3LoopAsNumbers : IGenerator
    {
        public List<string> Generate(string dic, int length)
        {
            var fromBase = dic.Length;
            int min = 0, max = 0;
            if (fromBase > 1)
            {
                max = (int)Math.Pow(fromBase, length) - 1;
            }
            else
            {
                max = min;
            }

            //Console.WriteLine("min = {0}, max = {1}", min, max);

            var result = new List<string>();
            for (var current = min;current <= max;current++)
            {
                var tmp = "";
                for (var j = length - 1;j >= 0;j--)
                {
                    var last = (int)(current % Math.Pow(fromBase, j + 1) / Math.Pow(fromBase, j));
                    tmp += dic[last];
                }

                result.Add(tmp);
            }

            return result;
        }

        public override string ToString()
        {
            return "Loop as numbers";
        }
    }
}