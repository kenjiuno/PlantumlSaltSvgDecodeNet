using CommandLine;
using CommandLine.Text;
using System.Text;
using System.Xml;

namespace PlantumlSaltSvgDecodeNet
{
    internal class Program
    {
        private class DecodeOpt
        {
            [Value(0, Required = true, HelpText = "Input svg file path")]
            public string? InputSvg { get; set; }

            [Value(1, Required = true, HelpText = "Output svg file path")]
            public string? OutputSvg { get; set; }
        }

        static void Main(string[] args)
            => Parser.Default.ParseArguments<DecodeOpt>(args)
                .MapResult<DecodeOpt, int>(
                    DoDecode,
                    ex => 1
                );

        private static int DoDecode(DecodeOpt opt)
        {
            var dom = new XmlDocument();
            dom.Load(opt.InputSvg ?? throw new NullReferenceException("InputSvg"));
            var ns = new XmlNamespaceManager(dom.NameTable);
            ns.AddNamespace("svg", "http://www.w3.org/2000/svg");
            ns.AddNamespace("xlink", "http://www.w3.org/1999/xlink");
            var newLayer = dom.CreateElement("g", "http://www.w3.org/2000/svg");
            newLayer.SetAttribute("id", "salt_layer");
            dom.DocumentElement!.AppendChild(newLayer);
            foreach (var imageElement in dom.SelectNodes(".//svg:image", ns)?.Cast<XmlElement>() ?? Array.Empty<XmlElement>())
            {
                var xlinkHref = imageElement.GetAttribute("xlink:href");
                if (xlinkHref.StartsWith("data:image/svg+xml;base64,"))
                {
                    var svgElementString = Encoding.UTF8.GetString(Convert.FromBase64String(xlinkHref.Substring(26)));

                    var innerSvg = new XmlDocument();
                    innerSvg.LoadXml(svgElementString);

                    foreach (var child in innerSvg.SelectNodes("/svg:svg/svg:g", ns)?.Cast<XmlElement>() ?? Array.Empty<XmlElement>())
                    {
                        var importedChild = (XmlElement)dom.ImportNode(child, true);
                        var imageElementX = imageElement.GetAttribute("x");
                        var imageElementY = imageElement.GetAttribute("y");
                        importedChild.SetAttribute("transform", $"translate({imageElementX}, {imageElementY})");
                        newLayer.AppendChild(importedChild);
                    }

                    imageElement.ParentNode!.RemoveChild(imageElement);
                }
            }

            using var writer = XmlWriter.Create(
                opt.OutputSvg ?? throw new NullReferenceException("OutputSvg"),
                new XmlWriterSettings
                {
                    Encoding = Encoding.UTF8,
                    Indent = true,
                }
            );
            dom.Save(writer);

            return 0;
        }
    }
}
