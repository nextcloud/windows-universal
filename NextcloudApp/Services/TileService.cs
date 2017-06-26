using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.StartScreen;
using NextcloudApp.Utils;
using NextcloudClient.Types;

namespace NextcloudApp.Services
{
    public class TileService
    {
        /// <summary>
        /// The instance
        /// </summary>
        private static TileService _instance;

        /// <summary>
        /// Gets the instance.
        /// </summary>
        /// <value>
        /// The instance.
        /// </value>
        public static TileService Instance => _instance ?? (_instance = new TileService());

        /// <summary>
        /// Determines whether [is tile pinned] [the specified identifier].
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns>
        ///   <c>true</c> if [is tile pinned] [the specified identifier]; otherwise, <c>false</c>.
        /// </returns>
        public bool IsTilePinned(string id)
        {
            return SecondaryTile.Exists(id);
        }

        /// <summary>
        /// Determines whether [is tile pinned] [the specified resource information].
        /// </summary>
        /// <param name="resourceInfo">The resource information.</param>
        /// <returns>
        ///   <c>true</c> if [is tile pinned] [the specified resource information]; otherwise, <c>false</c>.
        /// </returns>
        public bool IsTilePinned(ResourceInfo resourceInfo)
        {
            var id = resourceInfo.Path.ToBase64();
            return IsTilePinned(id);
        }

        /// <summary>
        /// Creates the pinned object.
        /// </summary>
        /// <param name="resourceInfo">The resource information.</param>
        public async void CreatePinnedObject(ResourceInfo resourceInfo)
        {
            var id = (resourceInfo.Path + "/" + resourceInfo.Name).ToBase64();
            if (!IsTilePinned(id))
            {
                var arguments = resourceInfo.Serialize();
                var displayName = resourceInfo.Name;

                var tile = new SecondaryTile(id, displayName, arguments, new Uri("ms-appx:///Assets/Square150x150Logo.png"), TileSize.Default);
                tile.VisualElements.ShowNameOnSquare150x150Logo = true;

                await tile.RequestCreateAsync();
            }
        }

        /// <summary>
        /// Removes the pinned object.
        /// </summary>
        /// <param name="resourceInfo">The resource information.</param>
        /// <returns></returns>
        public async Task RemovePinnedObject(ResourceInfo resourceInfo)
        {
            var id = resourceInfo.Path.ToBase64();
            if (IsTilePinned(id))
            {
                var tile = (await GetAllPinnedTiles()).FirstOrDefault(t => t.TileId == id);
                if (tile != null)
                {
                    await tile.RequestDeleteAsync();
                }
            }
        }

        /// <summary>
        /// Gets all pinned tiles.
        /// </summary>
        /// <returns></returns>
        public async Task<SecondaryTile[]> GetAllPinnedTiles()
        {
            var tiles = await SecondaryTile.FindAllAsync();
            if (tiles.Any())
            {
                return tiles.ToArray();
            }
            return new SecondaryTile[0];
        }
    }
}
