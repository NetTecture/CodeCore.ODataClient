using System;
using System.IO;
using Newtonsoft.Json;

namespace CodeCore.ODataClient.Abstract
{

    public abstract class ProgramAbstract<T> where T : ProxyGeneratorBase, new()
    {
#pragma warning disable IDE0060 // Remove unused parameter
        public static void RealMain(string[] args)
#pragma warning restore IDE0060 // Remove unused parameter
        {
            var filename = "odataconfig.json";

            var path = Directory.GetCurrentDirectory();
            var origPath = path;
            while (true)
            {
                if (File.Exists(Path.Combine(path, filename)))
                {
                    break;
                }
                var dirinfo = Directory.GetParent(path);
                if (dirinfo == null)
                {
                    // End of the line. We are at the root...
                    path = null;
                    break;
                }
                path = Directory.GetParent(path).FullName;
            }

            if (path == default)
            {
                throw new FileNotFoundException($"{filename} not found");
            } else
            {
                Console.WriteLine($"{filename} at {path}");
            }

            // Reading the settings file
            ODataSettings settings = default;
            using (var r = new StreamReader(Path.Combine(path, filename)))
            {
                var json = r.ReadToEnd();
                settings = JsonConvert.DeserializeObject<ODataSettings>(json);
            }

            try
            {
                Directory.SetCurrentDirectory(path);

                var generator = new T();

                generator.PrepareTarget(settings.Output, settings.Initialize);
                foreach (var service in settings.Services)
                {
                    generator.Initialize(service.Metadata);
                    generator.Generate(service.Namespace, service.ContextName ?? "ODataContainer");
                }
            }
            finally
            {
                Directory.SetCurrentDirectory(origPath);
            }

        }
    }

}
