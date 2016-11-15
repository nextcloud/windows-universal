using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.StartScreen;

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

        public async void CreateTile(string id, string dispalyName, string arguments, Uri logo150, TileSize tileSize = TileSize.Default)
        {
            var tile = new SecondaryTile(id, dispalyName, arguments, logo150, tileSize);
            await tile.RequestCreateAsync();
        }

        public async Task<IAsyncOperation<bool>> DeleteTile(string id)
        {
            var tile = (await GetAllPinnedTiles()).FirstOrDefault(t => t.TileId == id);
            if (tile != null)
            {
                return tile.RequestDeleteAsync();
            }
            return Task.FromResult(false).AsAsyncOperation();
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
