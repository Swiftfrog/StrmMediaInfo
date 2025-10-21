using System;
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
        private readonly IItemManager _itemManager;
        private readonly IUserManager _userManager;

        // 更新构造函数以接收所有必要的服务
        public MediaInfoService(
            ILogger logger, 
            IMediaEncoder mediaEncoder, 
            ILibraryManager libraryManager, 
            IItemManager itemManager, 
            IUserManager userManager)
        {
            _logger = logger;
            _mediaEncoder = mediaEncoder;
            _libraryManager = libraryManager;
            _itemManager = itemManager;
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
            var backupPath = item.Path + ".medinfo";
            _logger.Info($"Backing up media info to: {backupPath}");

            var model = new MediaInfoModel
            {
                MediaSource = mediaSource,
                // Chapters can be added here if needed
            };

            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(model, options);

            await File.WriteAllTextAsync(backupPath, json);
        }

        private async Task PersistMediaInfoToDatabase(BaseItem item, MediaSourceInfo mediaSource, CancellationToken cancellationToken)
        {
            _logger.Info($"Persisting media info to database for: {item.Name}");

            item.RunTimeTicks = mediaSource.RunTimeTicks;
            
            // 使用 ItemManager 来更新媒体流信息
            var video = item as Video;
            if (video == null)
            {
                _logger.Warn("Item is not a Video type, cannot save media streams.");
                return;
            }
            
            var DtoMediaSource = new MediaSourceInfo
            {
                 Id = mediaSource.Id,
                 Path = item.Path, // 关键：Path 必须是 strm 文件本身的路径
                 Protocol = mediaSource.Protocol,
                 RunTimeTicks = mediaSource.RunTimeTicks,
                 Container = mediaSource.Container,
                 // ... 复制其他需要的字段
            };
            
            _itemManager.SaveMediaStreams(video, new DtoOptions(true), new []{ DtoMediaSource });

            // 更新项目以刷新UI
            await item.UpdateToRepositoryAsync(ItemUpdateType.MetadataEdit, cancellationToken);
            _logger.Info("Successfully persisted media info and updated repository.");
        }
    }
}


