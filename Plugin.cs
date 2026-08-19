using BepInEx;

namespace MaytrixMods
{
    [BepInPlugin(PluginInfo.Guid, PluginInfo.Name, PluginInfo.Version)]
    public sealed class Plugin : BaseUnityPlugin
    {
        private MenuManager? _menuManager;

        private void Awake()
        {
            _menuManager = new MenuManager(Logger);
            Logger.LogInfo($"{PluginInfo.Name} {PluginInfo.Version} loaded");
        }

        private void Update()
        {
            _menuManager?.Tick();
        }

        private void OnDestroy()
        {
            _menuManager?.Dispose();
            _menuManager = null;
        }
    }
}
