namespace NextcloudClient.Types
{
    /// <summary>
    /// 
    /// </summary>
    public class Status
    {
        /// <summary>
        /// Gets or sets a value indicating whether the ownCloud instance was installed successfully; 
        /// true indicates a successful installation, and false indicates an unsuccessful installation.
        /// </summary>
        /// <value>
        ///   <c>true</c> if installed; otherwise, <c>false</c>.
        /// </value>
        public bool Installed { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether maintenance mode to disable ownCloud is enabled.
        /// </summary>
        /// <value>
        ///   <c>true</c> if maintenance; otherwise, <c>false</c>.
        /// </value>
        public bool Maintenance { get; set; }

        /// <summary>
        /// Gets or sets the current version number of your ownCloud installation.
        /// </summary>
        /// <value>
        /// The version.
        /// </value>
        public string Version { get; set; }

        /// <summary>
        /// Gets or sets the current version string of your ownCloud installation.
        /// </summary>
        /// <value>
        /// The version string.
        /// </value>
        public string VersionString { get; set; }

        /// <summary>
        /// Gets or sets the edition.
        /// </summary>
        /// <value>
        /// The edition.
        /// </value>
        public string Edition { get; set; }
    }
}
