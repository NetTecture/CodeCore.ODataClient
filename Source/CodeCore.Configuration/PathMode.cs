using System;
using System.Collections.Generic;
using System.Text;

namespace CodeCore.Configuration {

    public enum PathMode {

        Undefined = 0,

        /// <summary>
        /// Simple configuration. One folder per product.
        /// </summary>
        Simple = 1,

        /// <summary>
        /// Prefix configuration. Product as file prefix.
        /// </summary>
        Prefix = 2

    }

}
