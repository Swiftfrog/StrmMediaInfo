using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;

namespace Evermedia
{
    /// <summary>
    /// 核心服务，负责探测、备份和恢复 .strm 文件的媒体信息。
    /// </summary>
    public class MediaInfoService
    {
        private readonly ILogger _logger;
        private readonly IMediaEncoder _mediaEncoder;
        private readonly ILibraryManager _libraryManager;
        
        // JSON 序列化选项
        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

        public MediaInfoService(ILogManager logManager, IMediaEncoder mediaEncoder, ILibraryManager libraryManager)
        {
            _logger = logManager.GetLogger(GetType().Name);
            _mediaEncoder = mediaEncoder;
            _libraryManager = libraryManager;
        }

        /// <summary>
        /// 主方法：探测、备份媒体信息，并将其持久化到 Emby 数据库。
        /// </summary>
        public async Task ProbeAndBackupMediaInfoAsync(BaseItem item, CancellationToken cancellationToken)
        {
            // 步骤 1: 读取 .strm 文件获取真实媒体路径
            string mediaPath;
            try
            {
                mediaPath = await File.ReadAllTextAsync(item.Path, cancellationToken);
                mediaPath = mediaPath.Trim();
                if (string.IsNullOrWhiteSpace(mediaPath) || mediaPath.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.Info($"Skipping probe for remote or empty .strm file: {item.Path}");
                    return;
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to read .strm file content: {item.Path}", ex);
                return;
            }

            // 步骤 2: 调用 Emby 的 MediaEncoder 探测真实媒体信息
            _logger.Info($"Probing media info for: {mediaPath}");
            MediaBrowser.Model.MediaInfo.MediaInfoResult probedInfo;
            try
            {
                probedInfo = await _mediaEncoder.GetMediaInfo(new MediaInfo.MediaInfoRequest { Path = mediaPath }, cancellationToken);
                if (probedInfo?.MediaSources == null || probedInfo.MediaSources.Count == 0)
                {
                    _logger.Warn($"Probe result for '{mediaPath}' is empty or invalid.");
                    return;
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Error probing media file '{mediaPath}'.", ex);
                return;
            }
            
            var mediaSource = probedInfo.MediaSources[0];

            // 步骤 3: 将探测结果序列化并备份到 .medinfo 文件
            try
            {
                var backupModel = new MediaInfoModel { MediaSource = mediaSource };
                var json = JsonSerializer.Serialize(backupModel, JsonOptions);
                var backupPath = item.Path + ".medinfo";
                await File.WriteAllTextAsync(backupPath, json, cancellationToken);
                _logger.Info($"Successfully backed up media info to: {backupPath}");
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to backup media info for item: {item.Name}", ex);
                // 即使备份失败，我们仍然继续尝试更新数据库
            }

            // 步骤 4: 将媒体信息持久化到 Emby 数据库 (关键步骤!)
            try
            {
                _logger.Info($"Updating database for item: {item.Name} (ID: {item.Id})");

                // 更新媒体流信息
                await _libraryManager.SaveMediaStreams(item.Id, mediaSource.MediaStreams);

                // 更新 Item 的核心属性
                item.RunTimeTicks = mediaSource.RunTimeTicks;
                item.HasSubtitles = mediaSource.MediaStreams.Any(s => s.Type == MediaStreamType.Subtitle);
                // 可以根据需要添加更多字段，例如 3D 格式等
                // item.Video3DFormat = mediaSource.Video3DFormat;

                // 通知 Emby 刷新这个项目
                await _libraryManager.UpdateItem(item, item.Parent, ItemUpdateType.MetadataEdit, cancellationToken);
                
                _logger.Info($"Database update complete for: {item.Name}");
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to persist media info to database for item: {item.Name}", ex);
            }
        }
    }
}

