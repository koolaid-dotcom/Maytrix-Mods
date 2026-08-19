using BepInEx.Logging;
using System;

namespace MaytrixMods
{
    /// <summary>
    /// Owns the menu lifecycle so the BepInEx entry point stays small.
    /// </summary>
    internal sealed class MenuManager : IDisposable
    {
        private MaytrixMenu? _menu;

        public MenuManager(ManualLogSource logger)
        {
            _menu = new MaytrixMenu(logger);
        }

        public void Tick()
        {
            _menu?.Tick();
        }

        public void Dispose()
        {
            _menu?.Dispose();
            _menu = null;
        }
    }
}
