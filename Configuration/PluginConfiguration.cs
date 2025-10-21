using MediaBrowser.Model.Plugins;

namespace StrmMediaInfo.Configuration
{
    public class PluginConfiguration : BasePluginConfiguration
    {
        // Add any configuration properties here in the future
        public bool IsFirstRun { get; set; } = true;
    }
}
