using BepInEx;
using Maytrix.Menu.Menu;
using Maytrix.Menu.Services;

namespace Maytrix.Menu
{
    [BepInPlugin(PluginInfo.Guid, PluginInfo.Name, PluginInfo.Version)]
    public sealed class Plugin : BaseUnityPlugin
    {
        private MenuController? _menu;
        private LocalGraphicsController? _graphics;

        private void Awake()
        {
            var settings = new MenuSettings(Config);
            _graphics = new LocalGraphicsController(settings, Logger);
            _menu = new MenuController(settings, _graphics, Logger);
            Logger.LogInfo($"{PluginInfo.Name} {PluginInfo.Version} loaded");
        }

        private void Update()
        {
            _graphics?.Tick();
            _menu?.Tick();
        }

        private void OnDestroy()
        {
            _menu?.Dispose();
            _menu = null;
            _graphics?.Dispose();
            _graphics = null;
        }
    }
}
