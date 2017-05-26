using DecaTec.WebDav;
using DecaTec.WebDav.Tools;
using NextcloudClient.Types;
using NextcloudClient.WebDav;
using System;
using System.Xml.Linq;

namespace NextcloudClient.Extensions
{
    /// <summary>
    /// Creates a <see cref="ResourceInfo"/> from a <see cref="WebDavSessionItem"/>.
    /// </summary>
    public static class WebDavSessionItemExtensions
    {
        public const string NsOc = "http://owncloud.org/ns";

        public static ResourceInfo ToResourceInfo(this WebDavSessionItem item, Uri baseUri)
        {
            var res = new ResourceInfo
            {
                ContentType = item.IsFolder.HasValue ? "dav/directory" : item.ContentType,
                Created = item.CreationDate ?? DateTime.MinValue,
                ETag = item.ETag,
                LastModified = item.LastModified ?? DateTime.MinValue,
                Name = item.Name,
                QuotaAvailable = item.QuotaAvailableBytes ?? 0,
                QuotaUsed = item.QuotaUsedBytes ?? 0,
                Size = item.ContentLength.HasValue && item.ContentLength.Value != 0 ? item.ContentLength.Value : item.QuotaUsedBytes ?? 0,
                Path = Uri.UnescapeDataString(item.Uri.AbsoluteUri.Replace(baseUri.AbsoluteUri, ""))
            };

            // NC specific properties.
            var ncProps = item.AdditionalProperties;

            if(ncProps != null)
            {
                var key = XName.Get(NextcloudPropNameConstants.Checksums, NsOc);

                if (ncProps.ContainsKey(key))
                    res.Checksums = ncProps[key];

                key = XName.Get(NextcloudPropNameConstants.CommentsCount, NsOc);

                if (ncProps.ContainsKey(key))
                {
                    var commentsCount = ncProps[key];
                    res.CommentsCount = string.IsNullOrEmpty(commentsCount) ? 0 : long.Parse(commentsCount);
                }

                key = XName.Get(NextcloudPropNameConstants.CommentsHref, NsOc);

                if (ncProps.ContainsKey(key))
                {
                    var commentsHref = ncProps[key];
                    res.CommentsHref = string.IsNullOrEmpty(commentsHref) ? null : UriHelper.CombineUri(baseUri, new Uri(commentsHref, UriKind.Relative));
                }

                key = XName.Get(NextcloudPropNameConstants.CommentsUnread, NsOc);

                if (ncProps.ContainsKey(key))
                {
                    var commentsUnread = ncProps[key];
                    res.CommentsUnread = string.IsNullOrEmpty(commentsUnread) ? 0 : long.Parse(commentsUnread);
                }

                key = XName.Get(NextcloudPropNameConstants.Favorite, NsOc);

                if (ncProps.ContainsKey(key))
                {
                    var favorite = ncProps[key];
                    res.IsFavorite = string.IsNullOrEmpty(favorite) ? false : string.CompareOrdinal(favorite, "1") == 0 ? true : false;
                }

                key = XName.Get(NextcloudPropNameConstants.FileId, NsOc);

                if (ncProps.ContainsKey(key))
                {
                    res.FileId = ncProps[key];
                }

                key = XName.Get(NextcloudPropNameConstants.HasPreview, NsOc);

                if (ncProps.ContainsKey(key))
                {
                    var hasPreview = ncProps[key];
                    res.HasPreview = string.IsNullOrEmpty(hasPreview) ? false : string.CompareOrdinal(hasPreview, "1") == 0 ? true : false;
                }

                key = XName.Get(NextcloudPropNameConstants.Id, NsOc);

                if (ncProps.ContainsKey(key))
                {
                    res.Id = ncProps[key];
                }

                key = XName.Get(NextcloudPropNameConstants.OwnerDisplayName, NsOc);

                if (ncProps.ContainsKey(key))
                {
                    res.OwnerDisplayName = ncProps[key];
                }

                key = XName.Get(NextcloudPropNameConstants.OwnerId, NsOc);

                if (ncProps.ContainsKey(key))
                {
                    res.OwnderId = ncProps[key];
                }

                key = XName.Get(NextcloudPropNameConstants.ShareTypes, NsOc);

                if (ncProps.ContainsKey(key))
                {
                    var shareType = ncProps[key];

                    if(!string.IsNullOrEmpty(shareType))
                        res.ShareTypes = (OcsShareType)int.Parse(ncProps[key]);
                }

                key = XName.Get(NextcloudPropNameConstants.Size, NsOc);

                if (ncProps.ContainsKey(key))
                {
                    var size = ncProps[key];
                    res.Size = string.IsNullOrEmpty(size) ? 0 : long.Parse(size);
                }
            }

            return res;
        }
    }
}
