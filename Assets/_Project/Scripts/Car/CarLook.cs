using UnityEngine;

namespace MidnightLegacy
{
    /// <summary>What a car looks like. Purely visual: nothing in here touches handling.</summary>
    public sealed class CarLook
    {
        public Color body = new Color(0.88f, 0.88f, 0.86f);
        public Color trim = new Color(0.055f, 0.055f, 0.065f);
        public Color glass = new Color(0.07f, 0.09f, 0.13f);
        public Color rim = new Color(0.55f, 0.56f, 0.60f);

        /// <summary>0 coupe (the Kairo), 1 hatchback, 2 wedge sports car.</summary>
        public int style;
        /// <summary>0 duck tail, 1 none, 2 big wing, 3 lip.</summary>
        public int spoiler;
        /// <summary>0 none, 1 centre stripe, 2 side stripes, 3 hood panel, 4 number roundel, 5 twin gold stripe, 6 checker roof, 7 lightning.</summary>
        public int decal;
        public Color decalColor = new Color(0.05f, 0.05f, 0.06f);
        /// <summary>0 bar, 1 five spoke, 2 mesh.</summary>
        public int rimStyle;
        public Color underglow = Color.black;
        public Color lightColor = new Color(1f, 0.92f, 0.65f);
        /// <summary>Unlit, dark tail lights (the Unknown Driver).</summary>
        public bool darkLights;
        /// <summary>Shown for the masked driver silhouette in the cabin.</summary>
        public bool driverSilhouette;
    }

    /// <summary>Anything that can drive the wheel and body animation of a CarVisuals.</summary>
    public interface ICarState
    {
        float LateralAccel { get; }
        float LongAccel { get; }
        float SteerAngle { get; }
        float WheelSpinRad { get; }
        float BrakeAmount { get; }
    }

    /// <summary>A parked car (garage display, menu).</summary>
    public sealed class StaticCarState : ICarState
    {
        public float LateralAccel { get { return 0f; } }
        public float LongAccel { get { return 0f; } }
        public float SteerAngle { get { return 0f; } }
        public float WheelSpinRad { get { return 0f; } }
        public float BrakeAmount { get { return 0f; } }
    }

    public sealed class CosmeticItem
    {
        public string id;
        public string name;
        public Color color;
        public int value;         // style index for rims, decals, spoilers
        public int price;         // cash price, 0 = free
        public string pack;       // product id that unlocks it, or null
        public bool supporter;    // unlocked by the supporter pack
    }

    /// <summary>The cosmetic catalogue. All of it is visual only, so selling it never makes the game pay to win.</summary>
    public static class Cosmetics
    {
        static Color C(int hex)
        {
            return new Color(((hex >> 16) & 255) / 255f, ((hex >> 8) & 255) / 255f, (hex & 255) / 255f);
        }

        static CosmeticItem I(string id, string name, int hex, int value, int price, string pack = null, bool supporter = false)
        {
            return new CosmeticItem { id = id, name = name, color = C(hex), value = value, price = price, pack = pack, supporter = supporter };
        }

        public static readonly CosmeticItem[] Paints =
        {
            I("paint_white", "Ghost White", 0xE1E1DB, 0, 0),
            I("paint_grey", "Asphalt Grey", 0x5A5C66, 0, 0),
            I("paint_red", "Signal Red", 0xC4261E, 0, 200),
            I("paint_blue", "Harbor Blue", 0x2A5FA8, 0, 200),
            I("paint_green", "Pine Green", 0x2F6B45, 0, 200),
            I("paint_orange", "Ember Orange", 0xE0701C, 0, 250),
            I("paint_mustard", "Mustard", 0xD9A82A, 0, 250),
            I("paint_violet", "Violet Haze", 0x7A4FC2, 0, 0, ProductIds.PaintPack),
            I("paint_sakura", "Sakura", 0xE79DB8, 0, 0, ProductIds.PaintPack),
            I("paint_chrome", "Chrome", 0xB9C0CC, 0, 0, ProductIds.PaintPack),
            I("paint_gold", "Gold Leaf", 0xD9B13F, 0, 0, ProductIds.PaintPack),
            I("paint_st", "ST Indigo", 0x6B77D6, 0, 0, null, true)
        };

        public static readonly CosmeticItem[] Rims =
        {
            I("rim_stock", "Stock Bar", 0x8C8F99, 0, 0),
            I("rim_five", "Five Spoke", 0xC7CAD3, 1, 150),
            I("rim_black", "Black Five", 0x20222A, 1, 200),
            I("rim_gold", "Mesh Gold", 0xD2A93A, 2, 250),
            I("rim_blue", "Neon Blue Mesh", 0x2FA4FF, 2, 0, ProductIds.WheelPack),
            I("rim_red", "Red Five", 0xE23A32, 1, 0, ProductIds.WheelPack),
            I("rim_white", "White Mesh", 0xF2F2F2, 2, 0, ProductIds.WheelPack),
            I("rim_st", "ST Spoke", 0x6B77D6, 1, 0, null, true)
        };

        public static readonly CosmeticItem[] Decals =
        {
            I("decal_none", "None", 0x000000, 0, 0),
            I("decal_centre", "Centre Stripe", 0x15151A, 1, 120),
            I("decal_side", "Side Stripes", 0x15151A, 2, 120),
            I("decal_hood", "Matte Hood", 0x15151A, 3, 100),
            I("decal_roundel", "Number 8", 0xF2F2F2, 4, 150),
            I("decal_gold", "Twin Gold", 0xD9B13F, 5, 0, ProductIds.DecalPack),
            I("decal_checker", "Checker Roof", 0x15151A, 6, 0, ProductIds.DecalPack),
            I("decal_bolt", "Lightning", 0xFFD43B, 7, 0, ProductIds.DecalPack)
        };

        public static readonly CosmeticItem[] Spoilers =
        {
            I("spoiler_duck", "Stock Duck Tail", 0x000000, 0, 0),
            I("spoiler_none", "Clean Deck", 0x000000, 1, 0),
            I("spoiler_lip", "Lip", 0x000000, 3, 80),
            I("spoiler_wing", "Big Wing", 0x000000, 2, 300)
        };

        public static CosmeticItem[] ListFor(int category)
        {
            switch (category)
            {
                case 0: return Paints;
                case 1: return Rims;
                case 2: return Decals;
                default: return Spoilers;
            }
        }

        public static CosmeticItem Find(CosmeticItem[] list, string id)
        {
            for (int i = 0; i < list.Length; i++) if (list[i].id == id) return list[i];
            return list[0];
        }

        public static bool IsUnlocked(CosmeticItem item, SaveData save)
        {
            if (item.price == 0 && item.pack == null && !item.supporter) return true;
            if (item.pack != null && Monetization.Iap != null && Monetization.Iap.IsOwned(item.pack)) return true;
            if (item.supporter && Monetization.Iap != null && Monetization.Iap.IsOwned(ProductIds.SupporterPack)) return true;
            return save != null && save.unlockedCosmetics.Contains(item.id);
        }

        /// <summary>The player's car as set up in the garage.</summary>
        public static CarLook LookFor(SaveData save)
        {
            CarLook look = new CarLook();
            if (save == null) return look;
            look.body = Find(Paints, save.paintId).color;
            CosmeticItem rim = Find(Rims, save.rimId);
            look.rim = rim.color;
            look.rimStyle = rim.value;
            CosmeticItem decal = Find(Decals, save.decalId);
            look.decal = decal.value;
            look.decalColor = decal.color;
            look.spoiler = Find(Spoilers, save.spoilerId).value;
            look.style = 0;
            return look;
        }
    }
}
