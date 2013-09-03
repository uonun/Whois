using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Whois.DomainGenerator
{
    public class Generator2LoopSegmentsUnsafe : IGenerator
    {
        public unsafe List<string> Generate(string dic, int length)
        {
            char*[] result = null;
            for (int i = 0;i < length;i++)
            {
                result = Loop(dic, result);
            }

            var list = new List<string>();
            if (result != null)
            {
                foreach (var p in result)
                {
                    var aa = new String(p);
                    list.Add(aa);
                }
            }
            return list;
        }

        private unsafe char*[] Loop(string dic, char*[] seed)
        {
            char*[] result = null;
            if (seed == null || seed.Length == 0)
            {
                result = new char*[dic.Length];
                // for the first time, we just choose one char only to fill full into the List.
                for (int i = 0;i < dic.Length;i++)
                {
                    IntPtr p = Marshal.AllocHGlobal(sizeof(char));
                    result[i] = (char*)p.ToPointer();
                    *result[i] = dic[i];
                    // string will/should be end with the char '\0'.
                    *(result[i] + 1) = '\0';
                }
            }
            else
            {
                result = new char*[seed.Length * dic.Length];
                int n = 0;
                for (int i = 0;i < seed.Length;i++)
                {
                    foreach (var c in dic)
                    {
                        IntPtr p = Marshal.AllocHGlobal(sizeof(char));
                        result[n] = (char*)p.ToPointer();
                        *(result[n]) = *seed[i];

                        int j = 0;
                        // string will/should be end with the char '\0'.
                        while (*(seed[i] + ++j) != '\0')
                        {
                            *(result[n] + j) = *(seed[i] + j);
                        }
                        *(result[n] + j) = c;
                        // string will/should be end with the char '\0'.
                        *(result[n] + j + 1) = '\0';
                        n++;
                    }
                }
            }
            return result;
        }

        public override string ToString()
        {
            return "Loop segments(unsafe)";
        }
    }
}