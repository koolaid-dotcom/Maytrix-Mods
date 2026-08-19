using BepInEx;

namespace MaytrixMods
{
    [BepInPlugin(PluginInfo.Guid, PluginInfo.Name, PluginInfo.Version)]
    public sealed class Plugin : BaseUnityPlugin
    {
        private MaytrixMenu? _menu;

        private void Awake()
        {
            _menu = new MaytrixMenu(Logger);
            Logger.LogInfo($"{PluginInfo.Name} {PluginInfo.Version} loaded");
        }

        private void Update()
        {
            _menu?.Tick();
        }

        private void OnDestroy()
        {
            _menu?.Dispose();
            _menu = null;
        }
    }
}

