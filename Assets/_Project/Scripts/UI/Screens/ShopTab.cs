using UnityEngine;

namespace MidnightLegacy
{
    /// <summary>
    /// Real money shop. Everything sold here is cosmetic or removes ads. Nothing changes how the car drives
    /// and nothing sells cash or reputation, so the game stays fair for players who never pay.
    /// Each item has an optional painted picture (shop_removeads, shop_supporter, shop_paint, shop_wheels, shop_decals).
    /// </summary>
    public sealed class ShopTab
    {
        sealed class Item
        {
            public string id, art, title, desc;
            public Color tone;
        }

        static readonly Item[] Items =
        {
            new Item { id = ProductIds.RemoveAds, art = "shop_removeads", tone = new Color(0.85f, 0.25f, 0.30f), title = "Remove Ads", desc = "No more ads on the results screen. Optional reward ads stay available if you want them." },
            new Item { id = ProductIds.SupporterPack, art = "shop_supporter", tone = new Color(0.42f, 0.47f, 0.84f), title = "Supporter Pack", desc = "ST Indigo paint and ST Spoke wheels, in the colour of the studio that made the game. Thank you!" },
            new Item { id = ProductIds.PaintPack, art = "shop_paint", tone = new Color(0.70f, 0.40f, 0.90f), title = "Paint Pack", desc = "Four extra paints: Violet Haze, Sakura, Chrome and Gold Leaf." },
            new Item { id = ProductIds.WheelPack, art = "shop_wheels", tone = new Color(0.25f, 0.65f, 0.95f), title = "Wheel Pack", desc = "Three wheel designs: Neon Blue Mesh, Red Five and White Mesh." },
            new Item { id = ProductIds.DecalPack, art = "shop_decals", tone = new Color(1f, 0.76f, 0.26f), title = "Decal Pack", desc = "Twin Gold stripes, Checker Roof and Lightning." }
        };

        public Vector2 Draw(AppRoot app, Rect view, Vector2 scroll)
        {
            float w = view.width;
            float content = 300f + Items.Length * 252f + 200f;
            scroll = UIKit.BeginScroll(view, scroll, content);
            float y = -scroll.y;

            // ---- header: big italic title, stripes and a checker strip ----
            UIKit.Label(new Rect(4f, y, w * 0.5f, 100f), "SHOP", 84, UIKit.Text, TextAnchor.MiddleLeft, true);
            float tw = UIKit.TextWidth("SHOP", 84, true);
            UIKit.Slashes(new Rect(tw + 30f, y + 18f, 70f, 64f), UIKit.EdgeBlue);
            DrawChecker(new Rect(w - 300f, y + 22f, 300f, 56f));
            y += 108f;

            string note = "Everything here is cosmetic or removes ads. Nothing changes how the car drives, and cash and reputation are only earned by driving.";
            float nh = UIKit.TextHeight(note, 26, w);
            UIKit.Label(new Rect(4f, y, w, nh), note, 26, UIKit.Dim, TextAnchor.UpperLeft, false, true);
            y += nh + 24f;

            IPurchaseService iap = Monetization.Iap;
            for (int i = 0; i < Items.Length; i++)
            {
                Item it = Items[i];
                bool owned = iap != null && iap.IsOwned(it.id);
                Rect r = new Rect(0f, y, w, 238f);
                UIKit.Tile(r, UIKit.PanelLight);

                DrawPicture(new Rect(16f, y + 16f, 206f, 206f), it);

                float tx = 246f;
                float textW = w - tx - 300f;
                UIKit.Label(new Rect(tx, y + 18f, textW, 54f), it.title, 40, UIKit.Text, TextAnchor.UpperLeft, true);
                UIKit.Label(new Rect(tx, y + 76f, textW, 150f), it.desc, 24, UIKit.Dim, TextAnchor.UpperLeft, false, true);

                Rect b = new Rect(w - 276f, y + 70f, 252f, 98f);
                if (owned) UIKit.Button(b, "OWNED", BtnStyle.Secondary, false, 28);
                else
                {
                    string price = iap != null ? iap.LocalisedPrice(it.id) : "--";
                    string pid = it.id;
                    if (UIKit.Button(b, price, BtnStyle.Gold, true, 32)) app.Buy(pid);
                }
                y += 252f;
            }

            if (UIKit.Button(new Rect(0f, y + 10f, w, 96f), "RESTORE PURCHASES", BtnStyle.Secondary, true, 28)) app.RestorePurchases();

            UIKit.EndScroll();
            return scroll;
        }

        /// <summary>Painted picture if the file exists, otherwise a tinted tile with a plain mark.</summary>
        static void DrawPicture(Rect r, Item it)
        {
            Texture2D t = UIKit.Art(it.art);
            UIKit.Round(r, new Color(0.03f, 0.04f, 0.10f, 1f));
            if (t != null) UIKit.Cover(r, t, t.width / Mathf.Max(1f, (float)t.height), Color.white);
            else
            {
                UIKit.Fade(r, new Color(it.tone.r * 0.55f, it.tone.g * 0.55f, it.tone.b * 0.55f, 1f), true);
                UIKit.Circle(new Rect(r.x + r.width * 0.25f, r.y + r.height * 0.25f, r.width * 0.5f, r.height * 0.5f), new Color(it.tone.r, it.tone.g, it.tone.b, 0.9f));
                UIKit.Label(new Rect(r.x + r.width * 0.25f, r.y + r.height * 0.25f, r.width * 0.5f, r.height * 0.5f), it.title.Substring(0, 1), 64, Color.white, TextAnchor.MiddleCenter, true);
            }
            UIKit.Outline(r, new Color(0.28f, 0.45f, 1f, 0.55f));
        }

        /// <summary>A small chequered flag strip, drawn.</summary>
        static void DrawChecker(Rect r)
        {
            int cols = 10, rows = 2;
            float cw = r.width / cols, ch = r.height / rows;
            for (int j = 0; j < rows; j++)
                for (int i = 0; i < cols; i++)
                {
                    bool dark = (i + j) % 2 == 0;
                    UIKit.Fill(new Rect(r.x + i * cw, r.y + j * ch, cw, ch), dark ? new Color(1f, 1f, 1f, 0.55f) : new Color(1f, 1f, 1f, 0.10f));
                }
        }
    }
}
