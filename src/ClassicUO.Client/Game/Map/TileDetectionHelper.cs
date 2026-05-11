// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.Managers;
using ClassicUO.Game.GameObjects;
using ClassicUO.Utility;
using System;

namespace ClassicUO.Game.Map
{
    internal enum FootstepTerrainType
    {
        Dust,
        Snow,
        Water,
        Swamp
    }

    internal static class TileDetectionHelper
    {
        public static FootstepTerrainType GetFootstepTerrainType(Map map, int targetTileX, int targetTileY, int stepZ, Season season)
        {
            if (map == null) return FootstepTerrainType.Dust;

            Chunk chunk = map.GetChunk(targetTileX, targetTileY, load: false);

            if (chunk == null) return FootstepTerrainType.Dust;

            GameObject obj = chunk.Tiles[targetTileX % 8, targetTileY % 8];
            GameObject bestSurface = null;
            int bestDistance = int.MaxValue;
            sbyte bestZ = sbyte.MinValue;
            GameObject fallbackSurface = null;
            sbyte fallbackZ = sbyte.MinValue;

            while (obj != null)
            {
                bool isSurface = obj is Land || obj is Static;
                if (!isSurface)
                {
                    obj = obj.TNext;
                    continue;
                }

                bool isGraphicValid = obj is Land
                    ? obj.Graphic < Client.Game.UO.FileManager.TileData.LandData.Length
                    : obj.Graphic < Client.Game.UO.FileManager.TileData.StaticData.Length;

                if (!isGraphicValid)
                {
                    obj = obj.TNext;
                    continue;
                }

                sbyte surfaceZ = obj is Land landObj ? landObj.AverageZ : obj.Z;
                int zDistance = Math.Abs(surfaceZ - stepZ);

                if (zDistance < bestDistance || (zDistance == bestDistance && surfaceZ > bestZ))
                {
                    bestDistance = zDistance;
                    bestZ = surfaceZ;
                    bestSurface = obj;
                }

                if (surfaceZ > fallbackZ)
                {
                    fallbackZ = surfaceZ;
                    fallbackSurface = obj;
                }
                obj = obj.TNext;
            }

            GameObject selectedSurface = bestSurface ?? fallbackSurface;

            if (selectedSurface is null)
            {
                return season == Season.Winter ? FootstepTerrainType.Snow : FootstepTerrainType.Dust;
            }

            bool isWet = false;
            string tileName = string.Empty;

            switch (selectedSurface)
            {
                case Land land:
                    if (land.Graphic >= 0x3D65 && land.Graphic <= 0x3E45)
                    {
                        return FootstepTerrainType.Swamp;
                    }

                    isWet = land.TileData.IsWet;
                    tileName = land.TileData.Name ?? string.Empty;
                    break;
                case Static staticTile:
                    isWet = staticTile.ItemData.IsWet;
                    tileName = staticTile.ItemData.Name ?? string.Empty;
                    break;
            }

            string loweredName = tileName.ToLowerInvariant();

            if (loweredName.Contains("snow") || loweredName.Contains("tundra"))
            {
                return FootstepTerrainType.Snow;
            }

            if (loweredName.Contains("swamp") || loweredName.Contains("marsh") || loweredName.Contains("bog") || loweredName.Contains("mud") || loweredName.Contains("moss"))
            {
                return FootstepTerrainType.Swamp;
            }

            if (isWet && (loweredName.Contains("water") || loweredName.Contains("ocean") || loweredName.Contains("sea") || loweredName.Contains("river")))
            {
                return FootstepTerrainType.Water;
            }

            if (isWet && (loweredName.Contains("swamp") || loweredName.Contains("marsh") || loweredName.Contains("bog") || loweredName.Contains("mud")))
            {
                return FootstepTerrainType.Swamp;
            }

            return season == Season.Winter ? FootstepTerrainType.Snow : FootstepTerrainType.Dust;
        }

        public static string GetFootstepSurfaceName(Map map, int targetTileX, int targetTileY, int stepZ)
        {
            if (map == null) return string.Empty;
            Chunk chunk = map.GetChunk(targetTileX, targetTileY, load: false);
            if (chunk == null) return string.Empty;

            GameObject obj = chunk.Tiles[targetTileX % 8, targetTileY % 8];
            GameObject bestSurface = null;
            int bestDistance = int.MaxValue;
            sbyte bestZ = sbyte.MinValue;

            while (obj != null)
            {
                if (!(obj is Land) && !(obj is Static))
                {
                    obj = obj.TNext;
                    continue;
                }

                bool isGraphicValid = obj is Land
                    ? obj.Graphic < Client.Game.UO.FileManager.TileData.LandData.Length
                    : obj.Graphic < Client.Game.UO.FileManager.TileData.StaticData.Length;

                if (!isGraphicValid)
                {
                    obj = obj.TNext;
                    continue;
                }

                sbyte surfaceZ = obj is Land landObj ? landObj.AverageZ : obj.Z;
                int zDistance = Math.Abs(surfaceZ - stepZ);
                if (zDistance < bestDistance || (zDistance == bestDistance && surfaceZ > bestZ))
                {
                    bestDistance = zDistance;
                    bestZ = surfaceZ;
                    bestSurface = obj;
                }

                obj = obj.TNext;
            }

            return bestSurface switch
            {
                Land land => (land.TileData.Name ?? string.Empty).ToLowerInvariant(),
                Static staticTile => (staticTile.ItemData.Name ?? string.Empty).ToLowerInvariant(),
                _ => string.Empty
            };
        }

        /// <summary>
        /// Checks if the given tile position has a covering tile above the specified Z level.
        /// A covering tile is a roof or other structure that blocks weather effects and it's not currently rendering
        /// (e.g., hidden roof when player is inside a house or other non-rendering tile above player).
        /// </summary>
        /// <param name="map">The map instance</param>
        /// <param name="targetTileX">Tile X coordinate</param>
        /// <param name="targetTileY">Tile Y coordinate</param>
        /// <param name="playerZ">Player Z coordinate</param>
        /// <returns>True if the position has a non-rendering covering tile above the player, false otherwise.</returns>
        public static bool HasNonRenderingCoveringTile(Map map, int targetTileX, int targetTileY, int playerZ)
        {
            if (map == null) return false;

            Chunk chunk = map.GetChunk(targetTileX, targetTileY, load: false);

            if (chunk == null) return false;

            int pz14 = playerZ + 14; // Threshold for detecting tiles above player

            GameObject obj = chunk.GetHeadObject(targetTileX % 8, targetTileY % 8);

            while (obj != null)
            {
                if (obj.Graphic >= Client.Game.UO.FileManager.TileData.StaticData.Length)
                {
                    obj = obj.TNext;
                    continue;
                }

                // Check if tile is above the player and it's not rendering
                if ((sbyte)obj.PriorityZ > pz14 && obj.AlphaHue == 0)
                {
                    return true;
                }

                obj = obj.TNext;
            }

            return false;
        }

        /// <summary>
        /// Checks if the given tile position is a water tile.
        /// </summary>
        /// <param name="map">The map instance</param>
        /// <param name="targetTileX">Tile X coordinate</param>
        /// <param name="targetTileY">Tile Y coordinate</param>
        /// <returns>True if the position is on a water tile, false otherwise.</returns>
        /// <remarks>
        /// Thanks to [markdwags](https://github.com/markdwags) for the code 
        /// in [this comment](https://github.com/ClassicUO/ClassicUO/pull/1852#issuecomment-3656749076).
        /// </remarks>
        public static bool IsWaterTile(Map map, int targetTileX, int targetTileY)
        {
            if (map == null) return false;

            Chunk chunk = map.GetChunk(targetTileX, targetTileY, load: false);

            if (chunk == null) return false;

            // Get the first object in the tile's linked list
            GameObject obj = chunk.Tiles[targetTileX % 8, targetTileY % 8];
            // Find the highest Z-level object (the one that's actually visible)
            GameObject topMostObject = null;
            sbyte highestZ = sbyte.MinValue;

            while (obj != null)
            {
                if ((sbyte)obj.PriorityZ > highestZ &&
                    obj.Graphic < Client.Game.UO.FileManager.TileData.StaticData.Length &&
                    obj.AlphaHue != 0)
                {
                    highestZ = (sbyte)obj.PriorityZ;
                    topMostObject = obj;
                }
                obj = obj.TNext;
            }

            // Now check only the top-most visible object
            if (topMostObject != null)
            {
                switch (topMostObject)
                {
                    case Land land:
                        return land.TileData.IsWet &&
                            (land.TileData.Name?.ToLower().Contains("water") == true);
                    case Static staticTile:
                        return staticTile.ItemData.IsWet &&
                            (staticTile.ItemData.Name?.ToLower().Contains("water") == true);
                }
            }

            return false;
        }
    }
}
