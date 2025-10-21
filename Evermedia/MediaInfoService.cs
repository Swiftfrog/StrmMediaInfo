using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
// 新增: 这是 GetPathInfo 返回对象所在的命名空间
using MediaBrowser.Model.MediaInfo;

namespace Evermedia
{
    public class MediaInfoService
    {
        private readonly ILogger _logger;
        private readonly IMediaEncoder _mediaEncoder;
        private readonly ILibraryManager _libraryManager;
        private readonly IUserManager _userManager;

        public MediaInfoService(
            ILogger logger, 
            IMediaEncoder mediaEncoder, 
            ILibraryManager libraryManager,
            IUserManager userManager)
        {
            _logger = logger;
            _mediaEncoder = mediaEncoder;
            _libraryManager = libraryManager;
            _userManager = userManager;
        }

        public async void ProcessStrmFile(BaseItem item, CancellationToken cancellationToken)
        {
            try
            {
                _logger.Info($"Processing .strm file: {item.Path}");

                var mediaInfoResult = await ProbeAndExtractMediaInfoAsync(item, cancellationToken);
                if (mediaInfoResult == null)
                {
                    _logger.Warn($"Failed to probe media info for {item.Path}.");
                    return;
                }

                await BackupMediaInfoAsync(item, mediaInfoResult);

                await PersistMediaInfoToDatabase(item, mediaInfoResult, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.Error($"Error processing .strm file {item.Path}: {ex.Message}", ex);
            }
        }

        // ########## 第 1 处核心改动 ##########
        private async Task<MediaSourceInfo> ProbeAndExtractMediaInfoAsync(BaseItem item, CancellationToken cancellationToken)
        {
            var strmPath = item.Path;
            if (!File.Exists(strmPath))
            {
                _logger.Warn($"Strm file not found at {strmPath}");
                return null;
            }

            var realMediaPath = (await File.ReadAllTextAsync(strmPath, cancellationToken)).Trim();

            if (string.IsNullOrWhiteSpace(realMediaPath) || realMediaPath.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                _logger.Info($"Skipping probe for remote or empty strm: {realMediaPath}");
                return null;
            }

            if (!File.Exists(realMediaPath))
            {
                 _logger.Warn($"Real media file pointed to by strm does not exist: {realMediaPath}");
                 return null;
            }

            _logger.Info($"Probing real media file: {realMediaPath}");
            
            // API CHANGE: 使用 GetPathInfo, 它直接接收路径字符串
            var probeResult = await _mediaEncoder.GetPathInfo(realMediaPath, cancellationToken);

            if (probeResult?.MediaSources == null || probeResult.MediaSources.Count == 0)
            {
                _logger.Warn($"Probing did not return any media sources for {realMediaPath}");
                return null;
            }

            return probeResult.MediaSources[0];
        }

        private async Task BackupMediaInfoAsync(BaseItem item, MediaSourceInfo mediaSource)
        {
            var backupPath = item.Path + ".medinfo";
            _logger.Info($"Backing up media info to: {backupPath}");

            var model = new MediaInfoModel { MediaSource = mediaSource };
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(model, options);

            await File.WriteAllTextAsync(backupPath, json);
        }

        // ########## 第 2 处核心改动 ##########
        private async Task PersistMediaInfoToDatabase(BaseItem item, MediaSourceInfo mediaSource, CancellationToken cancellationToken)
        {
            _logger.Info($"Persisting media info to database for: {item.Name}");

            if (item is not Video video)
            {
                _logger.Warn("Item is not a Video type, cannot save media streams.");
                return;
            }
            
            mediaSource.Path = item.Path;

            // API CHANGE: 直接为 MediaSources 属性赋一个新列表
            video.MediaSources = new List<MediaSourceInfo> { mediaSource };
            
            video.RunTimeTicks = mediaSource.RunTimeTicks;

            // API CHANGE: 使用 ILibraryManager.UpdateItem 来保存变更
            // 注意：UpdateItem 是同步方法，但在后台是异步执行的。
            // 为了保持方法签名，我们用 Task.CompletedTask 包装。
            _libraryManager.UpdateItem(video, video.Parent, ItemUpdateType.MetadataEdit, CancellationToken.None);
            
            _logger.Info("Successfully persisted media info and triggered repository update.");
            
            await Task.CompletedTask;
        }
    }
}


