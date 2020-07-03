using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XSD_to_XML
{
    class Program
    {
        static void Main(string[] args)
        {
#if DEBUG

            //new Analizador().GerarXMLVazio(@"C:\Users\Gustavo\Documents\Inttegra\WRPWeb.com\WRP.Core\Xsd\PL_009_V4\inutNFe_v4.00.xsd", @"C:\Users\Gustavo\Documents\Inttegra\WRPWeb.com\WRP.Core\Xsd\PL_009_V4\");
            new Analizador().GerarXMLVazio(@"C:\Users\Gustavo\Documents\Inttegra\WRPWeb.com\WRP.Core\Xsd\PL_009_V4\nfe_v4.00.xsd", @"C:\Users\Gustavo\Documents\Inttegra\WRPWeb.com\WRP.Core\Xsd\PL_009_V4\");

#else
            if (args.Length != 3)
            {
                Console.WriteLine(
@"Use:
xsd2xml.exe arquivo.xsd pastaDestinho [True|False]
True ou False indica se deve gerar arquivos com comentário
");
                return;
            }
            new Analizador().GerarXMLVazio(args[0], args[1], Boolean.Parse(args[2]));
#endif

        }
    }

}
