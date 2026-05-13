using System.Xml.Linq;
using Services.Internal.Svg.Models;

namespace Services.Abstractions;

public interface ISvgGenerator
{
    Task<XDocument> GenerateSvgDocumentAsync(SymbolModel model);
}