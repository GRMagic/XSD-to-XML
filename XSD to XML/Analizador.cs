using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace XSD_to_XML
{
    class Analizador
    {

        private string arquivoXsd;

        Dictionary<string, XElement> tipos;
        //List<XElement> tiposDesconhecidos;
        
        ///List<XElement> fantasmas;
        //Dictionary<XElement, string> restricoesDesconhecidas;
        bool gerarComentarios;

        public void GerarXMLVazio(string arquivoXsd, string pastaDestino, bool gerarComentarios = true)
        {
            this.gerarComentarios = gerarComentarios;
            this.tipos = new Dictionary<string, XElement>();
            //this.tiposDesconhecidos = new List<XElement>();
            //this.fantasmas = new List<XElement>();
            //this.restricoesDesconhecidas = new Dictionary<XElement, string>();
            this.arquivoXsd = arquivoXsd;
            var xsd = XDocument.Load(arquivoXsd);
            var resultado = Analisar(xsd.Root, new Historico());
            foreach(var e in resultado) Reconhecer(e);
            for (int i = 0; i < resultado.Count; i++){
                try
                {
                    var xml = new XDocument();
                    xml.Add(resultado[i]);
                    xml.Save(System.IO.Path.Combine(pastaDestino, i+".xml" ));
                }
                catch
                {
                    Console.WriteLine("O seguinte xml não pode ser salvo:");
                    Console.WriteLine(resultado[i].ToString());
                }
            }
        }

        private List<XElement> Analisar(XElement element, Historico historico)
        {
            var lista = new List<XElement>();
            switch (element.Name.LocalName.ToLower())
            {
                case "schema":
                    lista.AddRange(Schema(element, historico));
                    break;
                case "import":
                    Include(element, historico);
                    break;
                case "include":
                    //lista.AddRange(Include(element, historico));
                    Include(element, historico);
                    break;
                case "complextype":
                    lista.AddRange(ComplexType(element, historico));
                    break;
                case "simpletype":
                    lista.AddRange(SimpleType(element, historico));
                    break;
                case "annotation":
                    Annotation(element, historico);
                    break;
                case "sequence":
                    lista.AddRange(Sequence(element, historico));
                    break;
                case "element":
                    lista.Add(Element(element, historico));
                    break;
                case "attribute":
                    Attribute(element, historico);
                    break;
                case "restriction":
                    Restriction(element, historico);
                    break;
                case "choice":
                    lista.AddRange(Choice(element, historico));
                    break;
                default:
                    //throw new Exception("Elemento desconhecido: " + element.Name.LocalName);
                    Console.WriteLine("Elemento desconhecido: " + element.Name.LocalName);
                    break;
            }
            return lista;
        }

        private List<XElement> Navegar(XElement element, Historico historico)
        {
            var lista = new List<XElement>();
            foreach (var descendant in element.Elements())
            {
                lista.AddRange(Analisar(descendant, historico));
            }
            return lista;
        }

        private void Reconhecer(XElement root)
        {
            // Reconhecimento de nós que não tinham sido declarados
            foreach (var elemento in root.DescendantNodesAndSelf().OfType<XElement>().Where(e => e.Attributes("TipoDesconhecidoNaoIdentificadoEmNenhumaAnalise").Any()))
            {
                var atributo = elemento.Attribute("TipoDesconhecidoNaoIdentificadoEmNenhumaAnalise");
                var tipo = atributo.Value;
                atributo.Remove();
                if (tipos.ContainsKey(tipo))
                {
                    Console.WriteLine("Tipo reconhecido: " + tipo);
                    elemento.ReplaceNodes(tipos[tipo].Nodes());
                    elemento.ReplaceAttributes(tipos[tipo].Attributes());
                }
                else
                {
                    Console.WriteLine("Tipo definitivamente desconhecido: " + tipo);
                }
            }
            //foreach(var elemento in tiposDesconhecidos)
            //{
            //    var tipo = elemento.Value;
            //    if (tipos.ContainsKey(tipo))
            //    {
            //        Console.WriteLine("Tipo reconhecido: " + tipo);
            //        elemento.ReplaceNodes(tipos[tipo].Nodes());
            //        elemento.ReplaceAttributes(tipos[tipo].Attributes());
            //    }
            //    else
            //    {
            //        Console.WriteLine("Tipo definitivamente desconhecido: " + tipo);
            //    }
            //}
            // Reconhecimento de restrições que tinham herança

            foreach (var elemento in root.DescendantNodesAndSelf().OfType<XElement>().Where(e => e.Attributes("RestricaoDesconhecidaNaoIdentificadoEmNenhumaAnalise").Any()))
            {
                var atributo = elemento.Attribute("RestricaoDesconhecidaNaoIdentificadoEmNenhumaAnalise");
                var tipo = atributo.Value;
                atributo.Remove();
                if (tipos.ContainsKey(tipo))
                {
                    var comentarios = tipos[tipo].DescendantNodesAndSelf().Where(n => n.NodeType == System.Xml.XmlNodeType.Comment);
                    elemento.Add(comentarios); // Todo: Aqui não está indentando, corrigir.
                }
                else
                {
                    Console.WriteLine("Tipo definitivamente desconhecido: " + tipo);
                }
            }
            //foreach (var item in restricoesDesconhecidas)
            //{
            //    if (tipos.ContainsKey(item.Value))
            //    {
            //        var comentarios = tipos[item.Value].DescendantNodesAndSelf().Where(n => n.NodeType == System.Xml.XmlNodeType.Comment);
            //        item.Key.Add(comentarios); // Todo: Aqui não está indentando, corrigir.
            //    }else
            //    {
            //        Console.WriteLine("Restrição definitivamente desconhecida: " + item.Value);
            //    }
            //}
        }

        #region Ações

        private List<XElement> Schema(XElement element, Historico historico)
        {
            //Todo: Colocar no namespace no elemento
            historico.ns = element.Attribute("targetNamespace").Value;
            return Navegar(element, historico);
        }

        private List<XElement> Include(XElement element, Historico historico)
        {
            var pastaXsd = System.IO.Path.GetDirectoryName(arquivoXsd);
            var schemaLocation = element.Attribute("schemaLocation").Value;
            var caminho = System.IO.Path.IsPathRooted(schemaLocation)? schemaLocation : System.IO.Path.Combine(pastaXsd, schemaLocation);
            element.Add(XDocument.Load(caminho).Root);
            return Navegar(element, historico);
        }

        private List<XElement> ComplexType(XElement element, Historico historico)
        {
            try
            {
                var nome = element.Attribute("name")?.Value ?? element.Attribute("ref")?.Value;
                var nomeLocal = nome?.Split(':').Last();
                historico.elemento = new XElement(nomeLocal);
                tipos.Add(nome, historico.elemento);
            }
            catch
            {
                // Sem nome
            }
            return Navegar(element, historico);
        }

        private List<XElement> SimpleType(XElement element, Historico historico)
        {
            try
            {
                var nome = element.Attribute("name")?.Value ?? element.Attribute("ref")?.Value;
                var nomeLocal = nome?.Split(':').Last();
                historico.elemento = new XElement(nomeLocal);
                historico.elemento.Value = "?";
                tipos.Add(nome, historico.elemento);
            }
            catch
            {
                // Sem nome
            }
            return Navegar(element, historico);
        }

        private List<XElement> Sequence(XElement element, Historico historico)
        {
            // Cria um nó fictício apenas para conter os filhos e comentários agrupados
            var original = historico.elemento;
            historico.elemento = new XElement("Sequence");
            //
            historico.elemento.Add(new XComment(" ____ Começo de uma sequência ____ "));
            var lista = Navegar(element, historico);
            historico.elemento.Add(new XComment(" ____ Final de uma sequência ____ "));
            //
            original.Add(historico.elemento.Nodes());
            //
            return lista;
        }

        private XElement Element(XElement element, Historico historico)
        {
            var nomeNovoElemento = element.Attribute("name")?.Value?? element.Attribute("ref")?.Value?.Split(':').Last();

            XElement novoElemento = new XElement(nomeNovoElemento);
            var tipo = element.Attributes("type").FirstOrDefault();
            if(tipo != null)
            {
                if(tipos.ContainsKey(tipo.Value))
                {
                    novoElemento = new XElement(tipos[tipo.Value]);
                    novoElemento.Name = nomeNovoElemento;
                }else
                {
                    Console.WriteLine("Tipo desconhecido: " + tipo.Value);
                    novoElemento.Value = tipo.Value;
                    novoElemento.Add(new XAttribute("TipoDesconhecidoNaoIdentificadoEmNenhumaAnalise", tipo.Value));
                    //tiposDesconhecidos.Add(novoElemento);
                }
            }
            var minOccurs = element.Attributes("minOccurs").FirstOrDefault()?.Value;
            var maxOccurs = element.Attributes("maxOccurs").FirstOrDefault()?.Value;
            if (!string.IsNullOrEmpty(minOccurs) || !string.IsNullOrEmpty(maxOccurs))
            {
                novoElemento.Add(new XComment(string.Format("Ocorrências: de {0} a {1} elementos", minOccurs ?? "1", maxOccurs ?? "?")));
            }

            if (historico.elemento != null)
            {
                historico.elemento.Add(novoElemento);
            }
            historico.elemento = novoElemento;
            var lista = Navegar(element, historico);
            return novoElemento;
        }

        private List<XElement> Choice(XElement element, Historico historico)
        {
            // Cria um nó fictício apenas para conter os filhos e comentários agrupados
            var original = historico.elemento;
            historico.elemento = new XElement("Choice");

            // Incluí os comentádios e processa os filhos
            historico.elemento.Add(new XComment(" ____ Começo de uma escolha ____ "));
            var lista = Navegar(element, historico);
            historico.elemento.Add(new XComment(" ____ Final de uma escolha ____ "));
            
            // Coloca os filhos no nó original
            original.Add(historico.elemento.Nodes());

            // Não preicisa retornar para o nó original, pois histórico é uma variável local que não vai mais ser usada antes do return
            
            return lista;
        }

        private void Annotation(XElement element, Historico historico)
        {
            if(gerarComentarios)historico.elemento.AddFirst(new XComment(element.Value));
        }

        private void Attribute(XElement element, Historico historico)
        {
            var nome = element.Attribute("name").Value;
            //var restricoes = element.Elements().Where(d => d.Name.LocalName == "restriction");
            //Todo: Verificar as restrições do atributo e criar um valor de exemplo
            var attribute = new XAttribute(nome, "?");
            historico.elemento?.Add(attribute);
            var use = element.Attributes("use").FirstOrDefault()?.Value;
            historico.elemento?.AddFirst(new XComment(string.Format("Uso do atributo {0}: {1}", nome, use)));
        }

        private void Restriction(XElement element, Historico historico)
        {
            if (!gerarComentarios) return; // tudo que é gerado pelas restrições são comentários, se não quer gerar, então nem precisa processar...
            var enumeration = element.Descendants().FirstOrDefault(d => d.Name.LocalName == "enumeration");
            if (enumeration != null)
            {
                historico.elemento.Add(new XComment("Exemplo: " + enumeration.Attribute("value")?.Value));
            }
            var pattern = element.Descendants().FirstOrDefault(d => d.Name.LocalName == "pattern");
            if (pattern != null)
            {
                var regex = pattern.Attribute("value")?.Value;
                try
                {
                    bool temCaractereEspecial = true;
                    string exemplo = "";
                    for(int i=0; i<10 && temCaractereEspecial; i++) {
                        exemplo = new Fare.Xeger(regex).Generate();
                        temCaractereEspecial = false;
                        for (int j=0; j<exemplo.Length; j++)
                        {
                            if (exemplo[j] < 32)
                            {
                                temCaractereEspecial = true;
                                break;
                            }
                        }
                    }
                    historico.elemento.Add(new XComment("Exemplo: " + exemplo));
                }
                catch
                {
                    historico.elemento.Add(new XComment("Pattern: " + regex));
                }
            }
            var minLength = element.Descendants().FirstOrDefault(d => d.Name.LocalName == "minLength")?.Attribute("value")?.Value;
            var maxLength = element.Descendants().FirstOrDefault(d => d.Name.LocalName == "maxLength")?.Attribute("value")?.Value;
            if (!string.IsNullOrEmpty(minLength) || !string.IsNullOrEmpty(maxLength))
            {
                historico.elemento.Add(new XComment(string.Format("Tamanho: de {0} a {1} caracteres", minLength??"0", maxLength??"infinitos")));
            }
            var atributoBase = element.Attribute("base")?.Value;
            if (!string.IsNullOrEmpty(atributoBase))
            {
                //restricoesDesconhecidas.Add(historico.elemento, atributoBase);
                historico.elemento.Add(new XAttribute("RestricaoDesconhecidaNaoIdentificadoEmNenhumaAnalise", atributoBase));
            }

        }
        
        #endregion

        internal struct Historico
        {
            public string ns;
            public XElement elemento;
        }
        
    }
}
