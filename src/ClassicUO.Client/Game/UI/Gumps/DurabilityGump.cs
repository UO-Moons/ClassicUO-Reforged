// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using System;
using System.Linq;
using System.Xml;

namespace ClassicUO.Game.UI.Gumps
{
    internal class DurabilityGumpMinimized : Gump
    {
        public DurabilityGumpMinimized(World world) : base(world, 0, 0)
        {
            SetTooltip("Open Equipment Durability Tracker");

            WantUpdateSize = true;
            AcceptMouseInput = true;
            CanMove = true;

            Width = 30;
            Height = 30;

            Add(new GumpPic(0, 0, 5587, 0));
        }

        protected override void OnMouseUp(int x, int y, MouseButtonType button)
        {
            UIManager.GetGump<DurabilitysGump>()?.Dispose();
            UIManager.Add(new DurabilitysGump(World));
        }
    }

    internal class DurabilitysGump : Gump
    {
        private const int WIDTH = 300;
        private const int HEIGHT = 400;

        private enum DurabilityColors : ushort
        {
            RED = 0x0805,
            BLUE = 0x0806,
            GREEN = 0x0808,
            YELLOW = 0x0809
        }

        private readonly DataBox _dataBox;

        public DurabilitysGump(World world) : base(world, 0, 0)
        {
            LayerOrder = UILayer.Default;
            CanCloseWithRightClick = true;
            CanMove = true;

            Width = WIDTH;
            Height = HEIGHT;

            X = Client.Game.Scene.Camera.Bounds.Width - Width - 10;
            Y = Client.Game.Scene.Camera.Bounds.Y + 10;

            BorderControl border = new BorderControl(0, 0, Width, Height, 4);

            Add(border);
            Add(new AlphaBlendControl(0.9f) { Width = Width, Height = Height });

            BuildHeader();

            ScrollArea area = new ScrollArea(10, 30, Width - 20, Height - 50, true)
            {
                ScrollbarBehaviour = ScrollbarBehaviour.ShowAlways
            };

            Add(area);

            _dataBox = new DataBox(0, 0, Width - 40, Height - 20);
            area.Add(_dataBox);

            UpdateContents();
        }

        private void BuildHeader()
        {
            DataBox area = new DataBox(0, 0, Width, 24)
            {
                WantUpdateSize = false
            };

            Label label = new Label("Equipment Durability", true, 0xFF)
            {
                Y = 4
            };

            label.X = (Width >> 1) - (label.Width >> 1);

            area.Add(label);

            Add(area);
        }

        protected override void UpdateContents()
        {
            _dataBox.Clear();
            _dataBox.WantUpdateSize = true;

            var redTexture = Client.Game.UO.Gumps.GetGump((uint)DurabilityColors.RED).Texture;

            if (redTexture == null)
            {
                return;
            }

            Rectangle barBounds = redTexture.Bounds;
            int startY = 0;

            if (World?.DurabilityManager == null)
            {
                return;
            }

            foreach (var durability in World.DurabilityManager.Durabilties.OrderBy(d => d.Percentage))
            {
                if (durability.MaxDurabilty <= 0)
                {
                    continue;
                }

                Item item = World.Items.Get((uint)durability.Serial);

                if (item == null)
                {
                    continue;
                }

                DataBox area = new DataBox(0, startY, Width - 40, 44)
                {
                    AcceptMouseInput = true,
                    WantUpdateSize = false,
                    CanMove = true
                };

                Label name = new Label
                (
                    string.IsNullOrWhiteSpace(item.Name) ? item.Layer.ToString() : item.Name,
                    true,
                    0xFFFF
                );

                area.Add(name);

                GumpPic red = new GumpPic
                (
                    0,
                    name.Y + name.Height + 5,
                    (ushort)DurabilityColors.RED,
                    0
                );

                area.Add(red);

                DurabilityColors statusGump = DurabilityColors.GREEN;

                if (durability.Percentage < 0.70)
                {
                    statusGump = DurabilityColors.YELLOW;
                }
                else if (durability.Percentage < 0.95)
                {
                    statusGump = DurabilityColors.BLUE;
                }

                if (durability.Percentage > 0)
                {
                    area.Add
                    (
                        new GumpPicTiled
                        (
                            0,
                            red.Y,
                            (int)Math.Floor(barBounds.Width * durability.Percentage),
                            barBounds.Height,
                            (ushort)statusGump
                        )
                    );
                }

                Label durabilityText = new Label
                (
                    $"{durability.Durabilty} / {durability.MaxDurabilty}",
                    true,
                    0xFFFF
                )
                {
                    Y = red.Y - 2,
                };

                durabilityText.X = area.Width - durabilityText.Width;
                area.Add(durabilityText);

                _dataBox.Add(area);

                startY += area.Height + 10;
            }
        }

        public override void Save(XmlTextWriter writer)
        {
            base.Save(writer);

            writer.WriteAttributeString("lastX", X.ToString());
            writer.WriteAttributeString("lastY", Y.ToString());
        }

        public override void Restore(XmlElement xml)
        {
            base.Restore(xml);

            if (int.TryParse(xml.GetAttribute("lastX"), out int x))
            {
                X = x;
            }

            if (int.TryParse(xml.GetAttribute("lastY"), out int y))
            {
                Y = y;
            }
        }
    }
}