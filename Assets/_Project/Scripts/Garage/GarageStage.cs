using UnityEngine;
using UnityEngine.Rendering;

namespace MidnightLegacy
{
    public enum StagePose { Menu, Hub, Garage }

    /// <summary>
    /// The 2D background for the menus: a side view of the garage with the player's car parked in it, a dust cover
    /// that comes off in the story, a slow idle sway, and a drag-to-pan with a little parallax.
    /// Everything is drawn with the same flat shape tools as the rest of the game. The scene is built in a frame where
    /// x runs along the car, z is up and y is depth, then the whole thing is turned so it faces the camera.
    /// Shown behind the main menu, the hub and the garage screen.
    /// </summary>
    public sealed class GarageStage : MonoBehaviour
    {
        const float CarDepth = 2.5f;       // the car sits this far in front of the back wall
        const float CamDistance = 30f;

        Camera cam;
        GameObject root;
        Transform view;                    // turned so local +y faces the camera, local z is up
        Transform room;
        GameObject carRoot, cover, underGo;
        NightLighting lighting;
        Mesh roomMesh, roomGlowMesh, carMesh, carGlowMesh, carUnderMesh, coverMesh, shadowMesh;
        Mesh bodySpriteMesh, wheelSpriteMesh;
        GameObject bodySpriteGo, wheelSpriteGo;
        bool paintedCar;                   // car_side_kairo is in the project
        readonly SpriteBatch spriteBatch = new SpriteBatch();

        // geometry of the painted side car (see car_side_kairo.png): metres, in the car's drawing frame
        const float ArtCarLength = 4.2f;
        const float ArtCarCentreX = 0.015f;
        const float ArtCarBottomZ = 0.04f;
        const float ArtRearWheelX = -1.185f, ArtFrontWheelX = 1.26f, ArtWheelZ = 0.32f, ArtWheelSize = 0.66f;
        readonly MeshBuilder carBuilder = new MeshBuilder();
        readonly MeshBuilder glowBuilder = new MeshBuilder();
        readonly MeshBuilder underBuilder = new MeshBuilder();

        StagePose pose = StagePose.Menu;
        float curCw = 0.84f, curCy = 0.58f;
        float userPan, lastUserTime = -10f;
        bool built;

        public bool Visible { get { return root != null && root.activeSelf; } }

        // ------------------------------------------------------------------ setup

        public void Build(Camera camera)
        {
            if (built) return;
            built = true;
            cam = camera;

            root = new GameObject("GarageStage");
            root.transform.SetParent(transform, false);
            // far away from the road world so the two never overlap
            root.transform.position = new Vector3(0f, -500f, 0f);

            view = new GameObject("View").transform;
            view.SetParent(root.transform, false);
            view.localRotation = Quaternion.Euler(-90f, 0f, 0f);

            BuildRoom();

            // car, its soft floor shadow and underglow
            carRoot = new GameObject("StageCar");
            carRoot.transform.SetParent(view, false);
            carRoot.transform.localPosition = new Vector3(0f, CarDepth, 0f);

            carMesh = new Mesh { name = "StageCar" };
            carGlowMesh = new Mesh { name = "StageCarGlow" };
            carUnderMesh = new Mesh { name = "StageCarUnder" };
            MakeMesh("CarBody", carRoot.transform, carMesh, MLMaterials.Opaque());
            MakeMesh("CarGlow", carRoot.transform, carGlowMesh, MLMaterials.Additive());
            underGo = MakeMesh("Underglow", carRoot.transform, carUnderMesh, MLMaterials.Additive());
            underGo.transform.localPosition = new Vector3(0f, 0.01f, 0.04f);
            underGo.transform.localScale = new Vector3(2.7f, 1f, 0.55f);
            underGo.SetActive(false);

            // painted side car: wheels sit just behind the body so they show through the open arches
            paintedCar = ArtLoader.Has("car_side_kairo") && ArtLoader.Sprite("car_side_kairo", 2, 0.5f) != null;
            if (paintedCar)
            {
                bodySpriteMesh = new Mesh { name = "StageCarSprite" };
                wheelSpriteMesh = new Mesh { name = "StageWheelSprite" };
                bodySpriteGo = MakeMesh("CarSprite", carRoot.transform, bodySpriteMesh, ArtLoader.Sprite("car_side_kairo", 2, 0.5f));
                wheelSpriteGo = MakeMesh("WheelSprites", carRoot.transform, wheelSpriteMesh, ArtLoader.Sprite("wheel_side_five", 1, 0.5f) ?? MLMaterials.Opaque());
                bodySpriteGo.SetActive(false);
                wheelSpriteGo.SetActive(false);
            }

            MeshBuilder mb = new MeshBuilder();
            mb.AddDisc(Vector3.zero, 1f, 20, Color.black, 0.62f, 0f);
            shadowMesh = mb.ToMesh("StageShadow");
            GameObject sh = MakeMesh("Shadow", view, shadowMesh, MLMaterials.Blend());
            sh.transform.localPosition = new Vector3(0f, CarDepth - 0.02f, 0.0f);
            sh.transform.localScale = new Vector3(2.4f, 1f, 0.17f);

            BuildCover();

            GameObject lg = new GameObject("StageLighting");
            lg.transform.SetParent(root.transform, false);
            lighting = lg.AddComponent<NightLighting>();
        }

        GameObject MakeMesh(string name, Transform parent, Mesh mesh, Material mat)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;
            return go;
        }

        public void Show(bool on)
        {
            if (root == null) return;
            root.SetActive(on);
            if (on)
            {
                cam.orthographic = true;
                cam.nearClipPlane = 1f;
                cam.farClipPlane = 60f;
                lighting.topDown = false;
                lighting.Init(null, cam);
                lighting.ApplyEnvironment(Environments.Garage, null);
                lighting.SetFogEnd(160f);
                // the flat shapes face the camera, so the "moon" shines from the camera's side
                lighting.moonDirection = new Vector3(0.25f, 0.35f, -1f);
            }
        }

        public void SetPose(StagePose p)
        {
            pose = p;
        }

        public void SetLook(CarLook look)
        {
            if (carMesh == null) return;
            carBuilder.Clear();
            glowBuilder.Clear();
            underBuilder.Clear();
            if (look == null) look = new CarLook();
            SideCar.Build(carBuilder, glowBuilder, underBuilder, look);

            // the painted Kairo replaces the flat body and wheels; the beam and underglow still come from SideCar
            bool painted = paintedCar && look.style == 0;
            if (paintedCar)
            {
                bodySpriteGo.SetActive(painted);
                wheelSpriteGo.SetActive(painted);
            }
            if (painted)
            {
                carBuilder.Clear();
                BuildPaintedCar(look);
            }
            carBuilder.Build(carMesh);
            glowBuilder.Build(carGlowMesh);
            underBuilder.Build(carUnderMesh);
            underGo.SetActive(underBuilder.VertexCount > 0);
        }

        void BuildPaintedCar(CarLook look)
        {
            float h = ArtCarLength / ArtLoader.Aspect("car_side_kairo");
            spriteBatch.Clear();
            spriteBatch.Add(ArtCarCentreX, ArtCarBottomZ + h * 0.5f, 0.05f, ArtCarLength * 0.5f, h * 0.5f, 0f, new Rect(0f, 0f, 1f, 1f), look.body);
            spriteBatch.Build(bodySpriteMesh);

            // wheels: a hue-only tint of the rim colour, so the silver art takes the rim colour picked in the garage
            Color rim = look.rim;
            float m = Mathf.Max(0.05f, Mathf.Max(rim.r, Mathf.Max(rim.g, rim.b)));
            Color tint = Color.Lerp(Color.white, new Color(rim.r / m, rim.g / m, rim.b / m, 1f), 0.85f);
            spriteBatch.Clear();
            Rect full = new Rect(0f, 0f, 1f, 1f);
            spriteBatch.Add(ArtRearWheelX, ArtWheelZ, -0.05f, ArtWheelSize * 0.5f, ArtWheelSize * 0.5f, 0f, full, tint);
            spriteBatch.Add(ArtFrontWheelX, ArtWheelZ, -0.05f, ArtWheelSize * 0.5f, ArtWheelSize * 0.5f, 0f, full, tint, true);
            spriteBatch.Build(wheelSpriteMesh);
            string wheelArt = look.rimStyle == 2 ? "wheel_side_mesh" : (look.rimStyle == 1 ? "wheel_side_five" : "wheel_side_bar");
            Material wm = ArtLoader.Sprite(wheelArt, 1, 0.5f);
            if (wm != null) wheelSpriteGo.GetComponent<MeshRenderer>().sharedMaterial = wm;
        }

        public void SetCover(bool covered)
        {
            if (cover == null) return;
            cover.SetActive(covered);
            if (carRoot != null) carRoot.SetActive(!covered);
        }

        /// <summary>Drag input from the garage screen, in degrees. It pans the view a little instead of spinning the car.</summary>
        public void AddOrbit(float degrees)
        {
            userPan = Mathf.Clamp(userPan + degrees * 0.012f, -1f, 1f);
            lastUserTime = Time.unscaledTime;
        }

        // ------------------------------------------------------------------ room

        static readonly Color Floor = new Color(0.17f, 0.17f, 0.19f);
        static readonly Color Wall = new Color(0.27f, 0.26f, 0.30f);
        static readonly Color WallDark = new Color(0.19f, 0.18f, 0.21f);
        static readonly Color Metal = new Color(0.36f, 0.37f, 0.40f);
        static readonly Color Yellow = new Color(0.85f, 0.70f, 0.15f);
        static readonly Color Neon = new Color(0.42f, 0.47f, 0.84f);
        static readonly Color Lamp = new Color(1f, 0.93f, 0.80f);
        static readonly Color Cloth = new Color(0.34f, 0.35f, 0.40f);

        static Color Mul(Color c, float f)
        {
            return new Color(Mathf.Clamp01(c.r * f), Mathf.Clamp01(c.g * f), Mathf.Clamp01(c.b * f), 1f);
        }

        /// <summary>
        /// The painted garage (garage_bg) as the back wall and floor. The picture is 16:9 and the stage is a tall portrait, so the
        /// picture covers the middle and its top and bottom rows are stretched (and faded) to fill the rest.
        /// Returns false when the art is not in the project, then the flat room is drawn instead.
        /// </summary>
        bool BuildPaintedRoom()
        {
            Material mat = ArtLoader.Sprite("garage_bg", -30, 0f);
            if (mat == null) return false;

            const float imgH = 4.6f;
            const float wallBaseFromTop = 0.655f;     // where the wall meets the floor in the picture
            const float floorZ = 0.95f;               // that line sits this far above the car's wheels (the car stands in front of the wall)
            float imgW = imgH * ArtLoader.Aspect("garage_bg");
            float zTop = floorZ + wallBaseFromTop * imgH;
            float zBottom = zTop - imgH;
            float far = 40f;
            Color dim = new Color(0.92f, 0.92f, 0.92f, 1f);

            SpriteBatch sb = new SpriteBatch();
            sb.Add(0f, (zTop + zBottom) * 0.5f, -5f, imgW * 0.5f, imgH * 0.5f, 0f, new Rect(0f, 0f, 1f, 1f), dim);
            // above and below: one flat colour taken from the picture's top and bottom edge, faded in so there is no seam
            Rect topTexel = new Rect(0.5f, 1f, 0f, 0f), bottomTexel = new Rect(0.5f, 0f, 0f, 0f);
            Color clear = new Color(0.6f, 0.6f, 0.6f, 0f), mid = new Color(0.6f, 0.6f, 0.6f, 1f), deep = new Color(0.3f, 0.3f, 0.3f, 1f);
            sb.AddFade(0f, zTop + 1.2f, -5f, far, 1.2f, topTexel, mid, clear);
            sb.Add(0f, zTop + 2.4f + far * 0.5f, -5f, far, far * 0.5f, 0f, topTexel, mid);
            sb.AddFade(0f, zBottom - 2f, -5f, far, 2f, bottomTexel, clear, deep);
            sb.Add(0f, zBottom - 4f - far * 0.5f, -5f, far, far * 0.5f, 0f, bottomTexel, deep);
            // either side of the picture (wide screens): the edge columns
            sb.Add(-(imgW * 0.5f + far * 0.5f), (zTop + zBottom) * 0.5f, -5f, far * 0.5f, imgH * 0.5f, 0f, new Rect(0f, 0f, 0f, 1f), dim);
            sb.Add(imgW * 0.5f + far * 0.5f, (zTop + zBottom) * 0.5f, -5f, far * 0.5f, imgH * 0.5f, 0f, new Rect(1f, 0f, 0f, 1f), dim);

            roomMesh = sb.ToMesh("GarageRoomPainted");
            roomGlowMesh = new Mesh { name = "GarageRoomGlow" };
            room = new GameObject("Room").transform;
            room.SetParent(view, false);
            MakeMesh("RoomPainted", room, roomMesh, mat);
            return true;
        }

        void BuildRoom()
        {
            if (BuildPaintedRoom()) return;
            MeshBuilder mb = new MeshBuilder();
            MeshBuilder gl = new MeshBuilder();

            // floor, wall and ceiling zone
            Flat.Rect(mb, 0f, -9f, 16f, 9f, 0.00f, Floor);
            Flat.Rect(mb, 0f, -0.25f, 16f, 0.25f, 0.005f, Mul(Floor, 1.25f));
            Flat.Rect(mb, 0f, 2.4f, 16f, 2.4f, 0.00f, Wall);
            Flat.Rect(mb, 0f, 8.4f, 16f, 3.6f, 0.00f, WallDark);

            // wall panels, dark lower band, rail and ceiling trim
            for (int i = 0; i < 7; i++) Flat.Rect(mb, 0f, 1.5f + i * 0.55f, 16f, 0.01f, 0.01f, new Color(0.22f, 0.21f, 0.25f));
            Flat.Rect(mb, 0f, 0.45f, 16f, 0.45f, 0.01f, WallDark);
            Flat.Rect(mb, 0f, 0.92f, 16f, 0.03f, 0.02f, Metal);
            Flat.Rect(mb, 0f, 4.8f, 16f, 0.04f, 0.02f, Metal);

            // floor markings
            for (int i = -9; i <= 9; i++) Flat.Rect(mb, i * 1.7f, -2.4f, 0.55f, 0.05f, 0.01f, Yellow);
            Flat.Rect(mb, 0f, -6.4f, 16f, 0.04f, 0.01f, Mul(Yellow, 0.7f));
            Flat.Ellipse(mb, 1.1f, -1.1f, 0.95f, 0.16f, 0.01f, Mul(Floor, 0.7f), 0f, 16);

            // neon ST triangle in the middle of the wall
            Vector2 top = new Vector2(0f, 3.15f), left = new Vector2(-0.62f, 2.05f), right = new Vector2(0.62f, 2.05f);
            NeonLine(mb, top, left); NeonLine(mb, left, right); NeonLine(mb, right, top);
            // S
            NeonLine(mb, new Vector2(-0.04f, 2.60f), new Vector2(-0.28f, 2.60f));
            NeonLine(mb, new Vector2(-0.28f, 2.60f), new Vector2(-0.28f, 2.45f));
            NeonLine(mb, new Vector2(-0.28f, 2.45f), new Vector2(-0.04f, 2.45f));
            NeonLine(mb, new Vector2(-0.04f, 2.45f), new Vector2(-0.04f, 2.30f));
            NeonLine(mb, new Vector2(-0.04f, 2.30f), new Vector2(-0.28f, 2.30f));
            // T
            NeonLine(mb, new Vector2(0.06f, 2.60f), new Vector2(0.30f, 2.60f));
            NeonLine(mb, new Vector2(0.18f, 2.60f), new Vector2(0.18f, 2.30f));
            Flat.Glow(gl, 0f, 2.55f, 1.7f, 0.05f, Neon, 0.30f, 20);

            // pendant lamps
            for (int s = -1; s <= 1; s += 2)
            {
                float x = 1.75f * s;
                Flat.Line(mb, new Vector3(x, 0f, 4.8f), new Vector3(x, 0f, 3.36f), 0.012f, 0.03f, Metal);
                Flat.Poly(mb, new[] { new Vector2(x - 0.07f, 3.36f), new Vector2(x + 0.07f, 3.36f), new Vector2(x + 0.34f, 3.08f), new Vector2(x - 0.34f, 3.08f) }, 0.04f, Mul(Metal, 0.75f));
                Flat.Ellipse(mb, x, 3.06f, 0.12f, 0.05f, 0.05f, Lamp, 1f, 10);
                Flat.Quad(gl, new Vector2(x - 0.12f, 3.05f), new Vector2(x + 0.12f, 3.05f), new Vector2(x + 1.3f, 0.5f), new Vector2(x - 1.3f, 0.5f), 0.04f, Lamp, 0.045f);
                Flat.Quad(gl, new Vector2(x - 0.12f, 3.05f), new Vector2(x + 0.12f, 3.05f), new Vector2(x + 0.7f, 1.2f), new Vector2(x - 0.7f, 1.2f), 0.05f, Lamp, 0.06f);
                Flat.Glow(gl, x, 3.05f, 0.85f, 0.06f, Lamp, 0.50f, 16);
            }

            // pegboard with tools, left
            Flat.Rect(mb, -1.7f, 2.3f, 0.64f, 0.57f, 0.03f, Metal);
            Flat.Rect(mb, -1.7f, 2.3f, 0.60f, 0.53f, 0.04f, new Color(0.20f, 0.20f, 0.23f));
            Flat.Line(mb, new Vector3(-2.05f, 0f, 2.72f), new Vector3(-2.05f, 0f, 2.00f), 0.022f, 0.05f, Mul(Metal, 1.3f));
            Flat.Circle(mb, -2.05f, 2.76f, 0.07f, 0.05f, Mul(Metal, 1.3f), 0f, 10);
            Flat.Circle(mb, -2.05f, 2.76f, 0.035f, 0.055f, new Color(0.20f, 0.20f, 0.23f), 0f, 8);
            Flat.Line(mb, new Vector3(-1.75f, 0f, 2.55f), new Vector3(-1.75f, 0f, 2.00f), 0.026f, 0.05f, new Color(0.45f, 0.32f, 0.20f));
            Flat.Rect(mb, -1.75f, 2.60f, 0.10f, 0.045f, 0.055f, Metal);
            Flat.Line(mb, new Vector3(-1.45f, 0f, 2.35f), new Vector3(-1.45f, 0f, 2.00f), 0.018f, 0.05f, Mul(Metal, 1.2f));
            Flat.Line(mb, new Vector3(-1.45f, 0f, 2.62f), new Vector3(-1.45f, 0f, 2.36f), 0.04f, 0.05f, new Color(0.75f, 0.18f, 0.14f));
            Flat.Line(mb, new Vector3(-1.2f, 0f, 2.7f), new Vector3(-1.2f, 0f, 2.2f), 0.02f, 0.05f, Mul(Metal, 1.1f));

            // shelf with boxes, right
            Flat.Rect(mb, 1.7f, 1.75f, 0.78f, 0.035f, 0.03f, Metal);
            Flat.Line(mb, new Vector3(1.15f, 0f, 1.74f), new Vector3(1.15f, 0f, 1.45f), 0.015f, 0.03f, Metal);
            Flat.Line(mb, new Vector3(2.25f, 0f, 1.74f), new Vector3(2.25f, 0f, 1.45f), 0.015f, 0.03f, Metal);
            Color card = new Color(0.46f, 0.38f, 0.29f);
            Flat.Rect(mb, 1.35f, 1.93f, 0.18f, 0.145f, 0.04f, card);
            Flat.Line(mb, new Vector3(1.35f, 0f, 2.075f), new Vector3(1.35f, 0f, 1.785f), 0.02f, 0.05f, Mul(card, 0.8f));
            Flat.Rect(mb, 1.78f, 1.88f, 0.21f, 0.10f, 0.04f, Mul(card, 1.12f));
            Flat.RoundRect(mb, 2.12f, 1.95f, 0.10f, 0.16f, 0.04f, 0.04f, new Color(0.18f, 0.32f, 0.55f), 0f, 0f, 3);
            Flat.Rect(mb, 2.12f, 1.98f, 0.075f, 0.03f, 0.05f, new Color(0.9f, 0.9f, 0.9f));

            // framed photo above the shelf (a white coupe)
            Flat.Rect(mb, 1.7f, 2.65f, 0.40f, 0.31f, 0.03f, Metal);
            Flat.Rect(mb, 1.7f, 2.65f, 0.36f, 0.27f, 0.04f, new Color(0.11f, 0.13f, 0.21f));
            Vector2[] coupe = SideCar.CoupeOutline();
            Vector2[] mini = new Vector2[coupe.Length];
            for (int i = 0; i < coupe.Length; i++) mini[i] = new Vector2(1.7f + coupe[i].x * 0.14f, 2.54f + coupe[i].y * 0.14f);
            Flat.Poly(mb, mini, 0.05f, new Color(0.85f, 0.85f, 0.83f));
            Flat.Rect(mb, 1.7f, 2.43f, 0.24f, 0.008f, 0.05f, new Color(0.35f, 0.38f, 0.5f));

            // things further out, for wider screens
            Flat.Rect(mb, -6.2f, 1.5f, 2.3f, 1.55f, 0.03f, Metal);
            for (int i = 0; i < 10; i++) Flat.Rect(mb, -6.2f, 0.17f + i * 0.30f, 2.2f, 0.135f, 0.04f, i % 2 == 0 ? Metal : Mul(Metal, 0.82f));
            Flat.Rect(mb, -3.9f, 0.55f, 0.52f, 0.55f, 0.04f, new Color(0.70f, 0.16f, 0.14f));
            for (int d = 0; d < 4; d++)
            {
                Flat.Rect(mb, -3.9f, 0.18f + d * 0.26f, 0.44f, 0.02f, 0.05f, Mul(new Color(0.70f, 0.16f, 0.14f), 0.6f));
                Flat.Rect(mb, -3.9f, 0.28f + d * 0.26f, 0.12f, 0.018f, 0.05f, Metal);
            }
            for (int i = 0; i < 4; i++) Flat.RoundRect(mb, 3.9f, 0.11f + i * 0.23f, 0.38f, 0.105f, 0.08f, 0.04f, new Color(0.06f, 0.06f, 0.07f), 0f, 0f, 3);
            Flat.Rect(mb, 5.0f, 0.45f, 0.30f, 0.45f, 0.04f, new Color(0.18f, 0.32f, 0.55f));
            Flat.Rect(mb, 5.0f, 0.45f, 0.30f, 0.02f, 0.05f, Metal);
            Flat.Rect(mb, 5.65f, 0.45f, 0.30f, 0.45f, 0.04f, new Color(0.55f, 0.22f, 0.15f));
            Flat.Rect(mb, 5.65f, 0.45f, 0.30f, 0.02f, 0.05f, Metal);

            roomMesh = mb.ToMesh("GarageRoom");
            roomGlowMesh = gl.ToMesh("GarageRoomGlow");
            room = new GameObject("Room").transform;
            room.SetParent(view, false);
            MakeMesh("RoomShapes", room, roomMesh, MLMaterials.Opaque());
            MakeMesh("RoomGlow", room, roomGlowMesh, MLMaterials.Additive());
        }

        static void NeonLine(MeshBuilder mb, Vector2 a, Vector2 b)
        {
            Flat.Line(mb, new Vector3(a.x, 0f, a.y), new Vector3(b.x, 0f, b.y), 0.04f, 0.06f, Neon, 1f);
        }

        void BuildCover()
        {
            Material paintedCover = ArtLoader.Sprite("car_cover", 3, 0.5f);
            if (paintedCover != null)
            {
                float cl = 4.55f;
                float ch = cl / ArtLoader.Aspect("car_cover");
                SpriteBatch cb = new SpriteBatch();
                cb.Add(0f, ch * 0.5f - 0.02f, 0f, cl * 0.5f, ch * 0.5f, 0f, new Rect(0f, 0f, 1f, 1f), Color.white);
                coverMesh = cb.ToMesh("CarCoverPainted");
                cover = MakeMesh("Cover", view, coverMesh, paintedCover);
                cover.transform.localPosition = new Vector3(0f, CarDepth + 0.45f, 0f);
                return;
            }
            MeshBuilder mb = new MeshBuilder();
            Vector2[] pts = Flat.Pts(-2.2f, 0.0f, -2.18f, 0.55f, -2.1f, 0.95f, -1.5f, 1.05f, -0.8f, 1.38f, -0.1f, 1.5f, 0.6f, 1.42f,
                1.1f, 1.08f, 1.8f, 0.92f, 2.18f, 0.68f, 2.24f, 0.3f, 2.2f, 0.0f);
            Flat.Poly(mb, Flat.Grow(pts, 0.04f), 0.00f, Mul(Cloth, 0.55f));
            Flat.Poly(mb, pts, 0.02f, Cloth);
            Color fold = Mul(Cloth, 0.78f);
            Flat.Line(mb, new Vector3(-1.6f, 0f, 0.95f), new Vector3(-1.65f, 0f, 0.05f), 0.028f, 0.04f, fold);
            Flat.Line(mb, new Vector3(-0.9f, 0f, 1.25f), new Vector3(-0.95f, 0f, 0.05f), 0.035f, 0.04f, fold);
            Flat.Line(mb, new Vector3(-0.1f, 0f, 1.35f), new Vector3(-0.15f, 0f, 0.05f), 0.03f, 0.04f, fold);
            Flat.Line(mb, new Vector3(0.7f, 0f, 1.25f), new Vector3(0.75f, 0f, 0.05f), 0.035f, 0.04f, fold);
            Flat.Line(mb, new Vector3(1.5f, 0f, 0.85f), new Vector3(1.55f, 0f, 0.05f), 0.028f, 0.04f, fold);
            Flat.Line(mb, new Vector3(-2.15f, 0f, 0.07f), new Vector3(2.15f, 0f, 0.07f), 0.02f, 0.05f, Mul(Cloth, 1.2f));
            Flat.Line(mb, new Vector3(-0.6f, 0f, 1.36f), new Vector3(0.4f, 0f, 1.40f), 0.025f, 0.05f, Mul(Cloth, 1.25f));

            coverMesh = mb.ToMesh("CarCover");
            cover = MakeMesh("Cover", view, coverMesh, MLMaterials.Opaque());
            cover.transform.localPosition = new Vector3(0f, CarDepth + 0.45f, 0f);
        }

        // ------------------------------------------------------------------ camera

        void LateUpdate()
        {
            if (!Visible || cam == null) return;
            float dt = Time.unscaledDeltaTime;
            float aspect = Mathf.Max(0.3f, cam.aspect);

            // how wide the car is on screen (share of the screen width) and where its middle sits (share of the height, from the bottom)
            float tcw, tcy;
            switch (pose)
            {
                case StagePose.Garage: tcw = 0.90f; tcy = 0.735f; break;
                case StagePose.Hub: tcw = 0.80f; tcy = 0.775f; break;
                default: tcw = 0.84f; tcy = 0.58f; break;
            }
            float k = 1f - Mathf.Exp(-3.5f * dt);
            curCw += (tcw - curCw) * k;
            curCy += (tcy - curCy) * k;

            if (Time.unscaledTime - lastUserTime > 2.5f) userPan *= Mathf.Exp(-1.5f * dt);
            float pan = userPan + Mathf.Sin(Time.unscaledTime * 0.4f) * 0.12f;

            float worldH = Mathf.Max(5.5f, SideCar.FrameLength / curCw / aspect);
            float centreZ = SideCar.MidHeight + (0.5f - curCy) * worldH;

            cam.orthographic = true;
            cam.orthographicSize = worldH * 0.5f;
            cam.nearClipPlane = 1f;
            cam.farClipPlane = 60f;
            Vector3 o = root.transform.position;
            cam.transform.SetPositionAndRotation(new Vector3(o.x + pan, o.y + centreZ, o.z - CamDistance), Quaternion.identity);

            // the wall moves slower than the car, which gives a little depth
            room.localPosition = new Vector3(pan * 0.55f, 0f, 0f);
        }

        void OnDestroy()
        {
            Mesh[] all = { roomMesh, roomGlowMesh, carMesh, carGlowMesh, carUnderMesh, coverMesh, shadowMesh, bodySpriteMesh, wheelSpriteMesh };
            for (int i = 0; i < all.Length; i++) if (all[i] != null) Destroy(all[i]);
        }
    }
}
