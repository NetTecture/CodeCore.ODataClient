using System;
using System.IO;
using System.Xml;
using CodeCore.OData.ProxyGen.Abstract;
using CodeCore.OData.ProxyGen.CSharp;
using Microsoft.OData.Edm;
using Newtonsoft.Json;

namespace CodeCore.OData.ProxyGen
{

    public abstract class Program
    {

        static void Main(string[] args)
        {
            ProgramAbstract<ProxyGenerator>.RealMain(args);
        }

    }

}
