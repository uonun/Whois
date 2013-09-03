using System.Collections.Generic;

namespace Whois.DomainGenerator
{
    public class Generator2LoopSegments : IGenerator
    {
        public List<string> Generate(string dic, int length)
        {
            List<string> result = null;
            for (int i = 0;i <= length;i++)
            {
                result = Loop(dic, result);
            }
            return result;
        }

        private List<string> Loop(string dic, List<string> seed)
        {
            var result = new List<string>();
            if (seed == null || seed.Count == 0)
            {
                // for the first time, we just choose one char only to fill full into the List.
                // result.AddRange(from c in dic select c.ToString());
                result.Add(string.Empty);
            }
            else
            {
                for (int i = 0;i < seed.Count;i++)
                {
                    foreach (var c in dic)
                    {
                        result.Add(string.Format("{0}{1}", seed[i], c));
                    }
                }
            }

            return result;
        }

        public override string ToString()
        {
            return "Loop segments";
        }
    }
}