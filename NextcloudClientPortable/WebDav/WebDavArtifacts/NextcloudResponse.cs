using DecaTec.WebDav;
using DecaTec.WebDav.WebDavArtifacts;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace NextcloudClient.WebDav.WebDavArtifacts
{
    /// <summary>
    /// Class representing an 'response' XML element for WebDAV communication.
    /// </summary>
    [DataContract]
    [DebuggerStepThrough]
    [XmlType(TypeName = WebDavConstants.Response, Namespace = WebDavConstants.DAV)]
    [XmlRoot(Namespace = WebDavConstants.DAV, IsNullable = false)]
    public class NextcloudResponse
    {
        [XmlElement(ElementName = WebDavConstants.Href, Order = 0)]
        public string Href
        {
            get;
            set;
        }

        [XmlElement(ElementName = WebDavConstants.Href, Type = typeof(string), Order = 1)]
        [XmlElement(ElementName = WebDavConstants.PropStat, Type = typeof(NextcloudPropstat), Order = 1)]
        [XmlElement(ElementName = WebDavConstants.Status, Type = typeof(string), Order = 1)]
        [XmlChoiceIdentifier(WebDavConstants.ItemsElementName)]
        public object[] Items
        {
            get;
            set;
        }

        [XmlElement(ElementName = WebDavConstants.ItemsElementName, Order = 2)]
        [XmlIgnore()]
        public ItemsChoiceType[] ItemsElementName
        {
            get;
            set;
        }

        [XmlElement(ElementName = WebDavConstants.ResponseDescription, Order = 3)]
        public string ResponseDescription
        {
            get;
            set;
        }
    }
}
