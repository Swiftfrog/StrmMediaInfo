using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.MediaInfo;

namespace Evermedia
{
    public class MediaInfoService
    {
        private readonly ILogger _logger;
        private readonly IMediaEncoder _mediaEncoder;

        public MediaInfoService(ILogger logger, IMediaEncoder mediaEncoder)
        {
            _logger = logger;
            _mediaEncoder = mediaEncoder;
        }

        public async void ProbeAndBackupStrmInfo(BaseItem item, CancellationToken cancellationToken)
        {
            try
            {
                var realMediaPath = await GetLocalPathFromStrm(item.Path, cancellationToken);
                if (string.IsNullOrEmpty(realMediaPath)) return;

                _logger.Info($"Probing real media path for '{item.Name}': {realMediaPath}");

                // The correct API call to probe a file
                var request = new MediaInfoRequest { FilePath = realMediaPath };
                var mediaInfo = await _mediaEncoder.GetMediaInfo(request, cancellationToken);
                
                if (mediaInfo?.MediaSources == null || mediaInfo.MediaSources.Count == 0)
                {
                    _logger.Warn($"Probing did not return any media sources for {realMediaPath}");
                    return;
                }

                var mediaSource = mediaInfo.MediaSources[0];
                await BackupMediaInfoAsync(item, mediaSource, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.Error($"Error in ProbeAndBackupStrmInfo for {item.Path}: {ex.Message}", ex);
            }
        }

        private async Task<string> GetLocalPathFromStrm(string strmPath, CancellationToken cancellationToken)
        {
            if (!File.Exists(strmPath)) return null;
            var realMediaPath = (await File.ReadAllTextAsync(strmPath, cancellationToken)).Trim();
            if (string.IsNullOrWhiteSpace(realMediaPath) || !File.Exists(realMediaPath)) return null;
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


