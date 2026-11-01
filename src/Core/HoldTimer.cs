using System;

namespace PeglinQuickRestart.Core
{
    /// <summary>
    /// Fires once when a key has been held for long enough. Letting go resets it, and it won't fire again until the
    /// key is released and held again. Pure C# (no Unity/BepInEx) so it can be unit tested.
    /// </summary>
    public sealed class HoldTimer
    {
        private bool _fired;

        public HoldTimer(float holdSeconds)
        {
            HoldSeconds = Math.Max(0f, holdSeconds);
        }

        public float HoldSeconds { get; }

        /// <summary>How long the key has been held, in seconds. Zero while it isn't held.</summary>
        public float Held { get; private set; }

        /// <summary>From 0 (not held) to 1 (held long enough).</summary>
        public float Progress => _fired ? 1f : HoldSeconds <= 0f ? 0f : Math.Min(1f, Held / HoldSeconds);

        /// <summary>True while the key is held but the hold hasn't completed yet.</summary>
        public bool Charging => Held > 0f && !_fired;

        /// <summary>Call once a frame. Returns true on the one frame the hold completes.</summary>
        public bool Update(bool keyHeld, float deltaTime)
        {
            if (!keyHeld)
            {
                Held = 0f;
                _fired = false;
                return false;
            }
            if (_fired)
            {
                return false;
            }
            Held += Math.Max(0f, deltaTime);
            if (Held >= HoldSeconds)
            {
                _fired = true;
                return true;
            }
            return false;
        }
    }
}
