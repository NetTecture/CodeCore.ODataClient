using System;
using System.Xml;
using Microsoft.OData.Edm;

namespace CodeCore.ODataClient.Abstract
{

    public class ODataSettings
    {

        /// <summary>
        /// Path for the output. All classes are generated in this one place
        /// </summary>
        public string Output { get; set; }

        /// <summary>
        /// Indicaates whether the folde is initialized. Initiaizing deletes all files
        /// and copies the client libraries new into the folder. This is mostly a debug
        /// setting, as during development we work on the client libraries - as it is easier.
        /// In this cases we skip initialization.
        /// </summary>
        public Boolean Initialize { get; set; } = true;

        public ServiceSettings[] Services { get; set; }

        public class ServiceSettings {

            /// <summary>
            /// Namespace for the generated elements. Must be unique.
            /// </summary>
            public string Namespace { get; set; }

            /// <summary>
            /// Path (can be http/s) to the metadata document to "control" the generation.
            /// </summary>
            public string Metadata { get; set; }

            /// <summary>
            /// The name to be used for the context that is generated.
            /// </summary>
            public string ContextName { get; set; }

        }

    }

}
