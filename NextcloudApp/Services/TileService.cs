using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.StartScreen;
using NextcloudApp.Models;
using NextcloudApp.Utils;
using NextcloudClient.Types;

namespace NextcloudApp.Services
{
    public class TileService
    {
        private static TileService _instance;

        public TileService()
        {

        }

        public static TileService Instance => _instance ?? (_instance = new TileService());

        public bool IsTilePinned(string id)
        {
            return SecondaryTile.Exists(id);
        }

        public async void CreatePinnedObject(ResourceInfo resourceInfo)
        {
            var id = resourceInfo.Path.ToBase64();
            if (!IsTilePinned(id))
            {
                var arguments = resourceInfo.Serialize();
                var displayName = resourceInfo.Name;

                var tile = new SecondaryTile(id, displayName, arguments, new Uri("ms-appx:///Assets/Square150x150Logo.png"), TileSize.Default);
                tile.VisualElements.ShowNameOnSquare150x150Logo = true;

                await tile.RequestCreateAsync();
            }
        }

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
