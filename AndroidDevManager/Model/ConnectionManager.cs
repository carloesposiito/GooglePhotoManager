using AdvancedSharpAdbClient;
using AdvancedSharpAdbClient.Models;
using AdvancedSharpAdbClient.Receivers;
using GooglePhotoTransferTool.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace GooglePhotoTransferTool.Model
{
    internal class ConnectionManager
    {
        #region "Private fields"

        private string _platformToolsPath;
        private AdbServer _adbServer;
        private AdbClient _adbClient;

        #endregion

        #region "Properties"

        /// <summary>
        /// Holds platform tools folder path.
        /// </summary>
        public string PlatformToolsPath
        {
            get
            {
                return _platformToolsPath;
            }
            set
            {
                _platformToolsPath = value;
            }
        }

        #endregion

        #region "Constructor"

        internal ConnectionManager(string platformToolsPath)
        {
            if (string.IsNullOrWhiteSpace(platformToolsPath))
            {
                throw new Exception("Invalid path to platform tools");
            }
            else
            {
                PlatformToolsPath = platformToolsPath;
            }
        }

        #endregion

        #region "Methods"

        internal bool CheckDependencies()
        {
            bool operationResult = false;

            try
            {
                // Check existing
                bool directoryExists = Directory.Exists(PlatformToolsPath);

                // If directory doesn't exists or some files are missing
                // Clear all and recreate zip file that will be extracted
                if (!directoryExists || (directoryExists && !Directory.GetFiles(PlatformToolsPath).Count().Equals(14)))
                {
                    // If platform tools folder exists but some files are missing
                    if (directoryExists)
                    {
                        Directory.Delete(PlatformToolsPath, true);
                    }

                    // Restore platform tools folder
                    FileManager.RestorePlatformTools(PlatformToolsPath);

                    // Refresh directory status
                    directoryExists = Directory.Exists(PlatformToolsPath);
                }

                operationResult = directoryExists && Directory.GetFiles(PlatformToolsPath).Count().Equals(14);
            }
            catch (Exception exception)
            {
                operationResult = false;
                MessageBox.Show("Errore durante il controllo delle dipendenze del programma:\n" +
                    $"{exception.Message}");
            }

            return operationResult;
        }

        internal async Task<bool> InitializeServiceAsync()
        {
            bool operationResult = false;

            try
            {
                using (CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(15)))
                {
                    try
                    {
                        // Create needed objects
                        _adbServer = new AdbServer();
                        _adbClient = new AdbClient();

                        // Stop server if already running
                        if (_adbServer.GetStatus().IsRunning)
                        {
                            await _adbServer.StopServerAsync();
                        }

                        // Start server
                        var startResult = await _adbServer.StartServerAsync(Path.Combine(PlatformToolsPath, "adb.exe"), true);

                        // Check result
                        operationResult = startResult.Equals(StartServerResult.Started) || startResult.Equals(StartServerResult.AlreadyRunning) || startResult.Equals(StartServerResult.RestartedOutdatedDaemon);
                    }
                    catch (OperationCanceledException)
                    {
                        throw new Exception("Timeout scaduto senza una risposta!");
                    }
                }
            }
            catch (Exception exception)
            {
                operationResult = false;
                MessageBox.Show("Errore durante l'inizializzazione del server ADB:\n" +
                    $"{exception.Message}");
            }

            return operationResult;
        }

        internal async Task<List<DeviceData>> ScanDevicesAsync()
        {
            List<DeviceData> operationResult = new List<DeviceData>();

            try
            {
                using (CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(15)))
                {
                    try
                    {
                        foreach (DeviceData device in await _adbClient.GetDevicesAsync())
                        {
                            // Check device data
                            operationResult.Add(device);
                        }
                    }
                    catch (OperationCanceledException)
                    {

                        throw new Exception("Timeout scaduto senza una risposta!");
                    }
                }
            }
            catch (Exception exception)
            {
                operationResult.Clear();
                MessageBox.Show("Errore durante la scansione dei dipositivi:\n" +
                    $"{exception.Message}");
            }

            return operationResult;
        }

        private async Task<int> PullFilesAsync(DeviceData targetDevice, List<string> fileNamesToBePulled, string localDestinationFolder, string targetDeviceFolder = "DCIM/Camera")
        {
            // Create destination folder if doesn't exist
            if (Directory.Exists(localDestinationFolder))
            {
                // Check that is empty to avoid problems
                if (Directory.GetFiles(localDestinationFolder).Count() > 0 || Directory.GetDirectories(localDestinationFolder).Count() > 0)
                {
                    throw new Exception("Cartella di destinazioe non vuota, impossible continuera! Meglio evitare problemi di foto mischiate!");
                }
            }
            else
            {
                Directory.CreateDirectory(localDestinationFolder);
            }

            // Create sync object
            var sync = new SyncService(AdbClient.Instance.EndPoint, targetDevice);

            // Pull photos
            int pulledPhotosCount = 0;
            foreach (var photo in fileNamesToBePulled)
            {
                string remotePath = $"/sdcard/{targetDeviceFolder}/{photo}";
                string localPath = Path.Combine(localDestinationFolder, photo);

                using (FileStream file = File.OpenWrite(localPath))
                {
                    await sync.PullAsync(remotePath, file, null, CancellationToken.None);
                }

                pulledPhotosCount++;
            }

            return pulledPhotosCount;
        }

        private async Task<int> PushFilesAsync(DeviceData targetDevice, List<string> filenamesToBePushed, string targetDeviceFolder = "DCIM/Camera") 
        {
            // Create sync object
            var sync = new SyncService(AdbClient.Instance.EndPoint, targetDevice);

            // Push files
            int pushedFilesCount = 0;
            List<string> notFoundFiles = new List<string>();
            foreach (var localFilename in filenamesToBePushed)
            {
                // If file doesn't exist locally save it's name
                if (!File.Exists(localFilename))
                {
                    notFoundFiles.Add(localFilename);
                }

                string fileName = Path.GetFileName(localFilename);
                string remotePath = $"/sdcard/{targetDeviceFolder}/{fileName}";

                using (FileStream fileStream = File.OpenRead(localFilename))
                {
                    await sync.PushAsync(fileStream, remotePath, UnixFileStatus.AllPermissions, DateTime.Now);
                }

                pushedFilesCount++;
            }

            // Check missing files if existing
            if (notFoundFiles.Count > 0)
            {
                string missingFiles = string.Join("\n", notFoundFiles);
                MessageBox.Show("Operazione completata. Saltati i seguenti file perchè non trovati:\n" +
                    $"{missingFiles}");
            }

            return pushedFilesCount;
        }

        private async Task<List<string>> GetFolderFilesAsync(DeviceData targetDevice, string deviceFolder = "DCIM/Camera")
        {
            // Remove slash at the start and end of the device path
            deviceFolder = deviceFolder.Trim('/');

            // Create output receiver to read list from console output
            var receiver = new ConsoleOutputReceiver();

            // Execute command
            await AdbClient.Instance.ExecuteRemoteCommandAsync($"ls -1 /sdcard/{deviceFolder}", targetDevice, receiver);

            // Return list of files from folder
            return receiver.ToString()
                .Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(f => f.Trim())
                .Where(f => !f.EndsWith("/"))       // Needed to exclude subdirectories
                .ToList();
        }

        internal async Task<TransferResult> TransferPhotos(DeviceData originDevice, DeviceData destinationDevice, bool deleteFromOriginDevice)
        {
            TransferResult tR = new TransferResult();

            bool extractionCompleted = false;
            bool pushCompleted = false;

            // Get files to be pulled from origin device
            List<string> originDevicePhotos = await GetFolderFilesAsync(originDevice);

            // Get files exsiting in destination device folder
            // In this way is possible to extract only useful photos
            List<string> destinationDevicePhotos = await GetFolderFilesAsync(destinationDevice);
            
            // Create a list with only useful photos
            // Then fill it
            List<string> photosFilenames = new List<string>();
            foreach (string originDevicePhoto in originDevicePhotos)
            {
                // Start with false flag
                bool shouldBeCopied = true;

                // Check if destination device has already current photo
                foreach (string destinationDevicePhoto in destinationDevicePhotos)
                {
                    if (originDevicePhoto.Equals(destinationDevicePhoto))
                    {
                        // Photo is already existing
                        shouldBeCopied = false;
                        break;
                    }
                }

                if (shouldBeCopied)
                {
                    // Add to list of file to be copied
                    photosFilenames.Add(originDevicePhoto);
                }
            }

            // Refresh variable of photos to be pulled
            tR.ToBePulledCount = photosFilenames.Count;
            
            // Create local temp folders
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string localDir = Path.Combine(Directory.GetCurrentDirectory(), "GooglePhotoTransfer", $"{originDevice.Model.ToUpper()}_to_{destinationDevice.Model.ToUpper()}_{timestamp}");
            tR.FolderPath = localDir;
            Directory.CreateDirectory(localDir);

            // Extract files to local folder
            tR.PulledCount = await PullFilesAsync(originDevice, photosFilenames, localDir);
            extractionCompleted = tR.PulledCount.Equals(tR.ToBePulledCount);
            if (!extractionCompleted)
            {
                MessageBoxResult result = MessageBox.Show(
                    $"Alcune foto sono state saltate durante l'estrazione dal dispositivo di origine ({tR.PulledCount}/{tR.ToBePulledCount})!\n" +
                    $"Vuoi procedere ugualmente?",
                    "Attenzione",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question
                );

                extractionCompleted = result.Equals(MessageBoxResult.Yes);
            }
            
            // Push files into destination device if ok
            if (extractionCompleted)
            {
                // Get file filenames from previous extraction directory
                List<string> filenamesToBePushed = Directory.GetFiles(localDir).ToList();
                tR.ToBePushedCount = filenamesToBePushed.Count;
                if (tR.ToBePushedCount.Equals(0))
                {
                    throw new Exception("No photos to push in destination device!");
                }

                tR.PushedCount = await PushFilesAsync(destinationDevice, filenamesToBePushed);
                pushCompleted = tR.PushedCount.Equals(tR.ToBePushedCount);
                //if (!pushCompleted)
                //{
                //    MessageBox.Show($"Some photos were skipped while transferring to destination device ({tR.PushedCount}/{tR.ToBePushedCount})!\n" +
                //        $"Photos won't be deleted from origine device for security reasons");
                //}
            }

            tR.AllFilesSynced = extractionCompleted.Equals(pushCompleted);
            
            // If all done successfully
            if (deleteFromOriginDevice)
            {
                if (tR.AllFilesSynced)
                {
                    // Delete from origin device
                    int deletedCount = await DeleteFilesAsync(originDevice, photosFilenames);
                    tR.DeleteCompleted = deletedCount.Equals(tR.PulledCount) && deletedCount.Equals(tR.PushedCount);
                }
                else
                {
                    MessageBox.Show($"Alcune foto sono state saltate durante il trasferimento al dispositivo di destinazione ({tR.PushedCount}/{tR.ToBePushedCount})!\n" +
                        $"Le foto non saranno eliminate dal dispositivo di origine per motivi di sicurezza.");
                }
            }

            return tR;
        }

        private async Task<int> DeleteFilesAsync(DeviceData targetDevice, List<string> filenamesToBeDeleted, string targetDeviceFolder = "DCIM/Camera")
        {
            int deletedFilesCount = 0;
            List<string> notFoundFiles = new List<string>();

            foreach (var filename in filenamesToBeDeleted)
            {
                string fileName = Path.GetFileName(filename);
                string remotePath = $"/sdcard/{targetDeviceFolder}/{fileName}";

                var receiver = new ConsoleOutputReceiver();

                await AdbClient.Instance.ExecuteRemoteCommandAsync($"rm -f \"{remotePath}\"", targetDevice, receiver);

                // If there's output probably it is an error (file not existing?)
                if (!string.IsNullOrWhiteSpace(receiver.ToString()))
                {
                    notFoundFiles.Add(fileName);
                }
                else
                {
                    deletedFilesCount++;
                }
            }

            if (notFoundFiles.Count > 0)
            {
                string missingFiles = string.Join("\n", notFoundFiles);
                MessageBox.Show("Operazioe completata. Le seguenti foto non sono state trovate nel dispositivo:\n" +
                    $"{missingFiles}");
            }

            return deletedFilesCount;
        }

        internal async Task<bool> RestartService()
        {
            bool operationResult = false;

            using (CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(15)))
            {
                try
                {
                    // Stop server if already running
                    if (_adbServer.GetStatus().IsRunning)
                    {
                        await _adbServer.StopServerAsync();
                    }

                    // Start server
                    var startResult = await _adbServer.StartServerAsync(Path.Combine(PlatformToolsPath, "adb.exe"), true);

                    // Check result
                    operationResult = startResult.Equals(StartServerResult.Started) || startResult.Equals(StartServerResult.AlreadyRunning) || startResult.Equals(StartServerResult.RestartedOutdatedDaemon);
                }
                catch (OperationCanceledException)
                {
                    throw new Exception("Timeout scaduto senza una risposta!");
                }
            }

            return operationResult;
        }

        internal async Task<string> ConnectWirelessAsync(string deviceIp, string devicePort)
        {
            string operationResult = string.Empty;

            try
            {
                string endpoint = $"{deviceIp}:{devicePort}";
                operationResult = await AdbClient.Instance.ConnectAsync(endpoint);
            }
            catch (Exception exception)
            {
                operationResult = $"Errore durante la connessione al dispositivo {deviceIp}:{devicePort}\n" +
                    $"{exception.Message}";
            }
            return operationResult;
        }

        internal async Task<string> PairWirelessAsync(string deviceIp, string devicePort, string pairingCode)
        {
            string operationResult = string.Empty;

            try
            {
                string endpoint = $"{deviceIp}:{devicePort}";
                operationResult = await AdbClient.Instance.PairAsync(endpoint, pairingCode);
            }
            catch (Exception exception)
            {
                operationResult = $"Errore durante l'abbinamento del dispositivo {deviceIp}:{devicePort}\n" +
                    $"{exception.Message}";
            }
            return operationResult;
        }

        internal async Task KillService()
        {
            try
            {
                await _adbServer.StopServerAsync();
            }
            catch (Exception exception)
            {
                MessageBox.Show("Errore durante lo stop del server ADB:\n" +
                    $"{exception.Message}");
            }
        }

        #endregion
    }
}
