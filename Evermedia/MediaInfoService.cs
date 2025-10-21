using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Logging;
// 新增：引入 ProviderManager 和正确的 DTO
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Dto;

namespace Evermedia
{
    public class MediaInfoService
    {
        private readonly ILogger _logger;
        private readonly IProviderManager _providerManager;

        // 构造函数：注入正确的服务
        public MediaInfoService(ILogger logger, IProviderManager providerManager)
        {
            _logger = logger;
            _providerManager = providerManager;
        }

        public async void RefreshAndBackupStrmInfo(BaseItem item, CancellationToken cancellationToken)
        {
            try
            {
                // 步骤 1: 检查 strm 文件是否指向本地文件，如果是远程则跳过
                var realMediaPath = await GetLocalPathFromStrm(item.Path, cancellationToken);
                if (string.IsNullOrEmpty(realMediaPath))
                {
                    return; // 日志已在 GetLocalPathFromStrm 中记录
                }

                // 步骤 2: 告诉 Emby 强制刷新这个项目。
                // Emby 会自动探测 realMediaPath 并将结果存入数据库。
                // 这是 API 的核心用法。
                _logger.Info($"Queueing metadata refresh for '{item.Name}' pointing to '{realMediaPath}'");
                var options = new MetadataRefreshOptions(new DirectoryService())
                {
                    MetadataRefreshMode = MetadataRefreshMode.FullRefresh,
                    ForceSave = true // 确保新探测的信息被保存
                };
                
                // QueueRefresh 会在后台执行，我们 await 等待它完成
                await _providerManager.QueueRefresh(item.Id, options, cancellationToken);
                _logger.Info($"Metadata refresh completed for '{item.Name}'.");


                // 步骤 3: 刷新完成后，从项目中读取刚刚被保存的媒体信息，并备份到 .medinfo 文件
                // API CHANGE: 使用 item.GetMediaSources() 来读取信息
                var mediaSources = item.GetMediaSources(false); 
                if (mediaSources.Count > 0)
                {
                    await BackupMediaInfoAsync(item, mediaSources[0]);
                }
                else
                {
                    _logger.Warn($"No media sources found for '{item.Name}' after refresh.");
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
        
        private async Task BackupMediaInfoAsync(BaseItem item, MediaSourceInfo mediaSource)
        {
            var backupPath = item.Path + ".medinfo";
            _logger.Info($"Backing up media info to: {backupPath}");

            var model = new MediaInfoModel { MediaSource = mediaSource };
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(model, options);

            await File.WriteAllTextAsync(backupPath, json);
        }
    }
}
