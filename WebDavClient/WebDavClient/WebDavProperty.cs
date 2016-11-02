namespace WebDavClient
{
    /// <summary>
    ///     All supported DAV Properties according to RFC 2518 - http://www.ietf.org/rfc/rfc2518.txt
    /// </summary>
    public enum WebDavProperty
    {
        /// <summary>
        ///     Creation date.
        /// </summary>
        CreationDate,

        /// <summary>
        ///     Display name.
        /// </summary>
        DisplayName,

        /// <summary>
        ///     Content language.
        /// </summary>
        GetContentLanguage,

        /// <summary>
        ///     Content length.
        /// </summary>
        GetContentLength,

        /// <summary>
        ///     Content type.
        /// </summary>
        GetContentType,

        /// <summary>
        ///     ETag.
        /// </summary>
        GetEtag,

        /// <summary>
        ///     Last modification.
        /// </summary>
        GetLastModified,

        /// <summary>
        ///     Lock discovery.
        /// </summary>
        LockDiscovery,

        /// <summary>
        ///     Resource type.
        /// </summary>
        ResourceType,

        /// <summary>
        ///     Source.
        /// </summary>
        Source,

        /// <summary>
        ///     Supported lock.
        /// </summary>
        Supportedlock
    }
}