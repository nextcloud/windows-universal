using DecaTec.WebDav;
using DecaTec.WebDav.WebDavArtifacts;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace NextcloudClient.WebDav.WebDavArtifacts
{
    [DataContract]
    [DebuggerStepThrough]
    [XmlType(TypeName = WebDavConstants.MultiStatus, Namespace = WebDavConstants.DAV)]
    [XmlRoot(Namespace = WebDavConstants.DAV, IsNullable = false)]
    public class NextcloudMultistatus
    {
        [XmlElement(ElementName = WebDavConstants.Response)]
        public NextcloudResponse[] Response
        {
            get;
            set;
        }

        [XmlElement(ElementName = WebDavConstants.ResponseDescription)]
        public string ResponseDescription
        {
            get;
            set;
        }
    }
}
