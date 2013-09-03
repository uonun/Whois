using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Whois.DomainGenerator
{
    public class Generator4GenerateNumbersFixedPointUnsafe : IGenerator
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

                bool tmp = true;
                while (tmp)
                {
                    result.Add(new string(seed));
                    tmp = FixedPointCombinator(
                        x =>
                        {
                            if (x != null)
                            {
                                return (s) =>
                                {
                                    int len = 0;
                                    for (;*(s.Seed + len) != '\0';len++) { }
                                    if (s.Idx < 0 || s.Idx >= len)
                                        return false;

                                    char c = s.Seed[s.Idx];
                                    if (s.Dic.IndexOf(c) < s.Dic.Length - 1)
                                    {
                                        c = s.Dic[s.Dic.IndexOf(c) + 1];
                                        *(s.Seed + s.Idx) = c;
                                        return true;
                                    }
                                    else
                                    {
                                        c = s.Dic[0];
                                        *(s.Seed + s.Idx) = c;
                                        s.Idx--;
                                        return x(s);
                                    }
                                };
                            }

                            return null;
                        })(new Args() { Seed = seed, Idx = length - 1, Dic = dic });
                }
            }

            return result;
        }

        private Func<Args, bool> FixedPointCombinator(
            Func<Func<Args, bool>, Func<Args, bool>> f1)
        {
            return (x) => f1(FixedPointCombinator(f1))(x);
        }

        public override string ToString()
        {
            return "Generate numbers(Fixed-Point Y, unsafe)";
        }

        private unsafe struct Args
        {
            public char* Seed;
            public int Idx;
            public string Dic;
        }
    }
}