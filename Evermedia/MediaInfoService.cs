using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Dto;

namespace Evermedia
{
    public class MediaInfoService
    {
        private readonly ILogger _logger;
        private readonly IProviderManager _providerManager;

        public MediaInfoService(ILogger logger, IProviderManager providerManager)
        {
            _logger = logger;
            _providerManager = providerManager;
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
                
                // ########## 修正 1: 使用无参数构造函数 ##########
                var options = new MetadataRefreshOptions
                {
                    MetadataRefreshMode = MetadataRefreshMode.FullRefresh,
                    ForceSave = true
                };
                
                // ########## 修正 2: 使用正确的 QueueRefresh 参数 ##########
                // 参数1: item.InternalId (long)
                // 参数3: RefreshPriority.Normal (enum)
                _providerManager.QueueRefresh(item.InternalId, options, RefreshPriority.Normal);
                
                // 我们不再 await QueueRefresh，因为它是一个即发即忘的后台任务排队。
                // 我们需要一种方式等待它完成。最简单的方式是短暂地延迟一下。
                // 注意：这是一个简化的实现。一个更复杂的实现会使用事件来确认刷新完成。
                await Task.Delay(5000, cancellationToken); // 等待 5 秒，让 Emby 有时间完成刷新

                _logger.Info($"Metadata refresh queued for '{item.Name}'. Now attempting to back up info.");

                // ########## 修正 3: 使用无参数的 GetMediaSources ##########
                var mediaSources = item.GetMediaSources(); 
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


