using UnityEngine;

namespace MidnightLegacy
{
    /// <summary>
    /// Garage tab: car rating, repairs, parts and cosmetics. Everything here is bought with in-game cash.
    /// Part pictures are optional painted art named part_&lt;slot name&gt; (for example part_engine); without them a drawn tile is used.
    /// </summary>
    public sealed class GarageTab
    {
        int category;     // 0 parts, 1 paint, 2 wheels, 3 decals, 4 spoiler
        CarHandlingProfile baseProfile, effective;
        CarRating rating;
        int ratingStamp = -1;

        static readonly string[] CatNames = { "PARTS", "PAINT", "WHEELS", "DECALS", "SPOILER" };
        static readonly Color Blue = new Color(0.50f, 0.62f, 1f);

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
            float listH = category == 0 ? Parts.SlotCount * 236f : Cosmetics.ListFor(category - 1).Length * 148f;
            float content = 640f + 130f + listH + 40f;
            scroll = UIKit.BeginScroll(view, scroll, content);
            float y = -scroll.y;

            // ---- name and rating ----
            UIKit.Label(new Rect(4f, y, w * 0.62f, 70f), "KAIRO HACHI-R", 52, UIKit.Text, TextAnchor.MiddleLeft, true);
            UIKit.Label(new Rect(w * 0.5f, y, w * 0.5f, 70f), "RATING " + rating.Overall, 44, UIKit.Gold, TextAnchor.MiddleRight, true);
            y += 86f;

            // ---- stat bars, two columns ----
            string[] namesL = { "Power", "Braking", "Nitro" };
            float[] valsL = { rating.power, rating.braking, rating.nitro };
            string[] namesR = { "Grip", "Stability", "Toughness" };
            float[] valsR = { rating.grip, rating.stability, rating.toughness };
            float colW = (w - 40f) * 0.5f;
            for (int i = 0; i < 3; i++)
            {
                float cy = y + i * 56f;
                StatRow(new Rect(0f, cy, colW, 44f), namesL[i], valsL[i]);
                StatRow(new Rect(colW + 40f, cy, colW, 44f), namesR[i], valsR[i]);
            }
            y += 3 * 56f + 18f;

            // ---- condition ----
            float dmg = Mathf.Clamp01(save.damage);
            Color cond = dmg > 0.5f ? UIKit.Bad : UIKit.Good;
            UIKit.Label(new Rect(4f, y, 220f, 44f), "CONDITION", 26, UIKit.Dim, TextAnchor.MiddleLeft, true);
            UIKit.SegBar(new Rect(230f, y + 11f, w - 230f - 130f, 22f), 1f - dmg, 20, cond, new Color(1f, 1f, 1f, 0.10f));
            UIKit.Label(new Rect(w - 120f, y, 120f, 44f), Mathf.RoundToInt((1f - dmg) * 100f) + "%", 34, cond, TextAnchor.MiddleRight, true);
            y += 62f;

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
                y += 108f;
            }
            else
            {
                y += 8f;
            }

            // ---- category pills ----
            float gap = 10f;
            float cw = (w - 4f * gap) / 5f;
            for (int i = 0; i < 5; i++)
            {
                Rect pr = new Rect(i * (cw + gap), y, cw, 74f);
                bool sel = category == i;
                if (sel)
                {
                    UIKit.Glow(pr, UIKit.EdgeBlue, 0.45f);
                    UIKit.Round(pr, new Color(0.20f, 0.40f, 0.96f, 1f));
                }
                else
                {
                    UIKit.Round(pr, new Color(0.05f, 0.07f, 0.16f, 0.95f));
                    UIKit.Outline(pr, new Color(0.28f, 0.45f, 1f, 0.45f));
                }
                UIKit.Label(pr, CatNames[i], 22, sel ? Color.white : UIKit.Dim, TextAnchor.MiddleCenter, true);
                if (UIKit.Tap(pr))
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

        /// <summary>One stat: label on the left, ten blocks on the right.</summary>
        static void StatRow(Rect r, string name, float v)
        {
            UIKit.Label(new Rect(r.x, r.y, 190f, r.height), name, 26, UIKit.Dim, TextAnchor.MiddleLeft, true);
            UIKit.SegBar(new Rect(r.x + 196f, r.y + 11f, r.width - 196f, 22f), Mathf.Clamp01(v / 100f), 10, UIKit.EdgeBlue, new Color(1f, 1f, 1f, 0.10f));
        }

        static string PartArtName(int slot)
        {
            return "part_" + Parts.SlotName(slot).ToLower().Replace(' ', '_');
        }

        /// <summary>The picture box on the left of a part row: painted art if present, otherwise a dark tile with a gear.</summary>
        static void PartPicture(Rect r, int slot)
        {
            UIKit.Round(r, new Color(0.03f, 0.04f, 0.10f, 1f));
            Texture2D t = UIKit.Art(PartArtName(slot));
            if (t != null) UIKit.Cover(r, t, t.width / Mathf.Max(1f, (float)t.height), Color.white);
            else UIKit.Round(new Rect(r.x + r.width * 0.2f, r.y + r.height * 0.2f, r.width * 0.6f, r.height * 0.6f), new Color(0.12f, 0.16f, 0.34f, 1f));
            if (t == null) UIIcons.Draw("gear", new Rect(r.x + r.width * 0.28f, r.y + r.height * 0.28f, r.width * 0.44f, r.height * 0.44f), UIKit.EdgeBlue);
            UIKit.Outline(r, new Color(0.28f, 0.45f, 1f, 0.5f));
        }

        float DrawParts(AppRoot app, SaveData save, float w, float y)
        {
            for (int slot = 0; slot < Parts.SlotCount; slot++)
            {
                Rect r = new Rect(0f, y, w, 222f);
                UIKit.Tile(r, UIKit.PanelLight);
                int tier = save.TierOf(slot);
                PartPicture(new Rect(18f, y + 18f, 186f, 186f), slot);

                float tx = 228f;
                float textW = w - tx - 290f;
                UIKit.Label(new Rect(tx, y + 12f, textW, 44f), Parts.SlotName(slot).ToUpper(), 34, UIKit.Text, TextAnchor.UpperLeft, true);
                for (int t = 0; t < Parts.MaxTier; t++)
                    UIKit.Round(new Rect(w - 28f - (Parts.MaxTier - t) * 40f, y + 22f, 30f, 30f), t < tier ? UIKit.EdgeBlue : new Color(1f, 1f, 1f, 0.14f));

                PartDef cur = tier > 0 ? Parts.Get(slot, tier) : null;
                UIKit.Label(new Rect(tx, y + 58f, textW + 100f, 32f), cur != null ? "Installed: " + cur.name : "Installed: Stock", 24, UIKit.Dim, TextAnchor.UpperLeft);

                PartDef next = Parts.Get(slot, tier + 1);
                if (next != null)
                {
                    UIKit.Label(new Rect(tx, y + 96f, textW, 34f), next.name, 28, UIKit.Text, TextAnchor.UpperLeft, true);
                    UIKit.Label(new Rect(tx, y + 132f, textW, 80f), next.desc, 22, UIKit.Dim, TextAnchor.UpperLeft, false, true);
                    string why = Economy.PartBlockReason(save, next);
                    Rect b = new Rect(w - 266f, y + 92f, 242f, 76f);
                    PriceButton(b, next.price, why == null, () =>
                    {
                        if (Economy.BuyPart(save, slot))
                        {
                            ratingStamp = -1;
                            app.Toast("Installed " + next.name);
                            app.SaveNow();
                            if (AudioManager.I != null) AudioManager.I.Play(Sfx.Coin);
                        }
                    });
                    if (why != null) UIKit.Label(new Rect(w - 290f, y + 172f, 280f, 30f), why, 20, UIKit.Bad, TextAnchor.UpperCenter);
                }
                else
                {
                    UIKit.Label(new Rect(tx, y + 110f, textW, 40f), "Fully upgraded", 30, UIKit.Good, TextAnchor.UpperLeft, true);
                }
                y += 236f;
            }
            return y;
        }

        /// <summary>Gold price button with a coin on the left. Runs onBuy when pressed.</summary>
        static void PriceButton(Rect b, int price, bool enabled, System.Action onBuy)
        {
            string txt = price.ToString("N0");
            if (UIKit.Button(b, "      " + txt, BtnStyle.Gold, enabled, 32) && onBuy != null) onBuy();
            float tw = UIKit.TextWidth("      " + txt, 32, true);
            UIKit.Coin(new Rect(b.center.x - tw * 0.5f + 6f, b.center.y - 20f, 40f, 40f));
        }

        float DrawCosmetics(AppRoot app, SaveData save, float w, float y, int cat)
        {
            CosmeticItem[] list = Cosmetics.ListFor(cat);
            string equipped = Economy.EquippedId(save, cat);
            for (int i = 0; i < list.Length; i++)
            {
                CosmeticItem it = list[i];
                Rect r = new Rect(0f, y, w, 134f);
                bool unlocked = Cosmetics.IsUnlocked(it, save);
                bool isEq = it.id == equipped;
                UIKit.Tile(r, isEq ? new Color(0.14f, 0.20f, 0.46f, 0.98f) : UIKit.PanelLight);
                if (isEq) UIKit.Outline(r, UIKit.EdgeBlue);
                if (UIKit.Tap(r)) app.PreviewCosmetic(cat, it);

                Rect sw = new Rect(22f, y + 24f, 86f, 86f);
                if (cat != 3) { UIKit.Circle(new Rect(sw.x - 4f, sw.y - 4f, sw.width + 8f, sw.height + 8f), new Color(1f, 1f, 1f, 0.18f)); UIKit.Circle(sw, it.color); }
                else UIKit.Round(sw, it.id == "decal_none" ? new Color(1f, 1f, 1f, 0.1f) : it.color);
                UIKit.Label(new Rect(130f, y + 22f, w - 430f, 46f), it.name, 34, UIKit.Text, TextAnchor.UpperLeft, true);
                string sub = it.pack != null ? "Cosmetic pack" : (it.supporter ? "Supporter pack" : (it.price > 0 ? UIKit.Money(it.price) : "Free"));
                UIKit.Label(new Rect(130f, y + 72f, w - 430f, 36f), unlocked ? "Owned" : sub, 26, unlocked ? UIKit.Good : UIKit.Dim, TextAnchor.UpperLeft);

                Rect b = new Rect(w - 268f, y + 26f, 244f, 82f);
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
                    PriceButton(b, it.price, save.cash >= it.price, () =>
                    {
                        if (Economy.BuyCosmetic(save, it))
                        {
                            Economy.Equip(save, cat, it);
                            app.SaveNow();
                            app.RefreshStage();
                            app.Toast("Bought " + it.name);
                            if (AudioManager.I != null) AudioManager.I.Play(Sfx.Coin);
                        }
                    });
                }
                y += 148f;
            }
            return y;
        }
    }
}
