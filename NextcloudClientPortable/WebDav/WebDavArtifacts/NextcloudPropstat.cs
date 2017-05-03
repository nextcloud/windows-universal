using DecaTec.WebDav;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace NextcloudClient.WebDav.WebDavArtifacts
{
    [DataContract]
    [DebuggerStepThrough]
    [XmlType(TypeName = WebDavConstants.PropStat, Namespace = WebDavConstants.DAV)]
    [XmlRoot(Namespace = WebDavConstants.DAV, IsNullable = false)]
    public class NextcloudPropstat
    {
        [XmlElement(ElementName = WebDavConstants.Prop)]
        public NextcloudProp Prop
        {
            get;
            set;
        }


        [XmlElement(ElementName = WebDavConstants.Status)]
        public string Status
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
