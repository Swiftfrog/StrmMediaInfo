using System;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Model.Logging;

namespace Evermedia
{
    public class ServerEntryPoint : IServerEntryPoint
    {
        private readonly ILibraryManager _libraryManager;
        private readonly ILogger _logger;
        private readonly MediaInfoService _mediaInfoService;

        // 新增：通过依赖注入获取 MediaInfoService 实例
        public ServerEntryPoint(ILibraryManager libraryManager, ILogManager logManager, MediaInfoService mediaInfoService)
        {
            _libraryManager = libraryManager;
            _logger = logManager.GetLogger(GetType().Name);
            _mediaInfoService = mediaInfoService;
        }

        public void Run()
        {
            _logger.Info("Evermedia Plugin: Initializing...");
            _libraryManager.ItemAdded += OnLibraryManagerItemChanged;
            _libraryManager.ItemUpdated += OnLibraryManagerItemChanged;
            _logger.Info("Evermedia Plugin: Ready and listening for item additions and updates.");
        }

        // 将两个事件处理器合并，并改为 async
        private async void OnLibraryManagerItemChanged(object sender, ItemChangeEventArgs e)
        {
            await ProcessStrmItem(e.Item);
        }

        // 共享的核心处理逻辑，调用 MediaInfoService
        private async Task ProcessStrmItem(BaseItem item)
        {
            try
            {
                if (item != null && !string.IsNullOrEmpty(item.Path) && item.Path.EndsWith(".strm", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.Info($"Processing .strm file: '{item.Name}'.");
                    // 调用核心服务来处理探测和备份
                    await _mediaInfoService.ProbeAndBackupMediaInfoAsync(item, CancellationToken.None);
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Error processing item '{item?.Name}'.", ex);
            }
        }

        public void Dispose()
        {
            _libraryManager.ItemAdded -= OnLibraryManagerItemChanged;
            _libraryManager.ItemUpdated -= OnLibraryManagerItemChanged;
        }
    }
}

