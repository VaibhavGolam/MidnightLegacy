using System;
using UnityEngine;

namespace MidnightLegacy
{
    /// <summary>
    /// Tiny typed event bus. Systems publish and subscribe without referencing each other.
    /// Always unsubscribe in OnDisable.
    /// </summary>
    public static class EventBus
    {
        static class Channel<T> where T : struct
        {
            public static Action<T> Handlers;
        }

        public static void Subscribe<T>(Action<T> handler) where T : struct
        {
            Channel<T>.Handlers += handler;
        }

        public static void Unsubscribe<T>(Action<T> handler) where T : struct
        {
            Channel<T>.Handlers -= handler;
        }

        public static void Raise<T>(T evt) where T : struct
        {
            Action<T> h = Channel<T>.Handlers;
            if (h != null) h(evt);
        }
    }

    // ---- Driving events ----

    /// <summary>The car hit the guard rail. side is -1 (left) or +1 (right).</summary>
    public struct WallHit
    {
        public float impactSpeed;
        public float side;
    }

    /// <summary>The world origin moved to keep floating point precision. Anything stored in world space must shift.</summary>
    public struct WorldRebased
    {
        public Vector3 shift;
    }

    /// <summary>The run was restarted.</summary>
    public struct RunReset { }

    /// <summary>The player car touched a traffic vehicle. impactSpeed is the closing speed in m/s.</summary>
    public struct TrafficHit
    {
        public float impactSpeed;
        public TrafficKind kind;
    }

    /// <summary>The player passed a traffic vehicle with less than about a metre to spare.</summary>
    public struct NearMiss
    {
        public float gap;
        public float relSpeed;
    }

    public enum StyleKind { NearMiss, CleanCorner, CleanSector, Drift, Overtake, Perfect }

    /// <summary>Points awarded by the style tracker. Used by the HUD for pop ups and by audio for a chime.</summary>
    public struct StylePoint
    {
        public StyleKind kind;
        public int points;
        public int combo;
    }

    public struct RaceStateChanged
    {
        public RaceState state;
    }

    public struct RaceFinished
    {
        public RaceResult result;
    }
}
