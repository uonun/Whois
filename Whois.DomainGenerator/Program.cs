using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading;

namespace Whois.DomainGenerator
{
    class Program
    {
        private const string DIC = "0123456789abcdefghijklmnopqrstuvwxyz";
        private static int _len = 4;
        private static int _loop = 10;

        static void Main(string[] args)
        {
            Console.WindowWidth = 120;
            Console.WindowHeight = 32;

            while (true)
            {
                Console.Write("Please enter the length of strings(e.g. 2,3,4...Enter for default of {0}): ", _len);

                var read = Console.ReadLine();
                int tmp;
                if (int.TryParse(read, out tmp))
                {
                    _len = tmp;
                }
                Console.Write("Please enter the times of generating(e.g. 2,5,10,50...Enter for default of {0}): ", _loop);
                read = Console.ReadLine();
                if (int.TryParse(read, out tmp))
                {
                    _loop = tmp;
                }

                Console.WriteLine();
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Begin to generate strings");
                Console.ResetColor();
                Console.WriteLine("dic: {0}", DIC);
                Console.WriteLine("len: {0}", _len);
                Console.WriteLine("loop: {0}", _loop);

                var generaters = new IGenerator[]
                    {
                        new Generator1Notimplemented()
                        //, new Generator2LoopSegments()
                        , new Generator2LoopSegmentsUnsafe()
                        //, new Generator3LoopAsNumbers()
                        //, new Generator4GenerateNumbers()
                        , new Generator4GenerateNumbersUnsafe()
                        //, new Generator4GenerateNumbersFixedPoint()
                        //, new Generator4GenerateNumbersFixedPointUnsafe()
                    };

                foreach (var g in generaters)
                {
                    if (g is Generator1Notimplemented) continue;

                    var p = Process.GetCurrentProcess();
                    GC.Collect();
                    Thread.Sleep(1000);
                    var mBegin = p.PrivateMemorySize64;
                    GenerateWithWatch(g);
                    p = Process.GetCurrentProcess();
                    Console.WriteLine("\tMemory: {0:f3} - {1:f3} = {2:f3}M", (double)p.PrivateMemorySize64 / 1024 / 1024, (double)mBegin / 1024 / 1024, (double)(p.PrivateMemorySize64 - mBegin) / 1024 / 1024);
                }

                Console.WriteLine("\r\nAll done.\r\n\r\n\r\n");

            }
        }

        private static void GenerateWithWatch(IGenerator g)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("\r\n{0}: ", g);
            Console.ResetColor();
            Console.Write("Working...\r\n");

            List<string> result = null;
            var sw = new Stopwatch();
            var total = 0L;
            for (int i = 0;i < _loop;i++)
            {
                sw.Reset();
                sw.Start();
                result = g.Generate(DIC, _len);
                sw.Stop();
                total += sw.ElapsedMilliseconds;
                if (i <= 10)
                {
                    Console.WriteLine("\tGot {0} items in {1,7}ms", result.Count, sw.ElapsedMilliseconds);
                    if (i == 10 && _loop > 10)
                    {
                        Console.WriteLine("\t......");
                    }
                }
            }
            Console.WriteLine("\tGenerate finished for {2} times in {0,7}ms, {1,7}ms for each time.", total, total / _loop, _loop);
            if (result != null)
            {
                PrintTheResult(result);
            }
        }

        private static void PrintTheResult(List<string> result)
        {
            const int topLastN = 5;
            if (result.Count > topLastN * 2)
            {
                for (int i = 0;i < topLastN;i++)
                {
                    Console.Write("\t" + result[i]);
                }
                Console.Write("\t......");
                for (int i = result.Count - topLastN;i < result.Count;i++)
                {
                    Console.Write("\t" + result[i]);
                }
            }
            else
            {
                foreach (var s in result)
                {
                    Console.Write("\t" + s);
                }
            }
            Console.WriteLine();
        }
    }
}
