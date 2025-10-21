using System;
using System.Collections.Generic;
using Evermedia.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.DependencyInjection;

namespace Evermedia
{
    // 1. 新增 IHasServices 接口
    public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages, IHasServices
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
                    EmbeddedResourcePath = GetType().Namespace + ".Configuration.config.html",
                    EnableInMainMenu = true
                }
            };
        }
        
        // 2. 移除 "override" 关键字
        public void ConfigureServices(IServiceCollection serviceCollection)
        {
            // 这是实现 IHasServices 接口的方法
            serviceCollection.AddScoped<MediaInfoService>();
        }
    }
}


