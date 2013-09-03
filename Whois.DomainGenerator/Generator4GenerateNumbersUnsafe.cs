using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Whois.DomainGenerator
{
    public class Generator4GenerateNumbersUnsafe : IGenerator
    {
        public List<string> Generate(string dic, int length)
        {
            var result = new List<string>();
            unsafe
            {
                IntPtr p = Marshal.AllocHGlobal(sizeof(char));
                var seed = (char*)p.ToPointer();
                int i = 0;
                for (;i < length;i++)
                {
                    *(seed + i) = dic[0];
                }
                *(seed + i) = '\0';

                do
                {
                    result.Add(new string(seed));
                } while (GetNext(ref seed, length - 1, dic));
            }

            return result;
        }

        private unsafe bool GetNext(ref char* seed, int idx, string dic)
        {
            int len = 0;
            for (;*(seed + len) != '\0';len++) { }
            if (idx < 0 || idx >= len)
                return false;

            char c = seed[idx];
            if (dic.IndexOf(c) < dic.Length - 1)
            {
                c = dic[dic.IndexOf(c) + 1];
                *(seed + idx) = c;
                return true;
            }
            else
            {
                c = dic[0];
                *(seed + idx) = c;
                return GetNext(ref seed, idx - 1, dic);
            }
        }

        public override string ToString()
        {
            return "Generate numbers(unsafe)";
        }
    }
}