using System;
using System.Threading;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Controller.MediaEncoding; // Required for IMediaEncoder

namespace Evermedia
{
    public class ServerEntryPoint : IServerEntryPoint
    {
        private readonly ILibraryManager _libraryManager;
        private readonly ILogger _logger;
        private readonly MediaInfoService _mediaInfoService;

        public ServerEntryPoint(ILogger logger, ILibraryManager libraryManager, IMediaEncoder mediaEncoder)
        {
            _logger = logger;
            _libraryManager = libraryManager;
            _mediaInfoService = new MediaInfoService(logger, mediaEncoder);
        }

        public void Run()
        {
            _libraryManager.ItemAdded += OnLibraryManagerItemAdded;
            _libraryManager.ItemUpdated += OnLibraryManagerItemUpdated;
        }

        public void Dispose()
        {
            _libraryManager.ItemAdded -= OnLibraryManagerItemAdded;
            _libraryManager.ItemUpdated -= OnLibraryManagerItemUpdated;
        }

        private void ProcessItem(BaseItem item, CancellationToken cancellationToken)
        {
            if (item == null || string.IsNullOrEmpty(item.Path) || !item.Path.EndsWith(".strm", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            _logger.Info($"Evermedia Plugin: Event triggered for '{item.Name}'. Processing...");
            _mediaInfoService.ProbeAndBackupStrmInfo(item, cancellationToken);
        }

        private void OnLibraryManagerItemUpdated(object sender, ItemChangeEventArgs e)
        {
            ProcessItem(e.Item, CancellationToken.None);
        }

        private void OnLibraryManagerItemAdded(object sender, ItemChangeEventArgs e)
        {
            ProcessItem(e.Item, CancellationToken.None);
        }
    }
}


