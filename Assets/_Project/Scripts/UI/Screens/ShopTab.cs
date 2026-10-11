using UnityEngine;

namespace MidnightLegacy
{
    /// <summary>
    /// Real money shop. Everything sold here is cosmetic or removes ads. Nothing changes how the car drives
    /// and nothing sells cash or reputation, so the game stays fair for players who never pay.
    /// </summary>
    public sealed class ShopTab
    {
        sealed class Item
        {
            public string id, title, desc;
        }

        static readonly Item[] Items =
        {
            new Item { id = ProductIds.RemoveAds, title = "Remove Ads", desc = "No more ads on the results screen. Optional reward ads stay available if you want them." },
            new Item { id = ProductIds.SupporterPack, title = "Supporter Pack", desc = "ST Indigo paint and ST Spoke wheels, in the colour of the studio that made the game. Thank you!" },
            new Item { id = ProductIds.PaintPack, title = "Paint Pack", desc = "Four extra paints: Violet Haze, Sakura, Chrome and Gold Leaf." },
            new Item { id = ProductIds.WheelPack, title = "Wheel Pack", desc = "Three wheel designs: Neon Blue Mesh, Red Five and White Mesh." },
            new Item { id = ProductIds.DecalPack, title = "Decal Pack", desc = "Twin Gold stripes, Checker Roof and Lightning." }
        };

        public Vector2 Draw(AppRoot app, Rect view, Vector2 scroll)
        {
            float w = view.width;
            float content = 220f + Items.Length * 226f + 200f;
            scroll = UIKit.BeginScroll(view, scroll, content);
            float y = -scroll.y;

            UIKit.Label(new Rect(0f, y, w, 40f), "SHOP", 32, UIKit.Accent, TextAnchor.UpperLeft, true);
            y += 50f;
            string note = "Everything here is cosmetic or removes ads. Nothing changes how the car drives, and cash and reputation are only earned by driving.";
            float nh = UIKit.TextHeight(note, 24, w);
            UIKit.Label(new Rect(0f, y, w, nh), note, 24, UIKit.Dim, TextAnchor.UpperLeft, false, true);
            y += nh + 20f;

            IPurchaseService iap = Monetization.Iap;
            for (int i = 0; i < Items.Length; i++)
            {
                Item it = Items[i];
                bool owned = iap != null && iap.IsOwned(it.id);
                Rect r = new Rect(0f, y, w, 212f);
                UIKit.Round(r, UIKit.PanelLight);
                UIKit.Label(new Rect(28f, y + 14f, w - 330f, 44f), it.title, 36, UIKit.Text, TextAnchor.UpperLeft, true);
                UIKit.Label(new Rect(28f, y + 64f, w - 330f, 130f), it.desc, 24, UIKit.Dim, TextAnchor.UpperLeft, false, true);

                Rect b = new Rect(w - 280f, y + 56f, 252f, 100f);
                if (owned) UIKit.Button(b, "OWNED", BtnStyle.Secondary, false, 28);
                else
                {
                    string price = iap != null ? iap.LocalisedPrice(it.id) : "--";
                    string pid = it.id;
                    if (UIKit.Button(b, price, BtnStyle.Gold, true, 32)) app.Buy(pid);
                }
                y += 226f;
            }

            if (UIKit.Button(new Rect(0f, y + 10f, w, 96f), "RESTORE PURCHASES", BtnStyle.Secondary, true, 28)) app.RestorePurchases();

            UIKit.EndScroll();
            return scroll;
        }
    }
}
