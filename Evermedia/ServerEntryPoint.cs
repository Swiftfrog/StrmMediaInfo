using System;
using System.Threading;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
// 新增：引入 ProviderManager
using MediaBrowser.Controller.Providers;

namespace Evermedia
{
    public class ServerEntryPoint : IServerEntryPoint
    {
        private readonly ILibraryManager _libraryManager;
        private readonly ILogger _logger;
        private readonly MediaInfoService _mediaInfoService;

        // 构造函数：注入正确的服务 IProviderManager
        public ServerEntryPoint(
            ILogger logger, 
            ILibraryManager libraryManager,
            IProviderManager providerManager // 不再需要 IMediaEncoder, IUserManager 等
            )
        {
            _logger = logger;
            _libraryManager = libraryManager;

            // 创建服务实例
            _mediaInfoService = new MediaInfoService(logger, providerManager);
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
            // 直接调用我们的服务
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


