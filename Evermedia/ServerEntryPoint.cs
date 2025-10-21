using System;
using System.Threading;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Controller.MediaEncoding;

namespace Evermedia
{
    public class ServerEntryPoint : IServerEntryPoint
    {
        private readonly ILibraryManager _libraryManager;
        private readonly ILogger _logger;
        private readonly MediaInfoService _mediaInfoService; // 存储我们自己服务的实例

        // 构造函数：请求所有需要的官方服务
        public ServerEntryPoint(
            ILogger logger, 
            ILibraryManager libraryManager, 
            ISessionManager sessionManager, 
            IMediaEncoder mediaEncoder, 
            IItemManager itemManager,
            IUserManager userManager
            )
        {
            _logger = logger;
            _libraryManager = libraryManager;

            // 关键改动：在这里手动创建我们的服务实例，并将所有依赖传递进去
            _mediaInfoService = new MediaInfoService(logger, mediaEncoder, libraryManager, itemManager, userManager);
        }

        public void Run()
        {
            _libraryManager.ItemAdded += OnLibraryManagerItemAdded;
            _libraryManager.ItemUpdated += OnLibraryManagerItemUpdated;
        }

        public void Dispose()
        {
            _libraryManager.ItemAdded -= OnLibraryManagerItemAdded;
            _libraryManager.ItemUpdated -= OnLibraryManagerItemUpdated;
        }

        private void OnLibraryManagerItemUpdated(object sender, ItemChangeEventArgs e)
        {
            if (e.Item is not BaseItem item || string.IsNullOrEmpty(item.Path) || !item.Path.EndsWith(".strm", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
            
            _logger.Info($"Evermedia Plugin: Item updated event for '{item.Name}'. Processing...");
            _mediaInfoService.ProcessStrmFile(item, CancellationToken.None);
        }

        private void OnLibraryManagerItemAdded(object sender, ItemChangeEventArgs e)
        {
            if (e.Item is not BaseItem item || string.IsNullOrEmpty(item.Path) || !item.Path.EndsWith(".strm", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            _logger.Info($"Evermedia Plugin: Item added event for '{item.Name}'. Processing...");
            _mediaInfoService.ProcessStrmFile(item, CancellationToken.None);
        }
    }
}


