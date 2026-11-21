using System;
using System.Collections.Generic;

namespace PeglinQuickRestart.Core
{
    /// <summary>
    /// Callbacks registered by other mods, one per mod (keyed by plugin GUID), kept in registration order.
    /// Registering again with the same GUID replaces that mod's callback in place, so its position doesn't change.
    /// Every call goes through <see cref="Call{TResult}"/>: a callback that throws is reported once and then treated
    /// as having no opinion. Pure C# (no Unity/BepInEx) so it can be unit tested.
    /// </summary>
    public sealed class CallbackRegistry<T> where T : class
    {
        private readonly List<Entry> _entries = new List<Entry>();
        private readonly string _kind;
        private readonly Action<string> _log;

        /// <param name="kind">What the callbacks are, for log lines ("restart handler").</param>
        /// <param name="log">Where to report problems; may be null.</param>
        public CallbackRegistry(string kind, Action<string> log)
        {
            _kind = kind;
            _log = log;
        }

        public int Count => _entries.Count;

        /// <summary>Adds or replaces the callback for this GUID. A null callback removes it. An empty GUID is ignored.</summary>
        public void Set(string guid, T callback)
        {
            if (string.IsNullOrEmpty(guid))
            {
                _log?.Invoke($"Ignored a {_kind} registered without a plugin GUID.");
                return;
            }
            int index = _entries.FindIndex(e => e.Guid == guid);
            if (callback == null)
            {
                if (index >= 0)
                {
                    _entries.RemoveAt(index);
                }
                return;
            }
            var entry = new Entry(guid, callback);
            if (index >= 0)
            {
                _entries[index] = entry;
            }
            else
            {
                _entries.Add(entry);
            }
        }

        public void Remove(string guid)
        {
            _entries.RemoveAll(e => e.Guid == guid);
        }

        /// <summary>
        /// A copy of the current registrations, so a callback that registers or removes while the list is being
        /// walked can't break the walk.
        /// </summary>
        public IReadOnlyList<Entry> Snapshot()
        {
            return _entries.ToArray();
        }

        /// <summary>Runs one callback. If it throws, logs that once for this registration and returns <paramref name="noOpinion"/>.</summary>
        public TResult Call<TResult>(Entry entry, Func<T, TResult> invoke, TResult noOpinion)
        {
            try
            {
                return invoke(entry.Callback);
            }
            catch (Exception e)
            {
                if (!entry.Reported)
                {
                    entry.Reported = true;
                    _log?.Invoke($"The {_kind} from {entry.Guid} threw and is being ignored: {e.GetType().Name}: {e.Message}");
                }
                return noOpinion;
            }
        }

        public sealed class Entry
        {
            internal Entry(string guid, T callback)
            {
                Guid = guid;
                Callback = callback;
            }

            public string Guid { get; }

            public T Callback { get; }

            /// <summary>Set once a throw has been logged; a new registration starts fresh.</summary>
            internal bool Reported { get; set; }
        }
    }
}
