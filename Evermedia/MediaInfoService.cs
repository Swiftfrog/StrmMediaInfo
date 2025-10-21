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

namespace Evermedia
{
    public class MediaInfoService
    {
        private readonly ILogger _logger;
        private readonly IMediaEncoder _mediaEncoder;
        private readonly ILibraryManager _libraryManager;
        private readonly IUserManager _userManager;

        // 更新构造函数以移除 IItemManager
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

        private async Task<MediaSourceInfo> ProbeAndExtractMediaInfoAsync(BaseItem item, CancellationToken cancellationToken)
        {
            // 代码无变化
            var strmPath = item.Path;
            if (!File.Exists(strmPath))
            {
                _logger.Warn($"Strm file not found at {strmPath}");
                return null;
            }

            var realMediaPath = await File.ReadAllTextAsync(strmPath, cancellationToken);
            realMediaPath = realMediaPath.Trim();

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
            var mediaInfo = await _mediaEncoder.GetMediaInfo(new MediaInfoRequest
            {
                Path = realMediaPath,
                ExtractChapters = true,
                MediaType = "Video"
            }, cancellationToken);

            return mediaInfo.MediaSources[0];
        }

        private async Task BackupMediaInfoAsync(BaseItem item, MediaSourceInfo mediaSource)
        {
            // 代码无变化
            var backupPath = item.Path + ".medinfo";
            _logger.Info($"Backing up media info to: {backupPath}");

            var model = new MediaInfoModel
            {
                MediaSource = mediaSource,
            };

            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(model, options);

            await File.WriteAllTextAsync(backupPath, json);
        }

        // ########## 核心改动在这里 ##########
        private async Task PersistMediaInfoToDatabase(BaseItem item, MediaSourceInfo mediaSource, CancellationToken cancellationToken)
        {
            _logger.Info($"Persisting media info to database for: {item.Name}");

            if (item is not Video video)
            {
                _logger.Warn("Item is not a Video type, cannot save media streams.");
                return;
            }
            
            // 关键步骤 1: 将探测到的媒体源信息的路径，强制设置为 strm 文件本身的路径
            // 这一点至关重要，否则 Emby 无法将此媒体源与当前项目关联起来
            mediaSource.Path = item.Path;

            // 关键步骤 2: 创建一个新的媒体源列表，并用探测到的信息替换它
            video.SetMediaSources(new List<MediaSourceInfo> { mediaSource });
            
            // 关键步骤 3: 同时更新时长等其他元数据
            video.RunTimeTicks = mediaSource.RunTimeTicks;

            // 关键步骤 4: 一次性将所有变更更新到数据库
            await video.UpdateToRepositoryAsync(ItemUpdateType.MetadataEdit, cancellationToken);
            
            _logger.Info("Successfully persisted media info and updated repository.");
        }
    }
}


