﻿namespace MCPSharp.Model.Capabilities
{
    /// <summary>
    /// Represents the capabilities of a client.
    /// </summary>
    public class ClientCapabilities
    {
        /// <summary>
        /// Gets or sets a value indicating wether roots are supported.
        /// </summary>
        public Dictionary<string, bool> Roots { get; set; } = [];

        /// <summary>
        /// Gets or sets a value indicating whether sampling is supported.
        /// </summary>

        public Dictionary<string, bool> Sampling { get; set; } = [];

        /// <summary>
        /// Gets or sets a value indicating whether tools are supported.
        /// </summary>
        public Dictionary<string, bool> Tools { get; set; } = [];

        /// <summary>
        /// Gets or sets a value indicating whether resources are supported.
        /// </summary>
        public Dictionary<string, bool> Resources { get; set; } = [];

        /// <summary>
        /// Gets or sets a value indicating whether prompts are supported.
        /// </summary>
        public Dictionary<string, bool> Prompts { get; set; } = [];
    }
}
