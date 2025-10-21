using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Dto;
// 新增：引入 IDirectoryService
using MediaBrowser.Model.IO;

namespace Evermedia
{
    public class MediaInfoService
    {
        private readonly ILogger _logger;
        private readonly IProviderManager _providerManager;
        private readonly IDirectoryService _directoryService; // 新增

        // 构造函数：接收并保存 IDirectoryService
        public MediaInfoService(ILogger logger, IProviderManager providerManager, IDirectoryService directoryService)
        {
            _logger = logger;
            _providerManager = providerManager;
            _directoryService = directoryService; // 新增
        }

        public async void RefreshAndBackupStrmInfo(BaseItem item, CancellationToken cancellationToken)
        {
            try
            {
                var realMediaPath = await GetLocalPathFromStrm(item.Path, cancellationToken);
                if (string.IsNullOrEmpty(realMediaPath))
                {
                    return;
                }

                _logger.Info($"Queueing metadata refresh for '{item.Name}' pointing to '{realMediaPath}'");
                
                // ########## 修正 1: 使用正确的构造函数 ##########
                var options = new MetadataRefreshOptions(_directoryService)
                {
                    MetadataRefreshMode = MetadataRefreshMode.FullRefresh,
                    ForceSave = true
                };
                
                // QueueRefresh 的参数签名保持不变，它们是正确的
                _providerManager.QueueRefresh(item.InternalId, options, RefreshPriority.Normal);
                
                await Task.Delay(5000, cancellationToken); 

                _logger.Info($"Metadata refresh queued for '{item.Name}'. Now attempting to back up info.");

                // ########## 修正 2: 使用正确的方法签名 ##########
                var mediaSources = item.GetMediaSources(false); 
                if (mediaSources.Count > 0)
                {
                    await BackupMediaInfoAsync(item, mediaSources[0], cancellationToken);
                }
                else
                {
                    _logger.Warn($"No media sources found for '{item.Name}' after refresh. The item may still be processing.");
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Error in RefreshAndBackupStrmInfo for {item.Path}: {ex.Message}", ex);
            }
        }

        private async Task<string> GetLocalPathFromStrm(string strmPath, CancellationToken cancellationToken)
        {
            if (!File.Exists(strmPath))
            {
                _logger.Warn($"Strm file not found at {strmPath}");
                return null;
            }

            var realMediaPath = (await File.ReadAllTextAsync(strmPath, cancellationToken)).Trim();

            if (string.IsNullOrWhiteSpace(realMediaPath) || realMediaPath.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                _logger.Info($"Skipping remote or empty strm: {realMediaPath}");
                return null;
            }

            if (!File.Exists(realMediaPath))
            {
                 _logger.Warn($"Real media file pointed to by strm does not exist: {realMediaPath}");
                 return null;
            }
            return realMediaPath;
        }
        
        private async Task BackupMediaInfoAsync(BaseItem item, MediaSourceInfo mediaSource, CancellationToken cancellationToken)
        {
            var backupPath = item.Path + ".medinfo";
            _logger.Info($"Backing up media info to: {backupPath}");

            var model = new MediaInfoModel { MediaSource = mediaSource };
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(model, options);

            await File.WriteAllTextAsync(backupPath, json, cancellationToken);
        }
    }
}


