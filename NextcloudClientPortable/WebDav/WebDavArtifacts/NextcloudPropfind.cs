using DecaTec.WebDav;
using DecaTec.WebDav.WebDavArtifacts;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace NextcloudClient.WebDav.WebDavArtifacts
{
    [DataContract]
    [XmlType(TypeName = WebDavConstants.PropFind, Namespace = WebDavConstants.DAV)]
    [XmlRoot(Namespace = WebDavConstants.DAV, IsNullable = false)]
    public class NextcloudPropFind
    {
        /// <summary>
        /// Creates a PropFind instance representing an 'allprop'-Propfind.
        /// </summary>
        /// <returns>A PropFind instance containing an <see cref="DecaTec.WebDav.WebDavArtifacts.AllProp"/> element.</returns>
        public static NextcloudPropFind CreatePropFindAllProp()
        {
            var propFind = new NextcloudPropFind()
            {
                Item = new AllProp()
            };

            return propFind;
        }

        /// <summary>
        /// Creates an empty PropFind instance. The server should return all known properties (for the server) by that empty PropFind.
        /// </summary>
        /// <returns>An empty PropFind instance.</returns>
        public static NextcloudPropFind CreatePropFind()
        {
            return new NextcloudPropFind();
        }

        /// <summary>
        /// Creates a PropFind instance containing empty property items with the specified names. Useful for obtaining only a few properties from the server.
        /// </summary>
        /// <param name="propertyNames">The property names which should be contained in the PropFind instance.</param>
        /// <returns>A PropFind instance containing the empty <see cref="DecaTec.WebDav.WebDavArtifacts.Prop"/> items specified.</returns>
        public static NextcloudPropFind CreatePropFindWithEmptyProperties(params string[] propertyNames)
        {
            var propFind = new NextcloudPropFind();
            var prop = NextcloudProp.CreatePropWithEmptyProperties(propertyNames);
            propFind.Item = prop;
            return propFind;
        }

        /// <summary>
        /// Creates a PropFind instance containing empty property items for all the Props defined in RFC4918/RFC4331.
        /// </summary>
        /// <returns>A PropFind instance containing the empty <see cref="DecaTec.WebDav.WebDavArtifacts.Prop"/> items of all Props defined in RFC4918/RFC4331.</returns>
        public static NextcloudPropFind CreatePropFindWithEmptyPropertiesAll()
        {
            var propFind = new NextcloudPropFind();
            var prop = NextcloudProp.CreatePropWithEmptyPropertiesAll();
            propFind.Item = prop;
            return propFind;
        }

        /// <summary>
        /// Creates a PropFind instance containing a PropertyName item.
        /// </summary>
        /// <returns>A PropFind instance containing a <see cref="DecaTec.WebDav.WebDavArtifacts.PropName"/> item.</returns>
        public static NextcloudPropFind CreatePropFindWithPropName()
        {
            var propFind = new NextcloudPropFind()
            {
                Item = new PropName()
            };

            return propFind;
        }

        /// <summary>
        /// Gets or sets the Item.
        /// </summary>
        [XmlElement(ElementName = WebDavConstants.AllProp, Type = typeof(AllProp))]
        [XmlElement(ElementName = WebDavConstants.Prop, Type = typeof(NextcloudProp))]
        [XmlElement(ElementName = WebDavConstants.PropName, Type = typeof(PropName))]
        public object Item
        {
            get;
            set;
        }
    }
}
