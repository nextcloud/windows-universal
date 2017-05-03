using DecaTec.WebDav;
using DecaTec.WebDav.WebDavArtifacts;
using NextcloudClient.WebDav.Constants;
using System;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace NextcloudClient.WebDav.WebDavArtifacts
{
    [DataContract]
    [XmlType(TypeName = WebDavConstants.Prop, Namespace = WebDavConstants.DAV)]
    [XmlRoot(Namespace = WebDavConstants.DAV, IsNullable = false)]
    public class NextcloudProp
    {        
        public static NextcloudProp CreatePropWithEmptyProperties(params string[] emptyPropertyNames)
        {
            NextcloudProp prop = new NextcloudProp();

            foreach (var emptyPropertyName in emptyPropertyNames)
            {
                switch (emptyPropertyName.ToLower())
                {                   
                    case PropNameConstants.GetContentLength:
                        prop.GetContentLengthString = string.Empty;
                        break;
                    case PropNameConstants.GetContentType:
                        prop.GetContentType = string.Empty;
                        break;
                    case PropNameConstants.GetLastModified:
                        prop.GetLastModifiedString = string.Empty;
                        break;
                    case PropNameConstants.GetEtag:
                        prop.GetEtag = string.Empty;
                        break;
                    case PropNameConstants.ResourceType:
                        prop.ResourceType = new ResourceType();
                        break;                    
                    case PropNameConstants.QuotaAvailableBytes:
                        prop.QuotaAvailableBytesString = string.Empty;
                        break;
                    case PropNameConstants.QuotaUsedBytes:
                        prop.QuotaUsedBytesString = string.Empty;
                        break;                   
                    case NextcloudPropNameConstants.Id:
                        prop.Id = string.Empty;
                        break;
                    case NextcloudPropNameConstants.FileId:
                        prop.FileId = string.Empty;
                        break;
                    case NextcloudPropNameConstants.Favorite:
                        prop.FavoriteString = string.Empty;
                        break;
                    case NextcloudPropNameConstants.CommentsHref:
                        prop.CommentsHrefString = string.Empty;
                        break;
                    case NextcloudPropNameConstants.CommentsCount:
                        prop.CommentsCountString = string.Empty;
                        break;
                    case NextcloudPropNameConstants.CommentsUnread:
                        prop.CommentsUnreadString = string.Empty;
                        break;
                    case NextcloudPropNameConstants.OwnerId:
                        prop.OwnerId = string.Empty;
                        break;
                    case NextcloudPropNameConstants.OwnerDisplayName:
                        prop.OwnerDisplayName = string.Empty;
                        break;
                    case NextcloudPropNameConstants.ShareTypes:
                        prop.ShareTypes= string.Empty;
                        break;
                    case NextcloudPropNameConstants.Checksums:
                        prop.Checksums = string.Empty;
                        break;
                    case NextcloudPropNameConstants.HasPreview:
                        prop.HasPreviewString = string.Empty;
                        break;
                    case NextcloudPropNameConstants.Size:
                        prop.SizeString = string.Empty;
                        break;
                    default:
                        break;
                }
            }

            return prop;
        }

        /// <summary>
        /// Creates a Prop with all empty properties which are defined in RFC4918. This is especially useful for PROPFIND commands when the so called 'allprop' cannot be used because the WebDAV server does not return all properties.
        /// </summary>
        /// <returns>A Prop with all empty properties defined in RFC4918.</returns>
        public static NextcloudProp CreatePropWithEmptyPropertiesAll()
        {
            NextcloudProp prop = new NextcloudProp()
            {
                GetContentLengthString = string.Empty,
                GetContentType = string.Empty,
                GetLastModifiedString = string.Empty,
                GetEtag = string.Empty,
                ResourceType = new ResourceType(),
                QuotaAvailableBytesString = string.Empty,
                QuotaUsedBytesString = string.Empty,
                Id = string.Empty,
                FileId = string.Empty,
                FavoriteString = string.Empty,
                CommentsHrefString = string.Empty,
                CommentsCountString = string.Empty,
                CommentsUnreadString = string.Empty,
                OwnerId = string.Empty,
                OwnerDisplayName = string.Empty,
                ShareTypes = string.Empty,
                Checksums = string.Empty,
                HasPreviewString = string.Empty,
                SizeString = string.Empty
            };

            return prop;
        }

        /// <summary>
        /// Gets or sets the GetContentLength as string.
        /// </summary>
        [XmlElement(ElementName = PropNameConstants.GetContentLength)]
        public string GetContentLengthString
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the GetContentLength or null when there is no GetContentLength available.
        /// </summary>
        [XmlIgnore]
        public long? GetContentLength
        {
            get
            {
                if (!string.IsNullOrEmpty(this.GetContentLengthString))
                    return long.Parse(this.GetContentLengthString);
                else
                    return null;
            }
        }

        /// <summary>
        /// Gets or sets the GetContentType.
        /// </summary>
        [XmlElement(ElementName = PropNameConstants.GetContentType)]
        public string GetContentType
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the GetEtag.
        /// </summary>
        [XmlElement(ElementName = PropNameConstants.GetEtag)]
        public string GetEtag
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the GetLastModified as string.
        /// </summary>
        [XmlElement(ElementName = PropNameConstants.GetLastModified)]
        public string GetLastModifiedString
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the GetLastModified or null if there is no GetLastModified available.
        /// </summary>
        [XmlIgnore]
        public DateTime? GetLastModified
        {
            get
            {
                if (!string.IsNullOrEmpty(this.GetLastModifiedString))
                    return DateTime.Parse(this.GetLastModifiedString);
                else
                    return null;
            }
        }       

        /// <summary>
        /// Gets or sets the <see cref="DecaTec.WebDav.WebDavArtifacts.ResourceType"/>.
        /// </summary>
        [XmlElement(ElementName = PropNameConstants.ResourceType)]
        public ResourceType ResourceType
        {
            get;
            set;
        }

        #region RFC4331

        // Properties as defined in https://tools.ietf.org/html/rfc4331

        /// <summary>
        /// Gets or sets the QuotaAvailableBytes as string.
        /// </summary>
        [XmlElement(ElementName = PropNameConstants.QuotaAvailableBytes)]
        public string QuotaAvailableBytesString
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the QuotaAvailableBytes or null when there is no QuotaAvailableBytes available.
        /// </summary>
        [XmlIgnore]
        public long? QuotaAvailableBytes
        {
            get
            {
                if (!string.IsNullOrEmpty(this.QuotaAvailableBytesString))
                    return long.Parse(this.QuotaAvailableBytesString);
                else
                    return null;
            }
        }

        /// <summary>
        /// Gets or sets the QuotaUsedBytes as string.
        /// </summary>
        [XmlElement(ElementName = PropNameConstants.QuotaUsedBytes)]
        public string QuotaUsedBytesString
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the QuotaUsedBytes or null if there is no QuotaUsedBytes available.
        /// </summary>
        [XmlIgnore]
        public long? QuotaUsedBytes
        {
            get
            {
                if (!string.IsNullOrEmpty(this.QuotaUsedBytesString))
                    return long.Parse(this.QuotaUsedBytesString);
                else
                    return null;
            }
        }

        #endregion RFC4331

        #region Nextcloud specific

        [XmlElement(ElementName = NextcloudPropNameConstants.Id, Namespace =WebDavNextcloudConstants.OwncloudNamespace)]
        public string Id
        {
            get;
            set;
        }

        [XmlElement(ElementName = NextcloudPropNameConstants.FileId, Namespace = WebDavNextcloudConstants.OwncloudNamespace)]
        public string FileId
        {
            get;
            set;
        }

        [XmlElement(ElementName = NextcloudPropNameConstants.Favorite, Namespace = WebDavNextcloudConstants.OwncloudNamespace)]
        public string FavoriteString
        {
            get;
            set;
        }

        [XmlIgnore]
        public bool? IsFavorite
        {
            get
            {
                if (!string.IsNullOrEmpty(this.FavoriteString))
                {
                    if (this.FavoriteString.Equals("1"))
                        return true;
                    else
                        return false;
                }
                else
                    return null;
            }
        }

        /// <summary>
        /// Gets or sets the IsFolder as string.
        /// </summary>
        [XmlElement(ElementName = PropNameConstants.IsFolder)]
        public string IsFolderString
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the IsFolder or null if there is no IsFolder available.
        /// </summary>
        [XmlIgnore]
        public bool? IsFolder
        {
            get
            {
                if (!string.IsNullOrEmpty(this.IsFolderString))
                {
                    if (this.IsFolderString.Equals("1"))
                        return true;
                    else
                        return false;
                }
                else
                    return null;
            }
        }

        [XmlElement(ElementName = NextcloudPropNameConstants.CommentsHref, Namespace = WebDavNextcloudConstants.OwncloudNamespace)]
        public string CommentsHrefString
        {
            get;
            set;
        }

        [XmlIgnore]
        public Uri CommentsHref
        {
            get
            {
                if (!string.IsNullOrEmpty(this.CommentsHrefString))
                    return UriHelper.CreateUriFromUrl(this.CommentsHrefString);
                else
                    return null;
            }
        }

        [XmlElement(ElementName = NextcloudPropNameConstants.CommentsCount, Namespace = WebDavNextcloudConstants.OwncloudNamespace)]
        public string CommentsCountString
        {
            get;
            set;
        }

        [XmlIgnore]
        public long? CommentsCount
        {
            get
            {
                if (!string.IsNullOrEmpty(this.CommentsCountString))
                    return long.Parse(this.CommentsCountString);
                else
                    return null;
            }
        }

        [XmlElement(ElementName = NextcloudPropNameConstants.CommentsUnread, Namespace = WebDavNextcloudConstants.OwncloudNamespace)]
        public string CommentsUnreadString
        {
            get;
            set;
        }

        [XmlIgnore]
        public long? CommentsUnread
        {
            get
            {
                if (!string.IsNullOrEmpty(this.CommentsUnreadString))
                    return long.Parse(this.CommentsUnreadString);
                else
                    return null;
            }
        }

        [XmlElement(ElementName = NextcloudPropNameConstants.OwnerId, Namespace = WebDavNextcloudConstants.OwncloudNamespace)]
        public string OwnerId
        {
            get;
            set;
        }

        [XmlElement(ElementName = NextcloudPropNameConstants.OwnerDisplayName, Namespace = WebDavNextcloudConstants.OwncloudNamespace)]
        public string OwnerDisplayName
        {
            get;
            set;
        }

        [XmlElement(ElementName = NextcloudPropNameConstants.ShareTypes, Namespace = WebDavNextcloudConstants.OwncloudNamespace)]
        public string ShareTypes
        {
            get;
            set;
        }

        [XmlElement(ElementName = NextcloudPropNameConstants.Checksums, Namespace = WebDavNextcloudConstants.OwncloudNamespace)]
        public string Checksums
        {
            get;
            set;
        }

        [XmlElement(ElementName = NextcloudPropNameConstants.HasPreview, Namespace = WebDavNextcloudConstants.NextcloudNamespace)]
        public string HasPreviewString
        {
            get;
            set;
        }

        [XmlIgnore]
        public bool? HasPreview
        {
            get
            {
                if (!string.IsNullOrEmpty(this.HasPreviewString))
                {
                    if (this.HasPreviewString.Equals("true"))
                        return true;
                    else
                        return false;
                }
                else
                    return null;
            }
        }

        [XmlElement(ElementName = NextcloudPropNameConstants.Size, Namespace = WebDavNextcloudConstants.OwncloudNamespace)]
        public string SizeString
        {
            get;
            set;
        }

        [XmlIgnore]
        public long? Size
        {
            get
            {
                if (!string.IsNullOrEmpty(this.SizeString))
                    return long.Parse(this.SizeString);
                else
                    return null;
            }
        }

        #endregion Nextcloud specific
    }
}
