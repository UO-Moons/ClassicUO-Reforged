// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Runtime.CompilerServices;
using ClassicUO.Configuration;
using ClassicUO.IO;
using ClassicUO.Assets;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.GameObjects
{
    enum ObjectHandlesStatus
    {
        NONE,
        OPEN,
        CLOSED,
        DISPLAYING
    }

    internal abstract partial class GameObject
    {
        public byte AlphaHue;
        public bool AllowedToDraw = true;
        public ObjectHandlesStatus ObjectHandlesStatus;
        public Rectangle FrameInfo;
        protected bool IsFlipped;

        public abstract bool Draw(UltimaBatcher2D batcher, int posX, int posY, float depth);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float CalculateDepthZ()
        {
            int x = X;
            int y = Y;
            int z = PriorityZ;

            // Offsets are in SCREEN coordinates
            if (Offset.X > 0 && Offset.Y < 0)
            {
                // North
            }
            else if (Offset.X > 0 && Offset.Y == 0)
            {
                // Northeast
                x++;
            }
            else if (Offset.X > 0 && Offset.Y > 0)
            {
                // East
                z += Math.Max(0, (int)Offset.Z);
                x++;
            }
            else if (Offset.X == 0 && Offset.Y > 0)
            {
                // Southeast
                x++;
                y++;
            }
            else if (Offset.X < 0 && Offset.Y > 0)
            {
                // South
                z += Math.Max(0, (int)Offset.Z);
                y++;
            }
            else if (Offset.X < 0 && Offset.Y == 0)
            {
                // Southwest
                y++;
            }
            else if (Offset.X < 0 && Offset.Y > 0)
            {
                // West
            }
            else if (Offset.X == 0 && Offset.Y < 0)
            {
                // Northwest
            }

            return (x + y) + (127 + z) * 0.01f;
        }

        public Rectangle GetOnScreenRectangle()
        {
            Rectangle prect = Rectangle.Empty;

            prect.X = (int)(RealScreenPosition.X - FrameInfo.X + 22 + Offset.X);
            prect.Y = (int)(RealScreenPosition.Y - FrameInfo.Y + 22 + (Offset.Y - Offset.Z));
            prect.Width = FrameInfo.Width;
            prect.Height = FrameInfo.Height;

            return prect;
        }

        public virtual bool TransparentTest(int z)
        {
            return false;
        }

        protected static void DrawStatic(
            UltimaBatcher2D batcher,
            ushort graphic,
            int x,
            int y,
            Vector3 hue,
            float depth,
            bool isWet = false
        )
        {
            ref readonly var artInfo = ref Client.Game.UO.Arts.GetArt(graphic);

            if (artInfo.Texture != null)
            {
                ref var index = ref Client.Game.UO.FileManager.Arts.File.GetValidRefEntry(graphic + 0x4000);
                index.Width = (short)((artInfo.UV.Width >> 1) - 22);
                index.Height = (short)(artInfo.UV.Height - 44);

                x -= index.Width;
                y -= index.Height;

                var pos = new Vector2(x, y);
                var scale = Vector2.One;
                if (isWet)
                {
                    batcher.Draw(
                        artInfo.Texture,
                        pos,
                        artInfo.UV,
                        hue,
                        0f,
                        Vector2.Zero,
                        scale,
                        SpriteEffects.None,
                        depth + 0.5f
                    );

                    var sin = (float)Math.Sin(Time.Ticks / 1000f);
                    var cos = (float)Math.Cos(Time.Ticks / 1000f);
                    scale = new Vector2(1.1f + sin * 0.1f, 1.1f + cos * 0.5f * 0.1f);
                }

                batcher.Draw(
                    artInfo.Texture,
                    pos,
                    artInfo.UV,
                    hue,
                    0f,
                    Vector2.Zero,
                    scale,
                    SpriteEffects.None,
                    depth + 0.5f
                );
            }
        }

        protected static void DrawGump(
            UltimaBatcher2D batcher,
            ushort graphic,
            int x,
            int y,
            Vector3 hue,
            float depth
        )
        {
            ref readonly var gumpInfo = ref Client.Game.UO.Gumps.GetGump(graphic);

            if (gumpInfo.Texture != null)
            {
                batcher.Draw(
                    gumpInfo.Texture,
                    new Vector2(x, y),
                    gumpInfo.UV,
                    hue,
                    0f,
                    Vector2.Zero,
                    1f,
                    SpriteEffects.None,
                    depth + 0.5f
                );
            }
        }

        protected static void DrawStaticRotated(
            UltimaBatcher2D batcher,
            ushort graphic,
            int x,
            int y,
            float angle,
            Vector3 hue,
            float depth
        )
        {
            ref readonly var artInfo = ref Client.Game.UO.Arts.GetArt(graphic);

            if (artInfo.Texture != null)
            {
                ref var index = ref Client.Game.UO.FileManager.Arts.File.GetValidRefEntry(graphic + 0x4000);
                index.Width = (short)((artInfo.UV.Width >> 1) - 22);
                index.Height = (short)(artInfo.UV.Height - 44);

                batcher.Draw(
                    artInfo.Texture,
                    new Rectangle(
                        x - index.Width,
                        y - index.Height,
                        artInfo.UV.Width,
                        artInfo.UV.Height
                    ),
                    artInfo.UV,
                    hue,
                    angle,
                    Vector2.Zero,
                    SpriteEffects.None,
                    depth + 0.5f
                );
            }
        }

        protected static void DrawStaticAnimated(
            UltimaBatcher2D batcher,
            ushort graphic,
            int x,
            int y,
            Vector3 hue,
            bool shadow,
            float depth,
            bool animateWater = false,
            bool animateFoliage = false,
            uint animationSeed = 0
        )
        {
            ref UOFileIndex index = ref Client.Game.UO.FileManager.Arts.File.GetValidRefEntry(graphic + 0x4000);

            graphic = (ushort)(graphic + index.AnimOffset);

            ref readonly var artInfo = ref Client.Game.UO.Arts.GetArt(graphic);

            if (artInfo.Texture != null)
            {
                index = ref Client.Game.UO.FileManager.Arts.File.GetValidRefEntry(graphic + 0x4000);
                index.Width = (short)((artInfo.UV.Width >> 1) - 22);
                index.Height = (short)(artInfo.UV.Height - 44);

                x -= index.Width;
                y -= index.Height;

                Vector2 pos = new Vector2(x, y);

                if (shadow)
                {
                    batcher.DrawShadow(artInfo.Texture, pos, artInfo.UV, false, depth + 0.25f);
                }

                var scale = Vector2.One;

                if (animateWater || animateFoliage)
                {
                    float globalTime = Time.Ticks / 1000f;

                    if (animateFoliage && animationSeed != 0)
                    {
                        float weatherSway = Weather.FoliageSwayIntensity;

                        // Spatially-stable phase so nearby trees move differently and gusts travel across terrain.
                        float phase = ((animationSeed & 0xFFFF) / 65535f) * MathHelper.TwoPi;
                        float gustDelay = (animationSeed & 0xFF) / 255f * 1.35f;
                        float localTime = globalTime - gustDelay;

                        // Low-frequency gust envelope + irregular turbulence for natural acceleration/deceleration.
                        float gustA = 0.5f + 0.5f * (float)Math.Sin((localTime / 15.0f) * MathHelper.TwoPi + phase);
                        float gustB = 0.5f + 0.5f * (float)Math.Sin((localTime / 23.0f) * MathHelper.TwoPi + phase * 0.67f);
                        float turbulence = (float)Math.Sin(localTime * 3.7f + phase * 1.9f) * 0.5f + (float)Math.Sin(localTime * 5.3f + phase * 0.41f) * 0.5f;

                        float gustStrength = Math.Clamp((0.25f + gustA * 0.5f + gustB * 0.25f + turbulence * 0.12f) * weatherSway, 0f, 1f);

                        // Layered sway: strong primary bend + softer cross motion + subtle leaf flutter.
                        float trunkWave = (float)Math.Sin(localTime * (0.62f + weatherSway * 0.18f) + phase);
                        float branchWave = (float)Math.Sin(localTime * (1.35f + weatherSway * 0.35f) + phase * 1.37f);
                        float leafFlutter = (float)Math.Sin(localTime * (4.15f + weatherSway * 0.85f) + phase * 2.21f);

                        float swayX = (trunkWave * 0.045f + branchWave * 0.075f + leafFlutter * 0.02f) * gustStrength;
                        float swayY = ((float)Math.Cos(localTime * 0.9f + phase * 0.7f) * 0.02f + leafFlutter * 0.01f) * gustStrength;

                        scale = new Vector2(1.1f + swayX, 1.1f + swayY);
                    }
                    else if (animateWater)
                    {
                        // Preserve water animation behavior.
                        var sin = (float)Math.Sin(globalTime);
                        var cos = (float)Math.Cos(globalTime);

                        scale = new Vector2(1.1f + sin * 0.1f, 1.1f + cos * 0.5f * 0.1f);
                    }
                }

                batcher.Draw(
                    artInfo.Texture,
                    pos,
                    artInfo.UV,
                    hue,
                    0f,
                    Vector2.Zero,
                    scale,
                    SpriteEffects.None,
                    depth + 0.5f
                );
            }
        }
    }
}
