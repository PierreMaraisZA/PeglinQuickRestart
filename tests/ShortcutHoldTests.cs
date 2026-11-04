using System;
using System.Collections.Generic;
using System.Linq;
using PeglinQuickRestart.Core;
using Xunit;

namespace PeglinQuickRestart.Tests
{
    public class ShortcutHoldTests
    {
        // Stands in for Unity's KeyCode: None is the default value.
        private enum Key { None, X, LeftControl, JoystickButton8, Joystick1Button8 }

        private static Func<Key, bool> Down(params Key[] keys) => keys.Contains;

        [Fact]
        public void IsHeld_TrueWhenTheMainKeyIsDown()
        {
            Assert.True(ShortcutHold.IsHeld(Key.JoystickButton8, new Key[0], Down(Key.JoystickButton8)));
        }

        [Fact]
        public void IsHeld_FalseWhenTheMainKeyIsUp()
        {
            Assert.False(ShortcutHold.IsHeld(Key.JoystickButton8, new Key[0], Down()));
        }

        [Fact]
        public void IsHeld_IgnoresOtherKeysBeingDown()
        {
            // Unity reports a pad button as both JoystickButton8 and Joystick1Button8; that must not block the hold.
            Assert.True(ShortcutHold.IsHeld(Key.JoystickButton8, new Key[0], Down(Key.JoystickButton8, Key.Joystick1Button8, Key.X)));
        }

        [Fact]
        public void IsHeld_NeedsEveryModifier()
        {
            var modifiers = new[] { Key.LeftControl };
            Assert.False(ShortcutHold.IsHeld(Key.X, modifiers, Down(Key.X)));
            Assert.False(ShortcutHold.IsHeld(Key.X, modifiers, Down(Key.LeftControl)));
            Assert.True(ShortcutHold.IsHeld(Key.X, modifiers, Down(Key.X, Key.LeftControl)));
        }

        [Fact]
        public void IsHeld_PadMainKeyWithKeyboardModifier()
        {
            var modifiers = new[] { Key.LeftControl };
            Assert.True(ShortcutHold.IsHeld(Key.JoystickButton8, modifiers, Down(Key.JoystickButton8, Key.LeftControl)));
            Assert.False(ShortcutHold.IsHeld(Key.JoystickButton8, modifiers, Down(Key.JoystickButton8)));
        }

        [Fact]
        public void IsHeld_NoneModifierIsIgnored()
        {
            var modifiers = new[] { Key.None, Key.LeftControl };
            Assert.True(ShortcutHold.IsHeld(Key.JoystickButton8, modifiers, Down(Key.JoystickButton8, Key.LeftControl)));
        }

        [Fact]
        public void IsHeld_NoneIsNeverHeld()
        {
            var checkedKeys = new List<Key>();
            Func<Key, bool> anyDown = key => { checkedKeys.Add(key); return true; };
            Assert.False(ShortcutHold.IsHeld(Key.None, new Key[0], anyDown));
            Assert.Empty(checkedKeys);
        }
    }
}
