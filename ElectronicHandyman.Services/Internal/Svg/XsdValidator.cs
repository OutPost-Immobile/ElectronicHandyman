using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;

namespace Services.Internal.Svg;

public class XsdValidator
{
    private const string Namespace = "http://www.w3.org/2000/svg";
    private const string SchemaUri = "https://github.com/dumistoklus/svg-xsd-schema/blob/master/svg.xsd";
    
    public static async Task ValidateSvgFile(XDocument doc)
    {
        var schemas = new XmlSchemaSet
        {
            XmlResolver = new XmlUrlResolver()
        };
        
        schemas.Add(Namespace, SchemaUri);
        schemas.Compile();
        
        doc.Validate(schemas, (_, e) =>
        {
            if (e.Severity == XmlSeverityType.Error)
            {
                throw new InvalidOperationException(e.Message);
            }
        }, addSchemaInfo: true);
        
        await Task.CompletedTask;
    }
}