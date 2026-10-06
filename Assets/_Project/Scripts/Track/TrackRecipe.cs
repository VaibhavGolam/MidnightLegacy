using UnityEngine;

namespace MidnightLegacy
{
    /// <summary>
    /// Seeded recipe for a road. Same seed = same road, so races are repeatable.
    /// An endless run uses the same recipe and simply never ends.
    /// </summary>
    [CreateAssetMenu(menuName = "Midnight Legacy/Track Recipe", fileName = "ForestRoad_Recipe")]
    public sealed class TrackRecipe : ScriptableObject
    {
        [Header("Seed")]
        public int seed = 1337;

        [Header("Cross section (metres)")]
        public float roadHalfWidth = 3.4f;
        [Tooltip("Distance from the road centre where the guard rail stops the car.")]
        public float wallOffset = 3.85f;
        [Tooltip("Guard rail sits this far outside the road edge.")]
        public float railOffset = 1.3f;

        [Header("Layout")]
        public float firstStraight = 90f;
        public Vector2 straightLength = new Vector2(20f, 90f);
        public Vector2 curveLength = new Vector2(45f, 120f);
        [Tooltip("Tightest corner radius at full difficulty.")]
        public float minRadius = 28f;
        [Tooltip("Widest corner radius.")]
        public float maxRadius = 150f;
        [Tooltip("Corners are gentle until this distance, then ramp to the tightest radius.")]
        public float difficultyRampDistance = 1800f;
        [Range(0f, 1f)] public float sBendChance = 0.35f;

        [Header("Hills")]
        public float hillAmplitude = 5f;

        [Header("Scenery")]
        [Range(0f, 2f)] public float treeDensity = 1f;

        public static TrackRecipe CreateDefault()
        {
            TrackRecipe r = CreateInstance<TrackRecipe>();
            r.name = "RuntimeDefaultRecipe";
            return r;
        }
    }
}
