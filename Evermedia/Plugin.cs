using System;
using Evermedia.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.DependencyInjection;

namespace Evermedia
{
    public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
    {
        public override string Name => "Evermedia";
        public override Guid Id => Guid.Parse("a54b9714-35ef-45e6-9915-f5a01339a44c"); // 随机生成的新 GUID
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
                    EmbeddedResourcePath = GetType().Namespace + ".Configuration.config.html",
                    EnableInMainMenu = true
                }
            };
        }
        
        // 关键：注册我们的核心服务
        public override void ConfigureServices(IServiceCollection serviceCollection)
        {
            serviceCollection.AddScoped<MediaInfoService>();
        }
    }
}

