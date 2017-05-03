using NextcloudClient.WebDav.Constants;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace NextcloudClient.WebDav.WebDavArtifacts
{
    [DataContract]
    [DebuggerStepThrough]
    [XmlType(TypeName = NextcloudPropNameConstants.ShareTypes, Namespace = WebDavNextcloudConstants.OwncloudNamespace)]
    [XmlRoot(Namespace = WebDavNextcloudConstants.OwncloudNamespace, IsNullable = false)]
    public class NextcloudShareTypes
    {
        [XmlElement(ElementName = NextcloudPropNameConstants.ShareType)]
        public string ShareType
        {
            get;
            set;
        }
    }
}
