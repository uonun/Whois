using System;

namespace Whois
{
    internal class BzException : Exception
    {
        public BzException(string msg) : base(msg) { }
        public BzException(string msg, Exception innerExeption) : base(msg, innerExeption) { }
    }
}