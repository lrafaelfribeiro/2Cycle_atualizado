using System;
using System.Collections.Generic;
using System.Text;

namespace APP.Services.Tiles
{
    public interface ITileCacheService
    {
        Task<byte[]?> GetTileAsync(int x, int y, int zoom);
    }
}
