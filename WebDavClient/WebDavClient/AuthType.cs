namespace WebDavClient
{
    /// <summary>
    /// All supported web authentication types.
    /// </summary>
	public enum AuthType
	{
        /// <summary>
        /// Basic authentication
        /// </summary>
		Basic,
        /// <summary>
        /// NTLM authentication.
        /// </summary>
		Ntlm,
        /// <summary>
        /// Digest authentication.
        /// </summary>
		Digest,
        /// <summary>
        /// Kerberos authentication.
        /// </summary>
		Kerberos,
        /// <summary>
        /// Negotiate the authentication between Client and Server.
        /// </summary>
		Negotiate
	}
}
