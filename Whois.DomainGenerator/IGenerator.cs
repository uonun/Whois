using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Whois.DomainGenerator
{
    public interface IGenerator
    {
        List<string> Generate(string dic, int length);
    }
}
