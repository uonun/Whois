using System.Collections.Generic;

namespace Whois.DomainGenerator
{
    public class Generator4GenerateNumbers : IGenerator
    {
        public List<string> Generate(string dic, int length)
        {
            var result = new List<string>();
            string seed = new string(dic[0], length);
            result.Add(seed);
            while (GetNext(ref seed, seed.Length - 1, dic))
            {
                result.Add(seed);
            }

            return result;
        }

        private bool GetNext(ref string seed, int idx, string dic)
        {
            if (idx < 0 || idx >= seed.Length)
                return false;

            char c = seed[idx];
            if (dic.IndexOf(c) < dic.Length - 1)
            {
                c = dic[dic.IndexOf(c) + 1];
                seed = seed.Substring(0, idx) + c + seed.Substring(idx + 1, seed.Length - 1 - idx);
                return true;
            }
            else
            {
                c = dic[0];
                seed = seed.Substring(0, idx) + c + seed.Substring(idx + 1, seed.Length - 1 - idx);
                return GetNext(ref seed, idx - 1, dic);
            }
        }

        public override string ToString()
        {
            return "Generate numbers";
        }
    }
}