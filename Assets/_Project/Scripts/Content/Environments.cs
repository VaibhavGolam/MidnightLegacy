using UnityEngine;

namespace MidnightLegacy
{
    public enum Scenery { Autumn, Pine, Coast, City }

    /// <summary>A location: road layout style plus the palette and scenery that dress it.</summary>
    public sealed class ThemeDef
    {
        public string id;
        public string name;
        public Scenery scenery;
        public Color asphalt, ground, groundFar, shoulder, trunk, rail;
        public Color[] foliage;
        public float treeDensity = 1f;
        public float hillAmplitude = 5f;
        public Vector2 straightLength = new Vector2(20f, 90f);
        public Vector2 curveLength = new Vector2(45f, 120f);
        public float minRadius = 28f;
        public float maxRadius = 150f;
        public float sBendChance = 0.35f;
        public float roadHalfWidth = 3.4f;
    }

    /// <summary>Lighting for a time of day. Colours are sRGB.</summary>
    public sealed class TimeDef
    {
        public string id;
        public string name;
        public Color ambient, moon, fog;
        public Vector3 moonDirection;
        public float fogStart = 35f;
        public float fogEndMultiplier = 1f;
        public float headIntensity = 2.2f;
        public float headRange = 42f;
    }

    public sealed class WeatherDef
    {
        public string id;
        public string name;
        public bool rain;
        public bool wet;
        public float fogMultiplier = 1f;
        public float lightMultiplier = 1f;
    }

    /// <summary>Catalogue of locations, times of day and weather. Races reference these by id.</summary>
    public static class Environments
    {
        static Color C(float r, float g, float b) { return new Color(r, g, b); }

        public static readonly ThemeDef[] Themes =
        {
            new ThemeDef
            {
                id = "forest", name = "Harbor Road", scenery = Scenery.Autumn,
                asphalt = C(0.17f, 0.18f, 0.22f), ground = C(0.13f, 0.17f, 0.08f), groundFar = C(0.09f, 0.12f, 0.07f),
                shoulder = C(0.20f, 0.19f, 0.17f), trunk = C(0.82f, 0.80f, 0.72f), rail = C(0.42f, 0.44f, 0.48f),
                foliage = new[]
                {
                    C(0.92f, 0.46f, 0.10f), C(0.70f, 0.14f, 0.10f), C(0.95f, 0.74f, 0.20f),
                    C(0.80f, 0.32f, 0.08f), C(0.55f, 0.20f, 0.10f), C(0.35f, 0.42f, 0.14f)
                },
                treeDensity = 1f, hillAmplitude = 5f
            },
            new ThemeDef
            {
                id = "pass", name = "Kairo Pass", scenery = Scenery.Pine,
                asphalt = C(0.15f, 0.16f, 0.20f), ground = C(0.10f, 0.14f, 0.10f), groundFar = C(0.07f, 0.10f, 0.08f),
                shoulder = C(0.22f, 0.22f, 0.23f), trunk = C(0.25f, 0.17f, 0.12f), rail = C(0.50f, 0.52f, 0.56f),
                foliage = new[]
                {
                    C(0.10f, 0.30f, 0.18f), C(0.08f, 0.25f, 0.15f), C(0.13f, 0.34f, 0.20f), C(0.16f, 0.30f, 0.14f)
                },
                treeDensity = 1.1f, hillAmplitude = 12f,
                straightLength = new Vector2(15f, 60f), curveLength = new Vector2(50f, 130f),
                minRadius = 22f, maxRadius = 110f, sBendChance = 0.5f
            },
            new ThemeDef
            {
                id = "coast", name = "Coast Highway", scenery = Scenery.Coast,
                asphalt = C(0.20f, 0.21f, 0.25f), ground = C(0.33f, 0.29f, 0.18f), groundFar = C(0.10f, 0.24f, 0.40f),
                shoulder = C(0.28f, 0.27f, 0.24f), trunk = C(0.35f, 0.26f, 0.16f), rail = C(0.75f, 0.76f, 0.78f),
                foliage = new[] { C(0.35f, 0.42f, 0.18f), C(0.45f, 0.38f, 0.16f), C(0.25f, 0.36f, 0.20f) },
                treeDensity = 0.5f, hillAmplitude = 3f,
                straightLength = new Vector2(60f, 160f), curveLength = new Vector2(60f, 160f),
                minRadius = 45f, maxRadius = 220f, sBendChance = 0.25f
            },
            new ThemeDef
            {
                id = "city", name = "Outskirts", scenery = Scenery.City,
                asphalt = C(0.12f, 0.12f, 0.15f), ground = C(0.16f, 0.16f, 0.19f), groundFar = C(0.10f, 0.10f, 0.13f),
                shoulder = C(0.30f, 0.30f, 0.33f), trunk = C(0.3f, 0.3f, 0.3f), rail = C(0.40f, 0.42f, 0.46f),
                foliage = new[] { C(0.18f, 0.19f, 0.25f), C(0.24f, 0.20f, 0.20f), C(0.15f, 0.20f, 0.24f), C(0.22f, 0.22f, 0.26f) },
                treeDensity = 1f, hillAmplitude = 1.5f,
                straightLength = new Vector2(40f, 140f), curveLength = new Vector2(40f, 100f),
                minRadius = 40f, maxRadius = 180f, sBendChance = 0.3f
            }
        };

        public static readonly TimeDef[] Times =
        {
            new TimeDef
            {
                id = "night", name = "Night",
                ambient = C(0.30f, 0.30f, 0.46f), moon = C(0.30f, 0.34f, 0.55f), fog = C(0.07f, 0.08f, 0.17f),
                moonDirection = new Vector3(-0.35f, 0.8f, 0.45f), fogStart = 35f, fogEndMultiplier = 1f, headIntensity = 2.2f
            },
            new TimeDef
            {
                id = "midnight", name = "Midnight",
                ambient = C(0.20f, 0.20f, 0.34f), moon = C(0.22f, 0.26f, 0.46f), fog = C(0.035f, 0.04f, 0.10f),
                moonDirection = new Vector3(0.3f, 0.8f, 0.4f), fogStart = 30f, fogEndMultiplier = 0.9f, headIntensity = 2.7f, headRange = 48f
            },
            new TimeDef
            {
                id = "dusk", name = "Dusk",
                ambient = C(0.55f, 0.42f, 0.46f), moon = C(0.85f, 0.50f, 0.32f), fog = C(0.42f, 0.24f, 0.28f),
                moonDirection = new Vector3(-0.8f, 0.35f, 0.5f), fogStart = 50f, fogEndMultiplier = 1.1f, headIntensity = 1.1f
            },
            new TimeDef
            {
                id = "dawn", name = "Dawn",
                ambient = C(0.48f, 0.50f, 0.64f), moon = C(0.80f, 0.64f, 0.60f), fog = C(0.52f, 0.46f, 0.55f),
                moonDirection = new Vector3(0.7f, 0.4f, 0.5f), fogStart = 50f, fogEndMultiplier = 1.0f, headIntensity = 0.8f
            }
        };

        public static readonly TimeDef Garage = new TimeDef
        {
            id = "garage", name = "Garage",
            ambient = C(0.66f, 0.60f, 0.56f), moon = C(0.55f, 0.50f, 0.46f), fog = C(0.05f, 0.05f, 0.07f),
            moonDirection = new Vector3(-0.3f, 0.9f, 0.3f), fogStart = 40f, fogEndMultiplier = 0.5f, headIntensity = 0f
        };

        public static readonly WeatherDef[] Weathers =
        {
            new WeatherDef { id = "clear", name = "Clear" },
            new WeatherDef { id = "rain", name = "Rain", rain = true, wet = true, fogMultiplier = 0.8f, lightMultiplier = 0.9f },
            new WeatherDef { id = "fog", name = "Fog", fogMultiplier = 0.45f },
            new WeatherDef { id = "storm", name = "Storm", rain = true, wet = true, fogMultiplier = 0.6f, lightMultiplier = 0.8f }
        };

        public static ThemeDef Theme(string id)
        {
            for (int i = 0; i < Themes.Length; i++) if (Themes[i].id == id) return Themes[i];
            return Themes[0];
        }

        public static TimeDef Time(string id)
        {
            for (int i = 0; i < Times.Length; i++) if (Times[i].id == id) return Times[i];
            return Times[0];
        }

        public static WeatherDef Weather(string id)
        {
            for (int i = 0; i < Weathers.Length; i++) if (Weathers[i].id == id) return Weathers[i];
            return Weathers[0];
        }
    }
}
