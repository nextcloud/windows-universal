using DecaTec.WebDav;
using DecaTec.WebDav.Tools;
using NextcloudClient.Types;
using NextcloudClient.WebDav;
using System;

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
                Name = System.Net.WebUtility.UrlDecode(item.Name),
                QuotaAvailable = item.QuotaAvailableBytes ?? 0,
                QuotaUsed = item.QuotaUsedBytes ?? 0,
                Size = item.ContentLength.HasValue && item.ContentLength.Value != 0 ? item.ContentLength.Value : item.QuotaUsedBytes ?? 0,
                Path = System.Net.WebUtility.UrlDecode(item.Uri.AbsoluteUri.Replace(baseUri.AbsoluteUri, ""))
            };

            // NC specific properties.
            var ncProps = item.AdditionalProperties;

            if(ncProps != null)
            {
                var key = NsOc + ":" + NextcloudPropNameConstants.Checksums;

                if (ncProps.ContainsKey(key))
                    res.Checksums = ncProps[NsOc + ":" + NextcloudPropNameConstants.Checksums];

                key = NsOc + ":" + NextcloudPropNameConstants.CommentsCount;

                if (ncProps.ContainsKey(key))
                {
                    var commentsCount = ncProps[NsOc + ":" + NextcloudPropNameConstants.CommentsCount];
                    res.CommentsCount = string.IsNullOrEmpty(commentsCount) ? 0 : long.Parse(commentsCount);
                }

                key = NsOc + ":" + NextcloudPropNameConstants.CommentsHref;

                if (ncProps.ContainsKey(key))
                {
                    var commentsHref = ncProps[NsOc + ":" + NextcloudPropNameConstants.CommentsHref];
                    res.CommentsHref = string.IsNullOrEmpty(commentsHref) ? null : UriHelper.CombineUri(baseUri, new Uri(commentsHref, UriKind.Relative));
                }

                key = NsOc + ":" + NextcloudPropNameConstants.CommentsUnread;

                if (ncProps.ContainsKey(key))
                {
                    var commentsUnread = ncProps[NsOc + ":" + NextcloudPropNameConstants.CommentsUnread];
                    res.CommentsUnread = string.IsNullOrEmpty(commentsUnread) ? 0 : long.Parse(commentsUnread);
                }

                key = NsOc + ":" + NextcloudPropNameConstants.Favorite;

                if (ncProps.ContainsKey(key))
                {
                    var favorite = ncProps[NsOc + ":" + NextcloudPropNameConstants.Favorite];
                    res.IsFavorite = string.IsNullOrEmpty(favorite) ? false : string.CompareOrdinal(favorite, "1") == 0 ? true : false;
                }

                key = NsOc + ":" + NextcloudPropNameConstants.FileId;

                if (ncProps.ContainsKey(key))
                {
                    res.FileId = ncProps[NsOc + ":" + NextcloudPropNameConstants.FileId];
                }

                key = NsOc + ":" + NextcloudPropNameConstants.HasPreview;

                if (ncProps.ContainsKey(key))
                {
                    var hasPreview = ncProps[NsOc + ":" + NextcloudPropNameConstants.HasPreview];
                    res.HasPreview = string.IsNullOrEmpty(hasPreview) ? false : string.CompareOrdinal(hasPreview, "1") == 0 ? true : false;
                }

                key = NsOc + ":" + NextcloudPropNameConstants.Id;

                if (ncProps.ContainsKey(key))
                {
                    res.Id = ncProps[NsOc + ":" + NextcloudPropNameConstants.Id];
                }

                key = NsOc + ":" + NextcloudPropNameConstants.OwnerDisplayName;

                if (ncProps.ContainsKey(key))
                {
                    res.OwnerDisplayName = ncProps[NsOc + ":" + NextcloudPropNameConstants.OwnerDisplayName];
                }

                key = NsOc + ":" + NextcloudPropNameConstants.OwnerId;

                if (ncProps.ContainsKey(key))
                {
                    res.OwnderId = ncProps[NsOc + ":" + NextcloudPropNameConstants.OwnerId];
                }

                key = NsOc + ":" + NextcloudPropNameConstants.ShareTypes;

                if (ncProps.ContainsKey(key))
                {
                    res.ShareTypes = ncProps[NsOc + ":" + NextcloudPropNameConstants.ShareTypes];
                }

                key = NsOc + ":" + NextcloudPropNameConstants.Size;

                if (ncProps.ContainsKey(key))
                {
                    var size = ncProps[NsOc + ":" + NextcloudPropNameConstants.Size];
                    res.Size = string.IsNullOrEmpty(size) ? 0 : long.Parse(size);
                }
            }

            return res;
        }
    }
}
