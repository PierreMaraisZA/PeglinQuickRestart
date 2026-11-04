using System;
using System.Collections.Generic;
using System.Linq;

namespace PeglinQuickRestart.Core
{
    /// <summary>
    /// Whether a shortcut is held: its main key and all its modifiers are down. Other keys being down doesn't matter,
    /// unlike BepInEx's KeyboardShortcut.IsPressed. Generic over the key type, with the key check passed in, so it has
    /// no Unity/BepInEx dependency and can be unit tested.
    /// </summary>
    public static class ShortcutHold
    {
        /// <summary>The key type's default value (KeyCode.None) means unbound and is never held.</summary>
        public static bool IsHeld<TKey>(TKey mainKey, IEnumerable<TKey> modifiers, Func<TKey, bool> isKeyHeld) where TKey : struct
        {
            if (EqualityComparer<TKey>.Default.Equals(mainKey, default))
            {
                return false;
            }
            // A None entry in the modifiers list means "no modifier", not an impossible key to hold.
            return isKeyHeld(mainKey)
                && modifiers.Where(m => !EqualityComparer<TKey>.Default.Equals(m, default)).All(isKeyHeld);
        }
    }
}
