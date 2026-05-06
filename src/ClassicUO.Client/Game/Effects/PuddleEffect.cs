// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Collections.Generic;
using ClassicUO.Game.Map;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Effects
{
    internal sealed class PuddleEffect
    {
        private const float MAX_LEVEL = 1.0f;
        private const float HIT_ACCUMULATION = 0.18f;
        private const float DRY_DECAY_PER_SECOND = 0.08f;
        private const float WET_DECAY_PER_SECOND = 0.02f;
        private const int MAX_PUDDLES = 768;
        private const int CIRCLE_SEGMENTS = 20;
        private const float MIN_VISIBLE_LEVEL = 0.03f;

        private readonly Dictionary<Point, Puddle> _puddles = new Dictionary<Point, Puddle>();

        public void AddRainHit(float worldX, float worldY, float intensity = 1f)
        {
            (int tileX, int tileY) = CoordinateHelper.IsometricToTile(worldX, worldY);
            var tile = new Point(tileX, tileY);

            if (!_puddles.TryGetValue(tile, out Puddle puddle))
            {
                if (_puddles.Count >= MAX_PUDDLES)
                {
                    return;
                }

                puddle = new Puddle
                {
                    Level = 0f,
                    WorldX = worldX,
                    WorldY = worldY
                };
            }
            else
            {
                puddle.WorldX = worldX;
                puddle.WorldY = worldY;
            }

            puddle.Level = Math.Min(MAX_LEVEL, puddle.Level + HIT_ACCUMULATION * Math.Max(0.25f, intensity));
            _puddles[tile] = puddle;
        }

        public void Update(float deltaTime, bool isRaining, int viewportOffsetX, int viewportOffsetY, int visibleRangeX, int visibleRangeY)
        {
            if (_puddles.Count == 0)
            {
                return;
            }

            float decay = (isRaining ? WET_DECAY_PER_SECOND : DRY_DECAY_PER_SECOND) * deltaTime;
            var keys = new List<Point>(_puddles.Keys);

            for (int i = 0; i < keys.Count; i++)
            {
                Point key = keys[i];
                Puddle puddle = _puddles[key];
                puddle.Level -= decay;

                if (puddle.Level <= MIN_VISIBLE_LEVEL)
                {
                    _puddles.Remove(key);
                    continue;
                }

                puddle.ScreenX = puddle.WorldX - viewportOffsetX;
                puddle.ScreenY = puddle.WorldY - viewportOffsetY;

                if (puddle.ScreenX < -visibleRangeX || puddle.ScreenX > visibleRangeX ||
                    puddle.ScreenY < -visibleRangeY || puddle.ScreenY > visibleRangeY)
                {
                    continue;
                }

                _puddles[key] = puddle;
            }
        }

        public void Draw(UltimaBatcher2D batcher, float layerDepth)
        {
            foreach (Puddle puddle in _puddles.Values)
            {
                float alpha = puddle.Level * 0.35f;
                var color = new Color(70, 100, 130) * alpha;
                DrawEllipse(batcher, new Vector2(puddle.ScreenX, puddle.ScreenY), 8f + (puddle.Level * 14f), color, layerDepth + 0.0002f);
            }
        }

        public void Reset() => _puddles.Clear();

        private static void DrawEllipse(UltimaBatcher2D batcher, Vector2 center, float radius, Color color, float layerDepth)
        {
            float angleStep = (float)(Math.PI * 2.0 / CIRCLE_SEGMENTS);
            float radiusY = radius * 0.5f;
            Vector2 prevPoint = center + new Vector2(radius, 0);

            for (int i = 1; i <= CIRCLE_SEGMENTS; i++)
            {
                float angle = i * angleStep;
                Vector2 currentPoint = center + new Vector2((float)(Math.Cos(angle) * radius), (float)(Math.Sin(angle) * radiusY));
                batcher.DrawLine(SolidColorTextureCache.GetTexture(color), prevPoint, currentPoint, Vector3.UnitZ, 1, layerDepth);
                prevPoint = currentPoint;
            }
        }

        private struct Puddle
        {
            public float Level;
            public float WorldX;
            public float WorldY;
            public float ScreenX;
            public float ScreenY;
        }
    }
}
