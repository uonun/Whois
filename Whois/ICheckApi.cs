using System.Text.RegularExpressions;

namespace Whois
{
    internal interface ICheckApi
    {
        string Url { get; }
        string PatternAvailable { get; }
        string PatternUnavailable { get; }
    }

    class CheckApi_RWEN : ICheckApi
    {
        public string Url { get; private set; }
        public string PatternAvailable { get; private set; }
        public string PatternUnavailable { get; private set; }

        public CheckApi_RWEN()
        {
            Url = "http://sys.rwen.com/style/info/newxhccl.asp?domain={0}";
            PatternAvailable = "value%3D{0}%20%3E%20{0}%3C/li%3E";
            PatternUnavailable = "value%3D{0}%20disabled%3D%22disabled%22";
        }
    }

    class CheckApi_HICHINA : ICheckApi
    {
        public string Url { get; private set; }
        public string PatternAvailable { get; private set; }
        public string PatternUnavailable { get; private set; }

        public CheckApi_HICHINA()
        {
            Url = "http://pandavip.www.net.cn/check/check_ac1.cgi?domain={0}";
            PatternAvailable = "Domain name is available";
            PatternUnavailable = "Domain name is not available";
        }
    }

    
}