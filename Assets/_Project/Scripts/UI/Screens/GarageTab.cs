using UnityEngine;

namespace MidnightLegacy
{
    /// <summary>Garage tab: car rating, repairs, parts and cosmetics. Everything here is bought with in-game cash.</summary>
    public sealed class GarageTab
    {
        int category;     // 0 parts, 1 paint, 2 wheels, 3 decals, 4 spoiler
        CarHandlingProfile baseProfile, effective;
        CarRating rating;
        int ratingStamp = -1;

        static readonly string[] CatNames = { "PARTS", "PAINT", "WHEELS", "DECALS", "SPOILER" };

        public void Reset(AppRoot app)
        {
            ratingStamp = -1;
        }

        void Recompute(SaveData save)
        {
            if (baseProfile == null)
            {
                baseProfile = CarHandlingProfile.CreateDefault();
                effective = CarHandlingProfile.CreateDefault();
                baseProfile.hideFlags = HideFlags.HideAndDontSave;
                effective.hideFlags = HideFlags.HideAndDontSave;
            }
            CarBuild.Apply(effective, baseProfile, save);
            rating = CarBuild.Rate(effective);
        }

        static int Stamp(SaveData s)
        {
            int h = 17;
            for (int i = 0; i < Parts.SlotCount; i++) h = h * 31 + s.TierOf(i);
            return h;
        }

        public Vector2 Draw(AppRoot app, Rect view, Vector2 scroll)
        {
            SaveData save = app.Save;
            int stamp = Stamp(save);
            if (stamp != ratingStamp) { Recompute(save); ratingStamp = stamp; }

            float w = view.width;
            float listH = category == 0 ? Parts.SlotCount * 214f : Cosmetics.ListFor(category - 1).Length * 134f;
            float content = 560f + 120f + listH + 40f;
            scroll = UIKit.BeginScroll(view, scroll, content);
            float y = -scroll.y;

            // ---- rating ----
            UIKit.Label(new Rect(0f, y, w * 0.6f, 44f), "KAIRO HACHI-R", 34, UIKit.Text, TextAnchor.UpperLeft, true);
            UIKit.Label(new Rect(w * 0.5f, y, w * 0.5f, 44f), "RATING " + rating.Overall, 34, UIKit.Gold, TextAnchor.UpperRight, true);
            y += 60f;
            string[] names = { "Power", "Grip", "Braking", "Stability", "Nitro", "Toughness" };
            float[] vals = { rating.power, rating.grip, rating.braking, rating.stability, rating.nitro, rating.toughness };
            float colW = (w - 24f) * 0.5f;
            for (int i = 0; i < 6; i++)
            {
                float cx = (i % 2) * (colW + 24f), cy = y + (i / 2) * 64f;
                UIKit.Label(new Rect(cx, cy, colW, 28f), names[i], 22, UIKit.Dim, TextAnchor.UpperLeft);
                UIKit.Bar(new Rect(cx, cy + 30f, colW, 18f), vals[i] / 100f, UIKit.Accent, new Color(1f, 1f, 1f, 0.1f));
            }
            y += 3 * 64f + 14f;

            // ---- condition ----
            float dmg = Mathf.Clamp01(save.damage);
            UIKit.Label(new Rect(0f, y, w, 30f), "CONDITION  " + Mathf.RoundToInt((1f - dmg) * 100f) + "%", 24, dmg > 0.3f ? UIKit.Bad : UIKit.Dim, TextAnchor.UpperLeft, true);
            UIKit.Bar(new Rect(0f, y + 34f, w, 18f), 1f - dmg, dmg > 0.5f ? UIKit.Bad : UIKit.Good, new Color(1f, 1f, 1f, 0.1f));
            y += 66f;
            if (dmg > 0.01f)
            {
                int cost = Economy.RepairCost(save);
                bool adReady = AdPolicy.CanOfferRewarded(AdPlacement.RewardedFreeRepair);
                float bw = adReady ? (w - 12f) * 0.5f : w;
                if (UIKit.Button(new Rect(0f, y, bw, 90f), "REPAIR " + UIKit.Money(cost), BtnStyle.Primary, save.cash >= cost, 28))
                {
                    if (Economy.Repair(save)) { app.Toast("Car repaired"); app.SaveNow(); if (AudioManager.I != null) AudioManager.I.Play(Sfx.Confirm); }
                }
                if (adReady && UIKit.Button(new Rect(bw + 12f, y, bw, 90f), "FREE (WATCH AD)", BtnStyle.Gold, true, 26))
                {
                    app.RewardedAd(AdPlacement.RewardedFreeRepair, ok =>
                    {
                        if (ok) { Economy.RepairFree(app.Save); app.Toast("Car repaired"); app.SaveNow(); }
                    });
                }
                y += 106f;
            }
            else
            {
                y += 6f;
            }

            // ---- category tabs ----
            float cw = (w - 4f * 8f) / 5f;
            for (int i = 0; i < 5; i++)
            {
                if (UIKit.Button(new Rect(i * (cw + 8f), y, cw, 76f), CatNames[i], category == i ? BtnStyle.Primary : BtnStyle.Secondary, true, 21))
                {
                    category = i;
                    app.RefreshStage();
                }
            }
            y += 96f;

            if (category == 0) y = DrawParts(app, save, w, y);
            else y = DrawCosmetics(app, save, w, y, category - 1);

            UIKit.EndScroll();
            return scroll;
        }

        float DrawParts(AppRoot app, SaveData save, float w, float y)
        {
            for (int slot = 0; slot < Parts.SlotCount; slot++)
            {
                Rect r = new Rect(0f, y, w, 200f);
                UIKit.Tile(r, UIKit.PanelLight);
                int tier = save.TierOf(slot);
                UIKit.Label(new Rect(24f, y + 10f, 360f, 40f), Parts.SlotName(slot).ToUpper(), 30, UIKit.Text, TextAnchor.UpperLeft, true);
                for (int t = 0; t < Parts.MaxTier; t++)
                    UIKit.Round(new Rect(w - 250f - 150f + t * 52f, y + 16f, 44f, 14f), t < tier ? UIKit.Accent : new Color(1f, 1f, 1f, 0.14f));

                PartDef cur = tier > 0 ? Parts.Get(slot, tier) : null;
                UIKit.Label(new Rect(24f, y + 52f, w - 300f, 32f), cur != null ? "Installed: " + cur.name : "Installed: Stock", 24, UIKit.Dim, TextAnchor.UpperLeft);

                PartDef next = Parts.Get(slot, tier + 1);
                if (next != null)
                {
                    UIKit.Label(new Rect(24f, y + 90f, w - 300f, 32f), next.name, 28, UIKit.Text, TextAnchor.UpperLeft, true);
                    UIKit.Label(new Rect(24f, y + 124f, w - 300f, 70f), next.desc, 22, UIKit.Dim, TextAnchor.UpperLeft, false, true);
                    string why = Economy.PartBlockReason(save, next);
                    Rect b = new Rect(w - 262f, y + 80f, 238f, 84f);
                    if (UIKit.Button(b, UIKit.Money(next.price), BtnStyle.Gold, why == null, 30))
                    {
                        if (Economy.BuyPart(save, slot))
                        {
                            ratingStamp = -1;
                            app.Toast("Installed " + next.name);
                            app.SaveNow();
                            if (AudioManager.I != null) AudioManager.I.Play(Sfx.Coin);
                        }
                    }
                    if (why != null) UIKit.Label(new Rect(w - 280f, y + 166f, 270f, 28f), why, 20, UIKit.Bad, TextAnchor.UpperCenter);
                }
                else
                {
                    UIKit.Label(new Rect(24f, y + 100f, w - 300f, 40f), "Fully upgraded", 28, UIKit.Good, TextAnchor.UpperLeft, true);
                }
                y += 214f;
            }
            return y;
        }

        float DrawCosmetics(AppRoot app, SaveData save, float w, float y, int cat)
        {
            CosmeticItem[] list = Cosmetics.ListFor(cat);
            string equipped = Economy.EquippedId(save, cat);
            for (int i = 0; i < list.Length; i++)
            {
                CosmeticItem it = list[i];
                Rect r = new Rect(0f, y, w, 122f);
                bool unlocked = Cosmetics.IsUnlocked(it, save);
                bool isEq = it.id == equipped;
                UIKit.Tile(r, isEq ? new Color(0.16f, 0.2f, 0.42f, 0.98f) : UIKit.PanelLight);
                if (UIKit.Tap(r)) app.PreviewCosmetic(cat, it);

                if (cat != 3) UIKit.Circle(new Rect(24f, y + 26f, 70f, 70f), it.color);
                else UIKit.Round(new Rect(24f, y + 26f, 70f, 70f), it.id == "decal_none" ? new Color(1f, 1f, 1f, 0.1f) : it.color);
                UIKit.Label(new Rect(116f, y + 18f, w - 420f, 44f), it.name, 32, UIKit.Text, TextAnchor.UpperLeft, true);
                string sub = it.pack != null ? "Cosmetic pack" : (it.supporter ? "Supporter pack" : (it.price > 0 ? UIKit.Money(it.price) : "Free"));
                UIKit.Label(new Rect(116f, y + 64f, w - 420f, 34f), unlocked ? "Owned" : sub, 24, unlocked ? UIKit.Good : UIKit.Dim, TextAnchor.UpperLeft);

                Rect b = new Rect(w - 262f, y + 20f, 238f, 82f);
                if (unlocked)
                {
                    if (UIKit.Button(b, isEq ? "EQUIPPED" : "EQUIP", isEq ? BtnStyle.Secondary : BtnStyle.Primary, !isEq, 26))
                    {
                        Economy.Equip(save, cat, it);
                        app.SaveNow();
                        app.RefreshStage();
                    }
                }
                else if (it.pack != null || it.supporter)
                {
                    if (UIKit.Button(b, "IN SHOP", BtnStyle.Gold, true, 26)) app.OpenShop();
                }
                else
                {
                    if (UIKit.Button(b, UIKit.Money(it.price), BtnStyle.Gold, save.cash >= it.price, 28))
                    {
                        if (Economy.BuyCosmetic(save, it))
                        {
                            Economy.Equip(save, cat, it);
                            app.SaveNow();
                            app.RefreshStage();
                            app.Toast("Bought " + it.name);
                            if (AudioManager.I != null) AudioManager.I.Play(Sfx.Coin);
                        }
                    }
                }
                y += 134f;
            }
            return y;
        }
    }
}
