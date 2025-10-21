using System;
using System.Collections.Generic;
using Evermedia.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace Evermedia
{
    // IHasAdditionalParts is the key to registering our provider
    public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages, IHasAdditionalParts
    {
        public override string Name => "Evermedia";
        public override Guid Id => Guid.Parse("a54b9714-35ef-45e6-9915-f5a01339a44c");
        public override string Description => "Automatically probes and persists media information for .strm files.";

        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
            : base(applicationPaths, xmlSerializer)
        {
        }

        public IEnumerable<PluginPageInfo> GetPages()
        {
            return new[]
            {
                new PluginPageInfo
                {
                    Name = "evermedia",
                    EmbeddedResourcePath = GetType().Namespace + ".Configuration.config.html"
                }
            };
        }
        
        // This method tells Emby to load our StrmMediaSourceProvider
        public IEnumerable<Type> GetAdditionalParts()
        {
            return new[] { typeof(StrmMediaSourceProvider) };
        }
    }
}


