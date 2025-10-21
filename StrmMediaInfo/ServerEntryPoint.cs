using System;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Model.Logging;

namespace StrmMediaInfo
{
    public class ServerEntryPoint : IServerEntryPoint
    {
        private readonly ILibraryManager _libraryManager;
        private readonly ILogger _logger;

        public ServerEntryPoint(ILibraryManager libraryManager, ILogManager logManager)
        {
            _libraryManager = libraryManager;
            _logger = logManager.GetLogger(GetType().Name);
        }

        public void Run()
        {
            _logger.Info("StrmMediaInfo Plugin: Initializing...");
            
            // 订阅“项目添加”事件
            _libraryManager.ItemAdded += OnLibraryManagerItemAdded;
            // 新增：订阅“项目更新”事件
            _libraryManager.ItemUpdated += OnLibraryManagerItemUpdated;
            
            _logger.Info("StrmMediaInfo Plugin: Ready and listening for item additions and updates.");
        }

        // “项目添加”事件的处理器
        private void OnLibraryManagerItemAdded(object sender, ItemChangeEventArgs e)
        {
            // 调用共享的处理方法
            ProcessStrmItem(e.Item);
        }

        // 新增：“项目更新”事件的处理器
        private void OnLibraryManagerItemUpdated(object sender, ItemChangeEventArgs e)
        {
            // 调用共享的处理方法
            ProcessStrmItem(e.Item);
        }

        // 共享的核心处理逻辑
        private void ProcessStrmItem(BaseItem item)
        {
            try
            {
                // 检查这是否是一个有效的 .strm 文件
                if (item != null && !string.IsNullOrEmpty(item.Path) && item.Path.EndsWith(".strm", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.Info($"SUCCESS! A .strm file was processed: '{item.Name}'. Trigger is working correctly.");
                    // 未来，真正的 Probe 和 Backup 逻辑将在这里被调用
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Error processing item '{item?.Name}'.", ex);
            }
        }

        public void Dispose()
        {
            // 取消订阅所有事件以防止内存泄漏
            _libraryManager.ItemAdded -= OnLibraryManagerItemAdded;
            _libraryManager.ItemUpdated -= OnLibraryManagerItemUpdated;
        }
    }
}


