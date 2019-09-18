using System.Text;
using System.Xml;
using Microsoft.OData.Edm;

namespace CodeCore.ODataClient.Abstract
{
    public abstract class ProxyGeneratorBase
    {

        public IEdmModel EdmModel { get; private set; }

        protected StringBuilder StringBuilder { get; } = new StringBuilder();

        public void Initialize(string uri)
        {
            // Reset the generator
            EdmModel = null;
            StringBuilder.Length = 0;

            // Initialize the data model from the url proided.
            using (var xmlReader = XmlReader.Create(uri))
            {
                EdmModel = Microsoft.OData.Edm.Csdl.CsdlReader.Parse(xmlReader);
            }
        }

        public void PrepareTarget(string outputPath, bool initialize)
        {
            TargetPath = outputPath;
            PrepareTarget(initialize);
        }

        protected abstract void PrepareTarget(bool initialize);


        protected string TargetPath { get; private set; }

        public abstract void Generate(string targetNamespace, string contextName);

    }
}
