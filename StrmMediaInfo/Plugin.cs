using System;
using System.Collections.Generic;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using StrmMediaInfo.Configuration;

namespace StrmMediaInfo
{
    /// <summary>
    /// The main plugin class
    /// </summary>
    public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
    {
        public override string Name => "Strm MediaInfo Plugin";

        public override Guid Id => Guid.Parse("a1d6df33-2a66-455b-9b48-527b37e402d4");

        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
            : base(applicationPaths, xmlSerializer)
        {
            Instance = this;
        }

        public static Plugin Instance { get; private set; }

        public override string Description => "Automatically fetch, persist, and restore MediaInfo for .strm files to ensure correct playback decisions.";

        public IEnumerable<PluginPageInfo> GetPages()
        {
            return new[]
            {
                new PluginPageInfo
                {
                    Name = "StrmMediaInfo",
                    EmbeddedResourcePath = GetType().Namespace + ".Configuration.config.html"
                }
            };
        }
    }
}
