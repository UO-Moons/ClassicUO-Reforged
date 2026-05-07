// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.Scenes;
using ClassicUO.IO;
using ClassicUO.Assets;
using ClassicUO.Renderer;
using System.Collections.Generic;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.GameObjects
{
    internal sealed partial class Static
    {
        private static readonly HashSet<ushort> _swayingLeafGraphics =
        [
            0x0D95, 0x0D96, 0x0D97, 0x0D99, 0x0D9A, 0x0D9B, 0x0D9D, 0x0D9E, 0x0D9F,
            0x0DA1, 0x0DA2, 0x0DA3, 0x0DA5, 0x0DA6, 0x0DA7, 0x0DA9, 0x0DAA, 0x0DAB,
            0x0CCE, 0x0CCF, 0x0CD1, 0x0CD2, 0x0CD4, 0x0CD5, 0x0CD7, 0x0CD9, 0x0CDB,
            0x0CDC, 0x0CDE, 0x0CDF, 0x0CE1, 0x0CE2, 0x0CE4, 0x0CE5, 0x0CE7, 0x0CE8
        ];

        private int _canBeTransparent;

        public override bool TransparentTest(int z)
        {
            bool r = true;

            if (Z <= z - ItemData.Height)
            {
                r = false;
            }
            else if (z < Z && (_canBeTransparent & 0xFF) == 0)
            {
                r = false;
            }

            return r;
        }

        public override bool Draw(UltimaBatcher2D batcher, int posX, int posY, float depth)
        {
            if (!AllowedToDraw || IsDestroyed)
            {
                return false;
            }

            ushort graphic = Graphic;
            ushort hue = Hue;
            bool partial = ItemData.IsPartialHue;

            if (ProfileManager.CurrentProfile.HighlightGameObjects && SelectedObject.Object == this)
            {
                hue = Constants.HIGHLIGHT_CURRENT_OBJECT_HUE;
                partial = false;
            }
            else if (
                ProfileManager.CurrentProfile.NoColorObjectsOutOfRange
                && Distance > World.ClientViewRange
            )
            {
                hue = Constants.OUT_RANGE_COLOR;
                partial = false;
            }
            else if (World.Player.IsDead && ProfileManager.CurrentProfile.EnableBlackWhiteEffect)
            {
                hue = Constants.DEAD_RANGE_COLOR;
                partial = false;
            }

            Vector3 hueVec = ShaderHueTranslator.GetHueVector(hue, partial, AlphaHue / 255f);

            bool isTree = StaticFilters.IsTree(graphic, out _);

            if (isTree && ProfileManager.CurrentProfile.TreeToStumps)
            {
                graphic = Constants.TREE_REPLACE_GRAPHIC;
            }

            bool isSwayingLeaf = _swayingLeafGraphics.Contains(graphic);

            DrawStaticAnimated(
                batcher,
                graphic,
                posX,
                posY,
                hueVec,
                ProfileManager.CurrentProfile.ShadowsEnabled
                    && ProfileManager.CurrentProfile.ShadowsStatics
                    && (isTree || ItemData.IsFoliage || StaticFilters.IsRock(graphic)),
                depth,
                ProfileManager.CurrentProfile.AnimatedWaterEffect
                    && (ItemData.IsWet || isSwayingLeaf),
                isSwayingLeaf ? (uint)(((X + Y) & 0xFF) | ((graphic & 0xFF) << 8)) : 0
            );

            if (ItemData.IsLight)
            {
                Client.Game.GetScene<GameScene>().AddLight(this, this, posX + 22, posY + 22);
            }

            return true;
        }

        public override bool CheckMouseSelection()
        {
            if (
                !(
                    SelectedObject.Object == this
                    || FoliageIndex != -1
                        && Client.Game.GetScene<GameScene>().FoliageIndex == FoliageIndex
                )
            )
            {
                ushort graphic = Graphic;

                bool isTree = StaticFilters.IsTree(graphic, out _);

                if (isTree && ProfileManager.CurrentProfile.TreeToStumps)
                {
                    graphic = Constants.TREE_REPLACE_GRAPHIC;
                }

                ref var index = ref Client.Game.UO.FileManager.Arts.File.GetValidRefEntry(graphic + 0x4000);

                Point position = RealScreenPosition;
                position.X -= index.Width;
                position.Y -= index.Height;

                return Client.Game.UO.Arts.PixelCheck(
                    graphic,
                    SelectedObject.TranslatedMousePositionByViewport.X - position.X,
                    SelectedObject.TranslatedMousePositionByViewport.Y - position.Y
                );
            }

            return false;
        }
    }
}
