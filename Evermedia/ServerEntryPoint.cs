using System;
using System.Threading;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Controller.Providers;
// 新增：引入 IDirectoryService
using MediaBrowser.Model.IO;

namespace Evermedia
{
    public class ServerEntryPoint : IServerEntryPoint
    {
        private readonly ILibraryManager _libraryManager;
        private readonly ILogger _logger;
        private readonly MediaInfoService _mediaInfoService;

        // 构造函数：注入所有需要的服务，包括 IDirectoryService
        public ServerEntryPoint(
            ILogger logger, 
            ILibraryManager libraryManager,
            IProviderManager providerManager,
            IDirectoryService directoryService // 新增
            )
        {
            _logger = logger;
            _libraryManager = libraryManager;

            // 创建服务实例，并将 directoryService 传递进去
            _mediaInfoService = new MediaInfoService(logger, providerManager, directoryService);
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

        private void ProcessItem(BaseItem item)
        {
            if (item == null || string.IsNullOrEmpty(item.Path) || !item.Path.EndsWith(".strm", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            _logger.Info($"Evermedia Plugin: Event triggered for '{item.Name}'. Processing...");
            _mediaInfoService.RefreshAndBackupStrmInfo(item, CancellationToken.None);
        }

        private void OnLibraryManagerItemUpdated(object sender, ItemChangeEventArgs e)
        {
            ProcessItem(e.Item);
        }

        private void OnLibraryManagerItemAdded(object sender, ItemChangeEventArgs e)
        {
            ProcessItem(e.Item);
        }
    }
}


