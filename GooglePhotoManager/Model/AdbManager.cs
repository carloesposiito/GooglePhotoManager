using AdvancedSharpAdbClient;
using AdvancedSharpAdbClient.Models;
using AdvancedSharpAdbClient.Receivers;
using GooglePhotoManager.Utils;
using System.IO.Compression;

namespace GooglePhotoManager.Model
{
    /// <summary>
    /// Class related to ADB connection.
    /// </summary>
    internal class AdbManager
    {
        #region "Constants about unlimited device name and product"

        internal const string UNLIMITED_BK_DEVICE_NAME = "Pixel 5 (redfin)";
        internal const string UNLIMITED_BK_DEVICE_MODEL = "Pixel_5";
        internal const string UNLIMITED_BK_DEVICE_PRODUCT = "redfin";
        internal const int USERSPACE_CHANGE_TIME = 6000;

        #endregion

        #region "Private fields"

        private string _baseDir = string.Empty;
        private string _platformToolsZipFilename = string.Empty;
        private string _platformToolsDir = string.Empty;
        private AdbServer _adbServer;
        private AdbClient _adbClient;
        private List<DeviceData> _devices = new List<DeviceData>();
        private List<DeviceData> _originDevices = new List<DeviceData>();
        private DeviceData _unlimitedDevice;
        private Dictionary<string, MyUser> _users = new();
        private MyUser _currentUser = null;

        #endregion

        #region "Properties"

        public List<DeviceData> Devices { get => _devices; set => _devices = value; }
        public List<DeviceData> OriginDevices { get => _originDevices; set => _originDevices = value; }
        public DeviceData UnlimitedDevice { get => _unlimitedDevice; set => _unlimitedDevice = value; }
        internal Dictionary<string, MyUser> Users { get => _users; set => _users = value; }
        internal MyUser CurrentUser { get => _currentUser; set => _currentUser = value; }

        #endregion

        #region "Methods"

        /// <summary>
        /// Initializes ADB and returns true if ready to work.<br/>
        /// </summary>
        internal async Task<bool> Initialize()
        {
            Console.WriteLine("Servizio ADB");

            // Solve directories path
            _baseDir = Directory.GetCurrentDirectory();
            _platformToolsZipFilename = $"{_baseDir}\\PlatformTools.zip";
            _platformToolsDir = $"{_baseDir}\\PlatformTools";

            // Create adb objects
            _adbServer = new AdbServer();
            _adbClient = new AdbClient();

            if (CheckDependencies() && await StartServiceAsync())
            {
                Console.WriteLine("Inizializzazione completata e servizio avviato");

                // Write current ADB settings
                Console.WriteLine($"Dispostivo di backup: {UNLIMITED_BK_DEVICE_NAME}");
                Console.WriteLine();
                return true;
            }
            else
            {
                Console.WriteLine("Inizializzazione non riuscita");
                Console.WriteLine();
                return false;
            }
        }

        /// <summary>
        /// Check if all needed files to make ADB working exist.<br/>
        /// Handles exception writing to console and returning false.
        /// </summary>
        private bool CheckDependencies()
        {
            bool checkResult = false;

            try
            {
                bool recreateFolder = true;

                // If platform tools folder exists, check that all files are existing
                if (Directory.Exists(_platformToolsDir))
                {
                    if (Directory.GetFiles(_platformToolsDir).Length.Equals(14))
                    {
                        // Everything is ok
                        recreateFolder = false;
                    }
                    else
                    {
                        // If something is missing, delete folder
                        Directory.Delete(_platformToolsDir, true);
                    }
                }

                // If needed to recreate platform tools folder
                if (recreateFolder)
                {
                    // Create directory (not existing atm) where files will be extracted
                    Directory.CreateDirectory(_platformToolsDir);

                    // Recreate zip file from resources (if not existing)
                    if (!File.Exists(_platformToolsZipFilename))
                    {
                        var platformToolsZip = Properties.Resources.PlatformTools;
                        File.WriteAllBytes(_platformToolsZipFilename, platformToolsZip);
                    }

                    // If zip file created successfully
                    if (File.Exists(_platformToolsZipFilename))
                    {
                        #region "Extract zip into previously created folder"

                        using (var archive = ZipFile.OpenRead(_platformToolsZipFilename))
                        {
                            foreach (var entry in archive.Entries)
                            {
                                string destinationPath = Path.GetFullPath(Path.Combine(_platformToolsDir, entry.FullName));

                                // Zip slip protection
                                if (!destinationPath.StartsWith(Path.GetFullPath(_platformToolsDir), StringComparison.OrdinalIgnoreCase))
                                {
                                    throw new Exception("File zip non sicuro");
                                }

                                // Directory
                                if (string.IsNullOrEmpty(entry.Name))
                                {
                                    Directory.CreateDirectory(destinationPath);
                                    continue;
                                }

                                string directory = Path.GetDirectoryName(destinationPath);
                                if (!string.IsNullOrEmpty(directory))
                                {
                                    Directory.CreateDirectory(directory);
                                }

                                // Extract with overwrite flag set to true just in case
                                entry.ExtractToFile(destinationPath, true);
                            }
                        }

                        #endregion
                    }

                    // If platform tools zip exists delete it
                    if (File.Exists(_platformToolsZipFilename))
                    {
                        File.Delete(_platformToolsZipFilename);
                    }
                }

                // Update result
                checkResult = Directory.Exists(_platformToolsDir) && Directory.GetFiles(_platformToolsDir).Length.Equals(14);
            }
            catch (Exception exception)
            {
                checkResult = false;
                Utilities.DisplayException(GetType().ToString(), "CheckDependencies", exception.Message);
            }

            return checkResult;
        }

        /// <summary>
        /// Starts ADB service and returns true if successful.<br/>
        /// Handles exception writing to console and returning false.
        /// </summary>
        private async Task<bool> StartServiceAsync()
        {
            bool operationResult = false;

            try
            {
                using (CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(15)))
                {
                    try
                    {
                        // Stop server if already running
                        if (_adbServer.GetStatus().IsRunning)
                        {
#if DEBUG
                            return true;
#endif
                            await _adbServer.StopServerAsync();
                        }

                        string adbExePath = Path.Combine(_platformToolsDir, "adb.exe");
                        if (!File.Exists(adbExePath))
                        {
                            throw new Exception("File \"adb.exe\" non trovato");
                        }

                        // Start server
                        var startResult = await _adbServer.StartServerAsync(adbExePath, true);

                        // Check result
                        operationResult = startResult.Equals(StartServerResult.Started) || startResult.Equals(StartServerResult.AlreadyRunning) || startResult.Equals(StartServerResult.RestartedOutdatedDaemon);
                    }
                    catch (OperationCanceledException)
                    {
                        throw new Exception("Timeout scaduto");
                    }
                }
            }
            catch (Exception exception)
            {
                operationResult = false;
                Utilities.DisplayException(GetType().ToString(), "StartServiceAsync", exception.Message);
            }

            return operationResult;
        }

        /// <summary>
        /// Scan devices and populates <see cref="Devices"/> list.<br/>
        /// If unlimited backup device is found, populates <see cref="UnlimitedDevice"/> object.<br/>
        /// Handles exceptions.
        /// </summary>
        internal async Task ScanDevicesAsync()
        {
            _devices.Clear();
            _unlimitedDevice = null;

            try
            {
                using (CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(15)))
                {
                    try
                    {
                        foreach (DeviceData device in await _adbClient.GetDevicesAsync())
                        {
                            if (device.State.Equals(DeviceState.Online))
                            {
                                if (device.Model.Equals(UNLIMITED_BK_DEVICE_MODEL) && device.Product.Equals(UNLIMITED_BK_DEVICE_PRODUCT))
                                {
                                    _unlimitedDevice = device;
                                }
                                else
                                {
                                    // Add to origin devices list
                                    _originDevices.Add(device);
                                }

                                // Add to all devices list
                                _devices.Add(device);
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        throw new Exception("Timeout scaduto");
                    }
                }
            }
            catch (Exception exception)
            {
                _devices.Clear();
                _unlimitedDevice = null;
                Utilities.DisplayException(GetType().ToString(), "ScanDevicesAsync", exception.Message);
            }
        }

        /// <summary>
        /// Connects a device with passed IP and port via ADB wireless.
        /// </summary>
        /// <returns>String containing operation result description.</returns>
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
                Utilities.DisplayException(GetType().ToString(), "ConnectWirelessAsync", exception.Message);
            }
            return operationResult;
        }

        /// <summary>
        /// Pairs a device with passed IP and port via ADB wireless.
        /// </summary>
        /// <returns>String containing operation result description.</returns>
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
                Utilities.DisplayException(GetType().ToString(), "PairWirelessAsync", exception.Message);
            }
            return operationResult;
        }

        /// <summary>
        /// Kills ADB service.
        /// </summary>
        internal async Task KillServiceAsync()
        {
            try
            {
                await _adbServer.StopServerAsync();
            }
            catch (Exception exception)
            {
                Utilities.DisplayException(GetType().ToString(), "KillServiceAsync", exception.Message);
            }
        }

        /// <summary>
        /// Returns unlimited backup device users.<br/>
        /// If passed bool is true, sets current user automatically according to the active user space on device.<br/>
        /// Handles exception writing message to console and returning an empty list.
        /// </summary>
        internal async Task GetUsersAsync()
        {
            _users.Clear();
            _currentUser = null;

            try
            {
                if (_unlimitedDevice != null)
                {
                    using (CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(15)))
                    {
                        try
                        {
                            var receiver = new ConsoleOutputReceiver();

                            await _adbClient.ExecuteRemoteCommandAsync(
                                "pm list users",
                                _unlimitedDevice,
                                receiver,
                                cts.Token
                            );

                            var output = receiver.ToString();

                            #region "Parse output into users"

                            foreach (string outputLine in output.Split('\n'))
                            {
                                string line = outputLine.Trim();
                                if (line.StartsWith("UserInfo{"))
                                {
                                    int openBrace = line.IndexOf('{');
                                    int firstColon = line.IndexOf(':', openBrace + 1);
                                    int secondColon = line.IndexOf(':', firstColon + 1);

                                    if (openBrace < 0 || firstColon < 0 || secondColon < 0)
                                    {
                                        continue;
                                    }

                                    // Extract user id
                                    string id = line.Substring(openBrace + 1, firstColon - openBrace - 1);

                                    // Extract user name
                                    string name = line.Substring(firstColon + 1, secondColon - firstColon - 1);

                                    // Create user starting from device user space name
                                    if (!string.IsNullOrWhiteSpace(name) && !_users.ContainsKey(name))
                                    {
                                        MyUser myUser = new(name, id);
                                        _users.Add(name, myUser);
                                    }
                                }
                            }

                            #endregion
                        }
                        catch (OperationCanceledException)
                        {
                            throw new Exception("Timeout scaduto");
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                _users.Clear();
                _currentUser = null;
                Utilities.DisplayException(GetType().ToString(), "GetUsersAsync", exception.Message);
            }
        }

        /// <summary>
        /// Returns current user active on unlimited backup device.<br/>
        /// Handles exceptions showing message and returning a null object.
        /// </summary>
        internal async Task<MyUser> GetCurrentUserAsync()
        {
            MyUser currentUser = null;

            try
            {
                if (_unlimitedDevice != null)
                {
                    using (CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(15)))
                    {
                        try
                        {
                            var receiver = new ConsoleOutputReceiver();

                            await _adbClient.ExecuteRemoteCommandAsync(
                                "dumpsys activity activities",
                                _unlimitedDevice,
                                receiver,
                                cts.Token
                            );

                            var output = receiver.ToString();

                            #region "Check current active user space"

                            foreach (string outputLine in output.Split('\n'))
                            {
                                string line = outputLine.Trim();

                                if (line.StartsWith("mCurrentUser="))
                                {
                                    int openBrace = line.IndexOf('{');
                                    int firstColon = line.IndexOf(':', openBrace + 1);
                                    int secondColon = line.IndexOf(':', firstColon + 1);

                                    if (openBrace < 0 || firstColon < 0 || secondColon < 0)
                                    {
                                        continue;
                                    }

                                    // Extract user id and name, then create object to be returned
                                    string id = line.Substring(openBrace + 1, firstColon - openBrace - 1);
                                    string name = line.Substring(firstColon + 1, secondColon - firstColon - 1);
                                    currentUser = new(id, name);
                                    break;
                                }
                            }

                            #endregion
                        }
                        catch (OperationCanceledException)
                        {
                            throw new Exception("Timeout scaduto");
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                currentUser = null;
                Utilities.DisplayException(GetType().ToString(), "GetCurrentUsersAsync", exception.Message);
            }

            return currentUser;
        }

        /// <summary>
        /// Sets current user chaning user space on backup device.<br/>
        /// Returns true if set successfully, otherwise false.<br/>
        /// Handles exception writing message to console and setting current user to null.
        /// </summary>
        internal async Task<bool> SetUserAsync(MyUser currentUser)
        {
            bool operationResult = false;

            try
            {
                if (currentUser != null)
                {
                    using (CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(15)))
                    {
                        try
                        {
                            var receiver = new ConsoleOutputReceiver();

                            // Change user space on unlimited backup device according to passed user ID
                            await _adbClient.ExecuteRemoteCommandAsync(
                                $"am switch-user {currentUser.Id}",
                                _unlimitedDevice,
                                receiver,
                                cts.Token
                            );

                            // Write command output if not empty
                            string outputResult = receiver.ToString();
                            if (!string.IsNullOrWhiteSpace(outputResult))
                            {
                                Console.Write(outputResult);
                            }

                            await Task.Delay(USERSPACE_CHANGE_TIME);

                            // Check active user space on device and also check if its name is equals to passed user one
                            MyUser fromDevice = await GetCurrentUserAsync();
                            while (_currentUser != fromDevice)
                            {
                                await Task.Delay(250);
                                fromDevice = await GetCurrentUserAsync();
                                Console.WriteLine("Impostazione utente in corso...");
                            }

                            operationResult = true;
                        }
                        catch (OperationCanceledException)
                        {
                            throw new Exception("Timeout scaduto");
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                operationResult = false;
                _currentUser = null;
                Utilities.DisplayException(GetType().ToString(), "SetUserAsync", exception.Message);
            }

            return operationResult;
        }

        #endregion
    }
}
