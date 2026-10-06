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
            Channel<T>.Handlers?.Invoke(evt);
        }
    }

    // ---- Events used from Phase 1 on ----

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
}
