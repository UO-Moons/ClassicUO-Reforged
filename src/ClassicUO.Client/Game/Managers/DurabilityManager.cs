// SPDX-License-Identifier: BSD-2-Clause

using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.UI.Gumps;

namespace ClassicUO.Game.Managers
{
    internal sealed class DurabilityManager
    {
        private readonly World _world;
        private readonly Dictionary<uint, DurabiltyProp> _itemLayerSlots = new Dictionary<uint, DurabiltyProp>();

        public static readonly Layer[] EquipLayers =
        {
            Layer.Cloak, Layer.Shirt, Layer.Pants, Layer.Shoes, Layer.Legs, Layer.Arms, Layer.Torso, Layer.Tunic,
            Layer.Ring, Layer.Bracelet, Layer.Gloves, Layer.Skirt, Layer.Robe, Layer.Waist, Layer.Necklace,
            Layer.Beard, Layer.Earrings, Layer.Helmet, Layer.OneHanded, Layer.TwoHanded, Layer.Talisman
        };

        public IReadOnlyCollection<DurabiltyProp> Durabilties => _itemLayerSlots.Values;

        public DurabilityManager(World world)
        {
            _world = world;
        }

        public bool TryGetDurability(uint serial, out DurabiltyProp durability)
        {
            return _itemLayerSlots.TryGetValue(serial, out durability);
        }

        public void UpdateItem(uint serial)
        {
            if (_world == null || _world.Player == null)
            {
                return;
            }

            if (!SerialHelper.IsValid(serial) || !SerialHelper.IsItem(serial))
            {
                _itemLayerSlots.Remove(serial);
                return;
            }

            if (!_world.Items.TryGetValue(serial, out Item item) || item == null || item.IsDestroyed)
            {
                _itemLayerSlots.Remove(serial);
                return;
            }

            if (!EquipLayers.Contains(item.Layer) || item.Container != _world.Player.Serial)
            {
                _itemLayerSlots.Remove(serial);
                UIManager.GetGump<DurabilitysGump>()?.RequestUpdateContents();
                return;
            }

            if (!_world.OPL.TryGetNameAndData(serial, out string name, out string data))
            {
                return;
            }

            DurabiltyProp durability = ParseDurability((int)item.Serial, data);

            if (durability.MaxDurabilty <= 0)
            {
                _itemLayerSlots.Remove(serial);
                UIManager.GetGump<DurabilitysGump>()?.RequestUpdateContents();
                return;
            }

            if (_itemLayerSlots.TryGetValue(item.Serial, out DurabiltyProp slot))
            {
                slot.Durabilty = durability.Durabilty;
                slot.MaxDurabilty = durability.MaxDurabilty;
            }
            else
            {
                _itemLayerSlots[item.Serial] = durability;
            }

            UIManager.GetGump<DurabilitysGump>()?.RequestUpdateContents();
        }

        public void Clear()
        {
            _itemLayerSlots.Clear();
            UIManager.GetGump<DurabilitysGump>()?.RequestUpdateContents();
        }

        public static DurabiltyProp ParseDurability(int serial, string data)
        {
            if (string.IsNullOrEmpty(data))
            {
                return new DurabiltyProp();
            }

            Match match = Regex.Match(data, @"Durability\s+(\d+)\s*/\s*(\d+)", RegexOptions.IgnoreCase);

            if (!match.Success)
            {
                return new DurabiltyProp();
            }

            if (!int.TryParse(match.Groups[1].Value, out int current))
            {
                return new DurabiltyProp();
            }

            if (!int.TryParse(match.Groups[2].Value, out int max))
            {
                return new DurabiltyProp();
            }

            return new DurabiltyProp(serial, current, max);
        }
    }

    internal sealed class DurabiltyProp
    {
        public int Serial { get; set; }
        public int Durabilty { get; set; }
        public int MaxDurabilty { get; set; }

        public float Percentage => MaxDurabilty > 0 ? (float)Durabilty / MaxDurabilty : 0f;

        public DurabiltyProp(int serial, int current, int max)
        {
            Serial = serial;
            Durabilty = current;
            MaxDurabilty = max;
        }

        public DurabiltyProp() : this(0, 0, 0)
        {
        }
    }
}