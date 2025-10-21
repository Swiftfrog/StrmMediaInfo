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
        private readonly MediaInfoService _mediaInfoService;

        // 构造函数：移除 IItemManager
        public ServerEntryPoint(
            ILogger logger, 
            ILibraryManager libraryManager, 
            ISessionManager sessionManager, 
            IMediaEncoder mediaEncoder,
            IUserManager userManager
            )
        {
            _logger = logger;
            _libraryManager = libraryManager;

            // 关键改动：创建服务实例时不再传递 itemManager
            _mediaInfoService = new MediaInfoService(logger, mediaEncoder, libraryManager, userManager);
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
            // 代码无变化
            if (e.Item is not BaseItem item || string.IsNullOrEmpty(item.Path) || !item.Path.EndsWith(".strm", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
            
            _logger.Info($"Evermedia Plugin: Item updated event for '{item.Name}'. Processing...");
            _mediaInfoService.ProcessStrmFile(item, CancellationToken.None);
        }

        private void OnLibraryManagerItemAdded(object sender, ItemChangeEventArgs e)
        {
            // 代码无变化
            if (e.Item is not BaseItem item || string.IsNullOrEmpty(item.Path) || !item.Path.EndsWith(".strm", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            _logger.Info($"Evermedia Plugin: Item added event for '{item.Name}'. Processing...");
            _mediaInfoService.ProcessStrmFile(item, CancellationToken.None);
        }
    }
}


