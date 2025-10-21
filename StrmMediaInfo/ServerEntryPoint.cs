using System;
using System.Threading;
using System.Threading.Tasks;
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

        // CORRECTED: Changed from RunAsync(CancellationToken) to the synchronous Run()
        public void Run()
        {
            _logger.Info("StrmMediaInfo Plugin: Initializing...");
            
            // Subscribe to the ItemAdded event
            _libraryManager.ItemAdded += OnLibraryManagerItemAdded;
            
            _logger.Info("StrmMediaInfo Plugin: Ready and listening for new items.");
        }

        private void OnLibraryManagerItemAdded(object sender, ItemChangeEventArgs e)
        {
            try
            {
                _logger.Info($"StrmMediaInfo Plugin: Item added detected. Name: '{e.Item.Name}', Path: '{e.Item.Path}'");

                if (e.Item != null && !string.IsNullOrEmpty(e.Item.Path) && e.Item.Path.EndsWith(".strm", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.Info($"SUCCESS! A .strm file was added: '{e.Item.Name}'. Trigger is working correctly.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OnLibraryManagerItemAdded event handler.");
            }
        }

        public void Dispose()
        {
            // Unsubscribe from the event to prevent memory leaks
            _libraryManager.ItemAdded -= OnLibraryManagerItemAdded;
        }
    }
}


