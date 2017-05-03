using DecaTec.WebDav;
using System;

namespace NextcloudClient.WebDav
{
    public class NextcloudWebDavSessionListItem : WebDavSessionListItem
    {
        public new string Id
        {
            get;
            set;
        }

        public string FileId
        {
            get;
            set;
        }

        public bool? IsFavorite
        {
            get;
            set;
        }

        public Uri CommentsHref
        {
            get;
            set;
        }

        public long? CommentsCount
        {
            get;
            set;
        }

        public long? CommentsUnread
        {
            get;
            set;
        }

        public string OwnerId
        {
            get;
            set;
        }

        public string OwnerDisplayName
        {
            get;
            set;
        }

        public string ShareTypes
        {
            get;
            set;
        }

        public string Checksums
        {
            get;
            set;
        }

        public bool? HasPreview
        {
            get;
            set;
        }

        public long? Size
        {
            get;
            set;
        }
    }
}
