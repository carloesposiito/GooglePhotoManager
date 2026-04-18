using AdvancedSharpAdbClient;
using AdvancedSharpAdbClient.Models;
using AdvancedSharpAdbClient.Receivers;
using GooglePhotoManager.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace GooglePhotoManager;

public class AdbService
{
    private const int USERSPACE_CHANGE_TIME = 6000;

    private AdbServer _adbServer = null!;
    private AdbClient _adbClient = null!;
    private string _adbPath = string.Empty;
    private bool _initialized;
    private ConfigManager _configManager = null!;

    private List<DeviceData> _devices = new();
    private List<DeviceData> _originDevices = new();
    private DeviceData? _backupDevice;
    private Dictionary<string, MyUser> _users = new();
    private MyUser? _currentUser;

    private static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    private static string AdbExecutableName => IsWindows ? "adb.exe" : "adb";

    public bool IsInitialized => _initialized;
    public ConfigManager Config => _configManager;
    public List<DeviceData> Devices => _devices;
    public List<DeviceData> OriginDevices => _originDevices;
    public DeviceData? BackupDevice => _backupDevice;
    public Dictionary<string, MyUser> Users => _users;
    public MyUser? CurrentUser { get => _currentUser; set => _currentUser = value; }
    public string BackupDeviceName => _configManager?.BackupDeviceName ?? "Non configurato";
    public bool IsBackupDeviceConnected => _backupDevice != null;

    public async Task<bool> InitializeAsync()
    {
        _configManager = new ConfigManager();
        _adbServer = new AdbServer();
        _adbClient = new AdbClient();

        if (!FindAdb())
        {
            _initialized = false;
            return false;
        }

        _initialized = await StartServerAsync();
        return _initialized;
    }

    private bool FindAdb()
    {
        if (IsWindows)
        {
            string platformToolsDir = Path.Combine(Directory.GetCurrentDirectory(), "PlatformTools");
            string path = Path.Combine(platformToolsDir, AdbExecutableName);
            if (File.Exists(path))
            {
                _adbPath = path;
                return true;
            }
        }

        string[] possiblePaths =
        {
            "/usr/bin/adb",
            "/usr/local/bin/adb",
            "/opt/android-sdk/platform-tools/adb",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Android/Sdk/platform-tools/adb")
        };

        foreach (string path in possiblePaths)
        {
            if (File.Exists(path))
            {
                _adbPath = path;
                return true;
            }
        }

        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "which",
                    Arguments = "adb",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            string output = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit();

            if (process.ExitCode == 0 && !string.IsNullOrWhiteSpace(output) && File.Exists(output))
            {
                _adbPath = output;
                return true;
            }
        }
        catch { }

        return false;
    }

    private async Task<bool> StartServerAsync()
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));

            if (_adbServer.GetStatus().IsRunning)
                return true;

            if (string.IsNullOrEmpty(_adbPath) || !File.Exists(_adbPath))
                return false;

            var result = await _adbServer.StartServerAsync(_adbPath, true);
            return result == StartServerResult.Started ||
                   result == StartServerResult.AlreadyRunning ||
                   result == StartServerResult.RestartedOutdatedDaemon;
        }
        catch
        {
            return false;
        }
    }

    public async Task ScanDevicesAsync()
    {
        _devices.Clear();
        _originDevices.Clear();
        _backupDevice = null;

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));

            foreach (var device in await _adbClient.GetDevicesAsync())
            {
                if (device.State == DeviceState.Online)
                {
                    if (_configManager.IsBackupDevice(device.Model, device.Product))
                    {
                        _backupDevice = device;
                    }
                    else
                    {
                        _originDevices.Add(device);
                    }

                    _devices.Add(device);
                }
            }
        }
        catch
        {
            _devices.Clear();
            _originDevices.Clear();
            _backupDevice = null;
        }
    }

    public async Task<string> ConnectWirelessAsync(string ip, string port)
    {
        try
        {
            string endpoint = $"{ip}:{port}";
            return await AdbClient.Instance.ConnectAsync(endpoint);
        }
        catch (Exception ex)
        {
            return $"Errore: {ex.Message}";
        }
    }

    public async Task<string> PairWirelessAsync(string ip, string port, string pairingCode)
    {
        try
        {
            string endpoint = $"{ip}:{port}";
            return await AdbClient.Instance.PairAsync(endpoint, pairingCode);
        }
        catch (Exception ex)
        {
            return $"Errore: {ex.Message}";
        }
    }

    public async Task StopAsync()
    {
        try
        {
            await _adbServer.StopServerAsync();
        }
        catch { }
    }

    // --- User management ---

    public async Task GetUsersAsync(DeviceData? device = null)
    {
        _users.Clear();
        _currentUser = null;
        var targetDevice = device ?? _backupDevice;

        try
        {
            if (targetDevice == null) return;

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            var receiver = new ConsoleOutputReceiver();

            await _adbClient.ExecuteRemoteCommandAsync("pm list users", targetDevice, receiver, cts.Token);
            var output = receiver.ToString();

            foreach (string outputLine in output.Split('\n'))
            {
                string line = outputLine.Trim();
                if (line.StartsWith("UserInfo{"))
                {
                    int openBrace = line.IndexOf('{');
                    int firstColon = line.IndexOf(':', openBrace + 1);
                    int secondColon = line.IndexOf(':', firstColon + 1);

                    if (openBrace < 0 || firstColon < 0 || secondColon < 0)
                        continue;

                    string id = line.Substring(openBrace + 1, firstColon - openBrace - 1);
                    string name = line.Substring(firstColon + 1, secondColon - firstColon - 1);

                    if (!string.IsNullOrWhiteSpace(name) && !_users.ContainsKey(name))
                    {
                        _users.Add(name, new MyUser(name, id));
                    }
                }
            }
        }
        catch
        {
            _users.Clear();
            _currentUser = null;
        }
    }

    public async Task<MyUser?> GetCurrentUserAsync(DeviceData? device = null)
    {
        var targetDevice = device ?? _backupDevice;
        try
        {
            if (targetDevice == null) return null;

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            var receiver = new ConsoleOutputReceiver();

            await _adbClient.ExecuteRemoteCommandAsync("dumpsys activity activities", targetDevice, receiver, cts.Token);
            var output = receiver.ToString();

            foreach (string outputLine in output.Split('\n'))
            {
                string line = outputLine.Trim();

                if (line.StartsWith("mCurrentUser="))
                {
                    int openBrace = line.IndexOf('{');
                    int firstColon = line.IndexOf(':', openBrace + 1);
                    int secondColon = line.IndexOf(':', firstColon + 1);

                    if (openBrace < 0 || firstColon < 0 || secondColon < 0)
                        continue;

                    string id = line.Substring(openBrace + 1, firstColon - openBrace - 1);
                    string name = line.Substring(firstColon + 1, secondColon - firstColon - 1);
                    return new MyUser(name, id);
                }
            }
        }
        catch { }

        return null;
    }

    public async Task<bool> SetUserAsync(MyUser targetUser, DeviceData? device = null)
    {
        var targetDevice = device ?? _backupDevice;
        try
        {
            if (targetUser == null || targetDevice == null) return false;

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var receiver = new ConsoleOutputReceiver();

            await _adbClient.ExecuteRemoteCommandAsync($"am switch-user {targetUser.Id}", targetDevice, receiver, cts.Token);

            await Task.Delay(USERSPACE_CHANGE_TIME);

            // Verify the switch
            var currentUser = await GetCurrentUserAsync();
            if (currentUser != null && currentUser.Name == targetUser.Name)
            {
                _currentUser = targetUser;
                return true;
            }
        }
        catch { }

        return false;
    }

    // --- Device management ---

    public async Task<string> CreateUserAsync(DeviceData device, string userName)
    {
        try
        {
            var receiver = new ConsoleOutputReceiver();
            await AdbClient.Instance.ExecuteRemoteCommandAsync($"pm create-user \"{userName}\"", device, receiver);
            return receiver.ToString().Trim();
        }
        catch (Exception ex)
        {
            return $"Errore: {ex.Message}";
        }
    }

    public async Task<string> RemoveUserAsync(DeviceData device, string userId)
    {
        try
        {
            var receiver = new ConsoleOutputReceiver();
            await AdbClient.Instance.ExecuteRemoteCommandAsync($"pm remove-user {userId}", device, receiver);
            return receiver.ToString().Trim();
        }
        catch (Exception ex)
        {
            return $"Errore: {ex.Message}";
        }
    }

    public async Task<string> RebootDeviceAsync(DeviceData device)
    {
        try
        {
            var receiver = new ConsoleOutputReceiver();
            await AdbClient.Instance.ExecuteRemoteCommandAsync("reboot", device, receiver);
            return "Comando di riavvio inviato";
        }
        catch (Exception ex)
        {
            return $"Errore: {ex.Message}";
        }
    }

    public async Task<Dictionary<string, string>> GetDeviceInfoAsync(DeviceData device)
    {
        var info = new Dictionary<string, string>();

        info["Modello"] = device.Model;
        info["Prodotto"] = device.Product;
        info["Seriale"] = device.Serial;

        try
        {
            // Android version
            var r1 = new ConsoleOutputReceiver();
            await AdbClient.Instance.ExecuteRemoteCommandAsync("getprop ro.build.version.release", device, r1);
            info["Android"] = r1.ToString().Trim();

            // API level
            var r2 = new ConsoleOutputReceiver();
            await AdbClient.Instance.ExecuteRemoteCommandAsync("getprop ro.build.version.sdk", device, r2);
            info["API"] = r2.ToString().Trim();

            // Battery
            var r3 = new ConsoleOutputReceiver();
            await AdbClient.Instance.ExecuteRemoteCommandAsync("dumpsys battery | grep level", device, r3);
            string battLine = r3.ToString().Trim();
            if (battLine.Contains("level"))
            {
                string level = battLine.Split(':').LastOrDefault()?.Trim() ?? "?";
                info["Batteria"] = $"{level}%";
            }

            // Storage
            var r4 = new ConsoleOutputReceiver();
            await AdbClient.Instance.ExecuteRemoteCommandAsync("df /data | tail -1", device, r4);
            string dfLine = r4.ToString().Trim();
            if (!string.IsNullOrEmpty(dfLine))
            {
                var parts = dfLine.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 4)
                {
                    info["Storage"] = $"Usato {parts[2]} / Totale {parts[1]}";
                }
            }
        }
        catch { }

        return info;
    }

    public async Task<string> TakeScreenshotAsync(DeviceData device, string localPath)
    {
        try
        {
            string remotePath = "/sdcard/screenshot_temp.png";
            var receiver = new ConsoleOutputReceiver();
            await AdbClient.Instance.ExecuteRemoteCommandAsync($"screencap -p {remotePath}", device, receiver);

            // Pull to local
            var sync = new SyncService(AdbClient.Instance.EndPoint, device);
            using (var fs = File.OpenWrite(localPath))
            {
                await sync.PullAsync(remotePath, fs, null, CancellationToken.None);
            }

            // Cleanup remote
            await AdbClient.Instance.ExecuteRemoteCommandAsync($"rm {remotePath}", device, new ConsoleOutputReceiver());

            return localPath;
        }
        catch (Exception ex)
        {
            return $"Errore: {ex.Message}";
        }
    }

    // --- File operations ---

    public async Task<List<string>> GetRootFoldersAsync(DeviceData device)
    {
        var receiver = new ConsoleOutputReceiver();
        await AdbClient.Instance.ExecuteRemoteCommandAsync("ls -1 /sdcard/", device, receiver);

        var items = receiver.ToString()
            .Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(f => f.Trim())
            .Where(f => !string.IsNullOrWhiteSpace(f))
            .ToList();

        var folders = new List<string>();
        foreach (var item in items)
        {
            var checkReceiver = new ConsoleOutputReceiver();
            await AdbClient.Instance.ExecuteRemoteCommandAsync($"test -d /sdcard/\"{item}\" && echo 'DIR'", device, checkReceiver);
            if (checkReceiver.ToString().Trim() == "DIR")
            {
                folders.Add(item);
            }
        }

        return folders;
    }

    public async Task<List<string>> GetFolderFilesAsync(DeviceData device, string deviceFolder = "DCIM/Camera")
    {
        deviceFolder = deviceFolder.Trim('/');
        var receiver = new ConsoleOutputReceiver();
        await AdbClient.Instance.ExecuteRemoteCommandAsync($"ls -1 /sdcard/{deviceFolder}", device, receiver);

        return receiver.ToString()
            .Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(f => f.Trim())
            .Where(f => !f.EndsWith("/"))
            .ToList();
    }

    public async Task<int> PushToDocumentsAsync(DeviceData device, List<string> localFilePaths, IProgress<(int current, int total, string fileName)>? progress = null)
    {
        return await PushFilesAsync(device, localFilePaths, "Documents", progress);
    }

    public async Task<int> PushFilesAsync(DeviceData device, List<string> filePaths, string targetFolder = "DCIM/Camera", IProgress<(int current, int total, string fileName)>? progress = null)
    {
        var sync = new SyncService(AdbClient.Instance.EndPoint, device);
        int pushedCount = 0;
        int total = filePaths.Count;

        foreach (var localFilename in filePaths)
        {
            string fileName = Path.GetFileName(localFilename);
            progress?.Report((pushedCount + 1, total, fileName));

            if (!File.Exists(localFilename))
                continue;

            string remotePath = $"/sdcard/{targetFolder}/{fileName}";

            using (FileStream fileStream = File.OpenRead(localFilename))
            {
                await sync.PushAsync(fileStream, remotePath, UnixFileStatus.AllPermissions, DateTime.Now);
            }

            pushedCount++;
        }

        return pushedCount;
    }

    public async Task<int> PullFilesAsync(DeviceData device, List<string> fileNames, string localDestFolder, string deviceFolder = "DCIM/Camera", IProgress<(int current, int total, string fileName)>? progress = null)
    {
        if (Directory.Exists(localDestFolder))
        {
            if (Directory.GetFiles(localDestFolder).Length > 0 || Directory.GetDirectories(localDestFolder).Length > 0)
                throw new Exception("Cartella di destinazione non vuota");
        }
        else
        {
            Directory.CreateDirectory(localDestFolder);
        }

        var sync = new SyncService(AdbClient.Instance.EndPoint, device);
        int pulledCount = 0;
        int total = fileNames.Count;

        foreach (var file in fileNames)
        {
            string remotePath = $"/sdcard/{deviceFolder}/{file}";
            string localPath = Path.Combine(localDestFolder, file);

            progress?.Report((pulledCount + 1, total, file));

            using (FileStream fs = File.OpenWrite(localPath))
            {
                await sync.PullAsync(remotePath, fs, null, CancellationToken.None);
            }

            pulledCount++;
        }

        return pulledCount;
    }

    public async Task<int> DeleteFilesAsync(DeviceData device, List<string> fileNames, string deviceFolder = "DCIM/Camera", IProgress<(int current, int total, string fileName)>? progress = null)
    {
        int deletedCount = 0;
        int total = fileNames.Count;

        foreach (var filename in fileNames)
        {
            string fileName = Path.GetFileName(filename);
            progress?.Report((deletedCount + 1, total, fileName));

            string remotePath = $"/sdcard/{deviceFolder}/{fileName}";
            var receiver = new ConsoleOutputReceiver();
            await AdbClient.Instance.ExecuteRemoteCommandAsync($"rm -f \"{remotePath}\"", device, receiver);

            if (string.IsNullOrWhiteSpace(receiver.ToString()))
                deletedCount++;
        }

        return deletedCount;
    }

    public async Task<TransferResult> BackupFolderAsync(DeviceData device, string deviceFolder, IProgress<(int current, int total, string fileName)>? progress = null)
    {
        var result = new TransferResult();

        try
        {
            List<string> files = await GetFolderFilesAsync(device, deviceFolder);
            result.ToBePulledCount = files.Count;

            if (files.Count == 0)
            {
                result.FolderPath = string.Empty;
                return result;
            }

            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string folderName = deviceFolder.Replace("/", "_").Replace("\\", "_");
            string localDir = Path.Combine(
                Directory.GetCurrentDirectory(),
                "Backup",
                $"{device.Model}_{folderName}_{timestamp}"
            );
            result.FolderPath = localDir;

            result.PulledCount = await PullFilesAsync(device, files, localDir, deviceFolder, progress);
            result.AllFilesSynced = result.PulledCount == result.ToBePulledCount;
        }
        catch
        {
            // Errore durante il backup
        }

        return result;
    }

    public async Task<TransferResult> TransferPhotosAsync(DeviceData originDevice, DeviceData destinationDevice, bool deleteFromOrigin, IProgress<(int current, int total, string fileName)>? progress = null)
    {
        var result = new TransferResult();

        // Get files from both devices
        List<string> originPhotos = await GetFolderFilesAsync(originDevice);
        List<string> destPhotos = await GetFolderFilesAsync(destinationDevice);

        // Filter: only photos NOT already on destination
        var destSet = new HashSet<string>(destPhotos);
        List<string> photosToTransfer = originPhotos.Where(p => !destSet.Contains(p)).ToList();

        result.ToBePulledCount = photosToTransfer.Count;

        if (photosToTransfer.Count == 0)
        {
            result.AllFilesSynced = true;
            return result;
        }

        // Create temp folder
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string localDir = Path.Combine(
            Directory.GetCurrentDirectory(),
            "GooglePhotoTransfer",
            $"{originDevice.Model.ToUpper()}_to_{destinationDevice.Model.ToUpper()}_{timestamp}"
        );
        result.FolderPath = localDir;
        Directory.CreateDirectory(localDir);

        // Pull from origin to local
        result.PulledCount = await PullFilesAsync(originDevice, photosToTransfer, localDir, "DCIM/Camera", progress);
        bool extractionCompleted = result.PulledCount == result.ToBePulledCount;

        // Push from local to destination
        if (result.PulledCount > 0)
        {
            List<string> filesToPush = Directory.GetFiles(localDir).ToList();
            result.ToBePushedCount = filesToPush.Count;

            if (filesToPush.Count > 0)
            {
                result.PushedCount = await PushFilesAsync(destinationDevice, filesToPush, "DCIM/Camera", progress);
            }
        }

        result.AllFilesSynced = extractionCompleted && result.PushedCount == result.ToBePushedCount;

        // Delete from origin if requested and all synced
        if (deleteFromOrigin && result.AllFilesSynced)
        {
            int deletedCount = await DeleteFilesAsync(originDevice, photosToTransfer, "DCIM/Camera", progress);
            result.DeleteCompleted = deletedCount == result.PulledCount;
        }

        return result;
    }
}
