using System.Net;

namespace WebDavClient
{
    /// <summary>
    ///     WebDavCredential class is an extension of the NetworkCredential class to support Web authentication.
    /// </summary>
    public class WebDavCredential : NetworkCredential
    {
        #region PUBLIC PROPERTIES

        /// <summary>
        ///     Gets or sets the type of the authentication.
        /// </summary>
        /// <value>The type of the authentication.</value>
        public AuthType AuthenticationType { get; set; }

        #endregion

        #region CONSTRUCTORS

        /// <summary>
        ///     Initializes a new instance of the <see cref="WebDavCredential" /> class.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="password">The password.</param>
        public WebDavCredential(string user, string password)
            : this(user, password, string.Empty)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="WebDavCredential" /> class.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="password">The password.</param>
        /// <param name="authType">Type of the authentication.</param>
        public WebDavCredential(string user, string password, AuthType authType)
            : this(user, password, string.Empty, authType)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="WebDavCredential" /> class.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="password">The password.</param>
        /// <param name="domain">The domain.</param>
        public WebDavCredential(string user, string password, string domain)
            : this(user, password, domain, AuthType.Basic)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="WebDavCredential" /> class.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="password">The password.</param>
        /// <param name="domain">The domain.</param>
        /// <param name="authType">Type of the authentication.</param>
        public WebDavCredential(string user, string password, string domain, AuthType authType)
            : base(user, password, domain)
        {
            AuthenticationType = authType;
        }

        #endregion
    }
}