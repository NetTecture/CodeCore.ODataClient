using System;
using System.Collections.Generic;
using System.Text;
using CodeCore.ODataClient.Abstract;

namespace CodeCore.ODataClient.TypeScript
{
    public abstract class Program
    {

        static void Main(string[] args)
        {
            ProgramAbstract<ProxyGenerator>.RealMain(args);
        }

    }
}
