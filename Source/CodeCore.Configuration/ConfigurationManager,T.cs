using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace CodeCore.Configuration {

    /// <summary>
    /// Used to find configuration file path in registry and load it. This method is not thread safe except for read only operations.
    /// In the intended use an instance will be configured on application start (one thread) and only be read from then on.
    /// </summary>
    public class ConfigurationManager<T> : ConfigurationManager where
        T : new() {

        /// <summary>
        /// Enumerates all file paths possible for a configuration. It does not do any checks whether
        /// the file actually exists..
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<String> GetFilePaths()
        {
            var typeName = typeof(T).Name;
            if (typeName.EndsWith("Configuration", StringComparison.InvariantCultureIgnoreCase))
            {
                var index = typeName.IndexOf("Configuration", StringComparison.InvariantCultureIgnoreCase);
                typeName = typeName.Remove(index);
            }
            var environment = ConfigurationRoot;
            if (String.IsNullOrWhiteSpace(environment))
            {
                environment = default;
            }
            if (!Directory.Exists(environment))
            {
                throw new InvalidOperationException("Configuration Root not set");
            }
            var variant = Variant;
            if (String.IsNullOrWhiteSpace(Variant))
            {
                variant = null;
            }

            if (variant == default)
            {
                yield return Path.Combine(environment, "@default", $"{typeName}.config");
                yield return Path.Combine(environment, $"{typeName}.config");
            }
            yield return Path.Combine(environment, variant, $"{typeName}.config");
            yield return Path.Combine(environment, $"{typeName}.{variant}.config");
            yield return Path.Combine(environment, "@default", $"{typeName}.config");
            yield return Path.Combine(environment, $"{typeName}.config");
            yield return Path.Combine(environment, "@default", $"{typeName}.config");
        }

        /// <summary>
        /// Returns the file path for a given configuration file. This is the topmost possible file, if multiple
        /// possible fiels exist. The list of possible files evaluated is from GetFilePaths.
        /// </summary>
        /// <returns></returns>
        static String GetFilePath() =>  GetFilePaths().Where(x => File.Exists(x)).FirstOrDefault();

        /// <summary>
        ///  This will load a configuration as per the GetFilePath(s) rules.
        /// </summary>
        /// <param name="setInstance">Indicates whether the stastic instance should be set as loaded.</param>
        /// <returns></returns>
        public static T Load(Boolean setInstance = true) {
            var filePath = GetFilePath();
            return Load(filePath, setInstance);
        }

        /// <summary>
        ///  This will load a specific configuration from a specific location.
        /// </summary>
        /// <param name="file">Fie path to the file</param>
        /// <param name="setInstance">Indicates whether the stastic instance should be set as loaded.</param>
        /// <returns></returns>
        public static T Load(String file, Boolean setInstance = false) {
            XmlSerializer serializer = new XmlSerializer(typeof(T));
            if (!File.Exists(file)) {
                return default;
            }
            using (var filestream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                T item = (T)serializer.Deserialize(filestream);
                InstancePath = file;
                if (setInstance) {
                    Instance = item;
                }
                return item;
            }
        }

        /// <summary>
        /// Resets the configuration manager, deleting any reference and data loaded so far. 
        /// Use case is mostly for test cases, which may want to test out various configuration versions and
        /// need to reset the manager in between.
        /// </summary>
        public static void Reset ()
        {
            Instance = default;
            InstancePath = default;
        }

        /// <summary>
        /// Returnes the standard instance of the configuration.
        /// </summary>
        /// <returns></returns>
        public static T GetInstance() {
            T instance = Instance;
            if (instance == null) {
                instance = Load();
                Instance = instance;
            }
            return instance;
        }

        public static T Instance { get; private set; }

        /// <summary>
        /// Returnes the path of the configuration instance that is loaded.
        /// </summary>
        public static String InstancePath { get; private set; }

        public static T Initialize() {
            T newInstance = new T();
            Instance = newInstance;
            return newInstance;
        }

    }

}
