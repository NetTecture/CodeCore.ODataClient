using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace CodeCore.Configuration
{

    /// <summary>
    /// This is the non-generic base class for all configuration managers. It contains configuration
    /// data that is either hardcoded for a product, or provides default values to fall back onto.
    /// </summary>
    public abstract class ConfigurationManager {

        /// <summary>
        /// Indicates that the manager already has been initialized. THis is not a bool because
        /// bool is not supported by lock free interlocked methods.
        /// </summary>
        static Int32 _isInitialized = 0;

        /// <summary>
        /// Property indicating whether the ConfigurationManager is initialized.
        /// </summary>
        public static Boolean IsInitialized => _isInitialized != 0;

        public static String Container { get; private set; } = null;

        /// <summary>
        /// The configuration root. If not set here, the registry is consulted. If nothing
        /// is found there, the ultimate fallback is to the app config folder.
        /// </summary>
        public static String ConfigurationRoot { get; set; } = null;

        public static String Product { get; private set; } = null;

        public static String Variant { get; private set; } = null;

        public const String CONFIGURATIONKEY = "Configuration";

        public const String VARIANTKEY = "Variant";

        /// <summary>
        /// Initializes the configuration subsystem.
        /// </summary>
        /// <param name="producer"></param>
        /// <param name="product"></param>
        /// <param name="pathMode"></param>
        /// <param name="configurationRoot"></param>
        /// <returns>true if initilized, false if aborted as it already was initialized</returns>
        public static Boolean Initialize(
            String producer = null,
            String product = null,
            PathMode pathMode = PathMode.Simple,
            String configurationRoot = null
        ) {
            // First: check for redundant call. This is a one off only.
            var repeated = Interlocked.Exchange(ref _isInitialized, 1);
            if (repeated != 0) {
                return false;
            }

            // Next: we generate the container name out of producer and product.

            var builder = new StringBuilder();
            if (!String.IsNullOrWhiteSpace(producer)) {
                builder.Append(producer.Trim()).Append(".");
            }
            if (!String.IsNullOrWhiteSpace(product)) {
                switch (pathMode) {
                    case PathMode.Simple:
                        builder.Append(product.Trim()).Append(".");
                        break;
                }
            }
            if (builder.Length > 1) {
                builder.Length -= 1;
            }
            if (builder.Length == 0) {
                throw new ArgumentException("producer or product must be set");
            }
            Container = builder.ToString();

            // Next: Product
            if (!String.IsNullOrWhiteSpace(product)) {
                switch (pathMode) {
                    case PathMode.Prefix:
                        Product = product;
                        break;
                }
            }

            // Next: find the root folder where the configuration is stored. This is named by the Container
            // variable. It can be in various places, including explicit pointing us to some - so we need to search.

            // If we have a container explicit, we use that one.
            if (!String.IsNullOrWhiteSpace(configurationRoot)) {
                ConfigurationRoot = configurationRoot;
            }
            ConfigurationRoot = Path.GetFullPath(GetConfigurationRoot());

            // Now, the variant.
            Variant = GetVariant();
            return true;
        }

        static string GetConfigurationRoot ()
        {
            return SelectConfigurationRoots()
                .Where(x => !String.IsNullOrWhiteSpace(x))
                .FirstOrDefault();
        }

        static IEnumerable<String> SelectConfigurationRoots()
        {
            // Check environment variable...
            if (String.IsNullOrWhiteSpace(ConfigurationRoot))
            {
                yield return Environment.GetEnvironmentVariable("Config.Root");
            }
            // No container? Let's look for a key... Registry (current user)
            if (String.IsNullOrWhiteSpace(ConfigurationRoot) && !String.IsNullOrWhiteSpace(Container))
            {
                using (var baseKey = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64))
                using (var key = baseKey.OpenSubKey(String.Format(@"SOFTWARE\{0}", Container)))
                {
                    yield return (String)key?.GetValue(CONFIGURATIONKEY, null);
                }
            }
            // No container? Let's look for a key in.... Registry (local machine)
            if (String.IsNullOrWhiteSpace(ConfigurationRoot) && !String.IsNullOrWhiteSpace(Container))
            {
                using (var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
                using (var key = baseKey.OpenSubKey(String.Format(@"SOFTWARE\{0}", Container)))
                {
                    yield return (String)key?.GetValue(CONFIGURATIONKEY, null);
                }
            }
            // No container? Is there a config folder? in LocalApplicationData?
            if (String.IsNullOrWhiteSpace(ConfigurationRoot))
            {
                var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Container);
                if (Directory.Exists(path))
                {
                    yield return path;
                }
            }
            // No container? Well, is there a "Configurations" around? Directly?
            if (String.IsNullOrWhiteSpace(ConfigurationRoot))
            {
                var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Configurations");
                if (Directory.Exists(path))
                {
                    yield return path;
                }
            }
            // No container? Well, is there a "Configurations" around? In App_Data?
            if (String.IsNullOrWhiteSpace(ConfigurationRoot))
            {
                var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data", "Configurations");
                if (Directory.Exists(path))
                {
                    yield return path;
                }
            }
            // No container? Well, is there a "Configurations" around? One ore more levels higher?
            if (String.IsNullOrWhiteSpace(ConfigurationRoot))
            {
                var path = AppDomain.CurrentDomain.BaseDirectory;
                while (!String.IsNullOrWhiteSpace(path))
                {
                    var configPath = Path.Combine(path, "Configurations");
                    if (Directory.Exists(configPath))
                    {
                        ConfigurationRoot = configPath;
                        break;
                    }
                    path = Directory.GetParent(path)?.FullName;
                }
            }
            // LAST resort... where I am.
            if (String.IsNullOrWhiteSpace(ConfigurationRoot))
            {
                yield return AppDomain.CurrentDomain.BaseDirectory;
            }
            // Terminate possible relative elements AND make sure we have access to the folder. 
            ConfigurationRoot = Path.GetFullPath(ConfigurationRoot);
        }

        /// <summary>
        /// Gets the current variant, or null if there is none. Internally this method calls all possible elements
        /// from SelectVariants until it finds one matching.
        /// </summary>
        /// <returns></returns>
        static string GetVariant()
        {
            return SelectVariants()
                .Where(x => !String.IsNullOrWhiteSpace(x))
                .FirstOrDefault();
        }

        /// <summary>
        /// This methods selects the current variant. it uses a number of possible locations to find the active variant-
        /// </summary>
        /// <returns></returns>
        static IEnumerable<String> SelectVariants ()
        {
            // We check the environment variable.
            yield return Environment.GetEnvironmentVariable("Config.Variant");

            // We check the user specific registry
            using (var baseKey = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64))
            using (var key = baseKey.OpenSubKey(String.Format(@"SOFTWARE\{0}", Container)))
            {
                yield return (String)key?.GetValue(VARIANTKEY, null);
            }

            using (var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
            using (var key = baseKey.OpenSubKey(String.Format(@"SOFTWARE\{0}", Container)))
            {
                yield return (String)key?.GetValue(VARIANTKEY, null);
            }
        }

    }

}
