using System;

namespace PeglinQuickRestart.Core
{
    /// <summary>
    /// What other mods have registered through QuickRestartApi: restart handlers (first one that says it can handle
    /// the current run gets the restart) and blockers (any one saying "block" turns restarting off). With nothing
    /// registered, nothing is blocked and nothing is handled, so the mod's own restart runs as before.
    /// Pure C# (no Unity/BepInEx) so it can be unit tested.
    /// </summary>
    public sealed class RestartRegistry
    {
        private readonly CallbackRegistry<Handler> _handlers;
        private readonly CallbackRegistry<Func<bool>> _blockers;
        private readonly CallbackRegistry<Action<string>> _listeners;

        public RestartRegistry(Action<string> log)
        {
            _handlers = new CallbackRegistry<Handler>("restart handler", log);
            _blockers = new CallbackRegistry<Func<bool>>("restart blocker", log);
            _listeners = new CallbackRegistry<Action<string>>("restart listener", log);
        }

        /// <summary>A null callback removes this GUID's listener.</summary>
        public void SetListener(string guid, Action<string> listener)
        {
            _listeners.Set(guid, listener);
        }

        /// <summary>
        /// Tells every listener a restart went ahead, and what asked for it. A listener that throws is logged once and
        /// skipped; the others are still told.
        /// </summary>
        public void NotifyRestart(string source)
        {
            foreach (var entry in _listeners.Snapshot())
            {
                _listeners.Call(entry, listener => { listener(source); return true; }, false);
            }
        }

        /// <summary>Both callbacks are needed; a null for either removes this GUID's handler.</summary>
        public void RegisterHandler(string guid, Func<bool> canHandle, Action restart)
        {
            _handlers.Set(guid, canHandle != null && restart != null ? new Handler(canHandle, restart) : null);
        }

        /// <summary>A null callback removes this GUID's blocker.</summary>
        public void SetBlocker(string guid, Func<bool> block)
        {
            _blockers.Set(guid, block);
        }

        public void RemoveAll(string guid)
        {
            _handlers.Remove(guid);
            _blockers.Remove(guid);
            _listeners.Remove(guid);
        }

        /// <summary>Set while blockers or handlers are being asked; a question asked from inside one of them gets "no opinion".</summary>
        private bool _asking;

        /// <summary>True if any blocker says so. A blocker that throws doesn't block, and nor does a check made from inside a blocker or handler.</summary>
        public bool IsBlocked()
        {
            if (_asking)
            {
                return false;
            }
            _asking = true;
            try
            {
                return AnyBlocker();
            }
            finally
            {
                _asking = false;
            }
        }

        private bool AnyBlocker()
        {
            foreach (var entry in _blockers.Snapshot())
            {
                if (_blockers.Call(entry, block => block(), false))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>True if any handler would take a restart of the current run. A handler that throws doesn't, and nor does a check made from inside a blocker or handler.</summary>
        public bool AnyCanHandle()
        {
            return FindHandler() != null;
        }

        /// <summary>
        /// Gives the restart to the first handler that can handle it. Returns true if one did; the caller must then do
        /// nothing else. A handler whose restart throws still counts as having taken it, because it may already have
        /// abandoned the run, and running the normal restart on top of that could delete the wrong save.
        /// </summary>
        public bool TryHandle()
        {
            CallbackRegistry<Handler>.Entry entry = FindHandler();
            if (entry == null)
            {
                return false;
            }
            _handlers.Call(entry, handler => { handler.Restart(); return true; }, true);
            return true;
        }

        private CallbackRegistry<Handler>.Entry FindHandler()
        {
            if (_asking)
            {
                return null;
            }
            _asking = true;
            try
            {
                return FirstHandler();
            }
            finally
            {
                _asking = false;
            }
        }

        private CallbackRegistry<Handler>.Entry FirstHandler()
        {
            foreach (var entry in _handlers.Snapshot())
            {
                if (_handlers.Call(entry, handler => handler.CanHandle(), false))
                {
                    return entry;
                }
            }
            return null;
        }

        private sealed class Handler
        {
            public Handler(Func<bool> canHandle, Action restart)
            {
                CanHandle = canHandle;
                Restart = restart;
            }

            public Func<bool> CanHandle { get; }

            public Action Restart { get; }
        }
    }
}
