using System;
using System.Collections.Generic;
using System.Text;
using CodeCore.ODataClient.Abstract;

namespace CodeCore.ODataClient.DotNet
{
    class Program
    {
        static void Main(string[] args)
        {
            ProgramAbstract<ProxyGenerator>.RealMain(args);
        }
    }
}
