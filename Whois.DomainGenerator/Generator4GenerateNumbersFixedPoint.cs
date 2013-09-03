using System;
using System.Collections.Generic;

namespace Whois.DomainGenerator
{
    public class Generator4GenerateNumbersFixedPoint : IGenerator
    {
        public List<string> Generate(string dic, int length)
        {
            var result = new List<string>();
            var seed = new string(dic[0], length);
            while (!string.IsNullOrEmpty(seed))
            {
                result.Add(seed);

                seed = FixedPointCombinator(
                    x =>
                    {
                        if (x != null)
                        {
                            return (s, idx, idc) =>
                            {
                                if (idx < 0 || idx >= s.Length)
                                    return null;

                                char c = s[idx];
                                if (dic.IndexOf(c) < dic.Length - 1)
                                {
                                    c = dic[dic.IndexOf(c) + 1];
                                    s = s.Substring(0, idx) + c + s.Substring(idx + 1, s.Length - 1 - idx);
                                }
                                else
                                {
                                    c = dic[0];
                                    s = s.Substring(0, idx) + c + s.Substring(idx + 1, s.Length - 1 - idx);
                                    s = x(s, idx - 1, dic);
                                }

                                return s;
                            };
                        }

                        return null;
                    })(seed, seed.Length - 1, dic);
            }

            return result;
        }

        private Func<string, int, string, string> FixedPointCombinator(
            Func<Func<string, int, string, string>, Func<string, int, string, string>> f1)
        {
            return (x, y, z) => f1(FixedPointCombinator(f1))(x, y, z);
        }

        public override string ToString()
        {
            return "Generate numbers(Fixed-Point Y)";
        }
    }
}