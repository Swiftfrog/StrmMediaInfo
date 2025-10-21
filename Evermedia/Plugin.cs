using System;
using System.Collections.Generic; // 新增：解决 IEnumerable<> 找不到的问题
using Evermedia.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.DependencyInjection; // 新增：解决 IServiceCollection 和 AddScoped 找不到的问题

namespace Evermedia
{
    public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
    {
        public override string Name => "Evermedia";
        public override Guid Id => Guid.Parse("a54b9714-35ef-45e6-9915-f5a01339a44c");
        public override string Description => "Automatically probes and persists media information for .strm files.";

        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
            : base(applicationPaths, xmlSerializer)
        {
        }

        // 现在编译器可以正确识别 IEnumerable<>
        public IEnumerable<PluginPageInfo> GetPages()
        {
            return new[]
            {
                new PluginPageInfo
                {
                    Name = "evermedia",
                    EmbeddedResourcePath = GetType().Namespace + ".Configuration.config.html",
                    EnableInMainMenu = true
                }
            };
        }
        
        // 现在编译器可以正确识别 IServiceCollection 和 AddScoped
        public override void ConfigureServices(IServiceCollection serviceCollection)
        {
            serviceCollection.AddScoped<MediaInfoService>();
        }
    }
}


