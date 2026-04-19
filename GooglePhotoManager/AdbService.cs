using AdvancedSharpAdbClient;
using AdvancedSharpAdbClient.Models;
using AdvancedSharpAdbClient.Receivers;
using GooglePhotoManager.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace GooglePhotoManager;

public class AdbService
{
    #region Campi privati

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

    #endregion

    #region Proprietà

    // Indica se il server ADB e' stato inizializzato
    public bool IsInitialized => _initialized;

    // Gestore della configurazione
    public ConfigManager Config => _configManager;

    // Lista di tutti i dispositivi connessi
    public List<DeviceData> Devices => _devices;

    // Dispositivi sorgente (tutti tranne il backup)
    public List<DeviceData> OriginDevices => _originDevices;

    // Dispositivo di backup configurato
    public DeviceData? BackupDevice => _backupDevice;

    // Dizionario degli utenti trovati sul dispositivo
    public Dictionary<string, MyUser> Users => _users;

    // Utente attualmente attivo sul dispositivo
    public MyUser? CurrentUser { get => _currentUser; set => _currentUser = value; }

    // Nome descrittivo del dispositivo di backup
    public string BackupDeviceName => _configManager?.BackupDeviceName ?? "Non configurato";

    // Indica se il dispositivo di backup e' connesso
    public bool IsBackupDeviceConnected => _backupDevice != null;

    // Indica se la modalita' simulazione e' attiva
    public bool IsSimulation { get; private set; }

    #endregion

    #region Metodi

    // Inizializza il server ADB e carica la configurazione
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

    // Estrae PlatformTools.zip dalle risorse embedded nella cartella locale
    private bool ExtractPlatformTools(string platformToolsDir)
    {
        try
        {
            byte[]? zipData = Properties.Resources.PlatformTools;
            if (zipData == null || zipData.Length == 0)
                return false;

            // Scrive lo zip in un file temporaneo e lo estrae
            string tempZip = Path.Combine(Path.GetTempPath(), "PlatformTools.zip");
            File.WriteAllBytes(tempZip, zipData);
            ZipFile.ExtractToDirectory(tempZip, platformToolsDir, true);
            File.Delete(tempZip);

            // Su Linux/macOS rende adb eseguibile
            if (!IsWindows)
            {
                string adbPath = Path.Combine(platformToolsDir, "adb");
                if (File.Exists(adbPath))
                {
                    Process.Start("chmod", $"+x \"{adbPath}\"")?.WaitForExit();
                }
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    // Cerca l'eseguibile ADB nel sistema
    private bool FindAdb()
    {
        // Cerca nella cartella PlatformTools locale (embedded)
        string platformToolsDir = Path.Combine(Directory.GetCurrentDirectory(), "PlatformTools");
        string embeddedPath = Path.Combine(platformToolsDir, AdbExecutableName);

        // Se adb non c'e', prova a estrarlo dalle risorse
        if (!File.Exists(embeddedPath))
            ExtractPlatformTools(platformToolsDir);

        if (File.Exists(embeddedPath))
        {
            _adbPath = embeddedPath;
            return true;
        }

        // Cerca nei percorsi comuni in base all'OS
        string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        string[] possiblePaths = IsWindows
            ? new[]
            {
                Path.Combine(localAppData, "Android", "Sdk", "platform-tools", "adb.exe"),
                Path.Combine(userProfile, "Android", "Sdk", "platform-tools", "adb.exe"),
                @"C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe",
                @"C:\Android\platform-tools\adb.exe"
            }
            : new[]
            {
                "/usr/bin/adb",
                "/usr/local/bin/adb",
                "/opt/android-sdk/platform-tools/adb",
                Path.Combine(userProfile, "Android/Sdk/platform-tools/adb")
            };

        foreach (string path in possiblePaths)
        {
            if (File.Exists(path))
            {
                _adbPath = path;
                return true;
            }
        }

        // Ultimo tentativo: cerca nel PATH con "where" (Windows) o "which" (Linux/macOS)
        try
        {
            string command = IsWindows ? "where" : "which";
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = command,
                    Arguments = "adb",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            string output = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit();

            // "where" su Windows puo' restituire piu' righe, prendi la prima
            string firstLine = output.Split('\n')[0].Trim();
            if (process.ExitCode == 0 && !string.IsNullOrWhiteSpace(firstLine) && File.Exists(firstLine))
            {
                _adbPath = firstLine;
                return true;
            }
        }
        catch { }

        return false;
    }

    // Avvia il server ADB
    private async Task<bool> StartServerAsync()
    {
        try
        {
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

    // Popola la lista con dispositivi fittizi per testare il wizard senza hardware
    public void LoadSimulatedDevices()
    {
        _devices.Clear();
        _originDevices.Clear();

        var backup = new DeviceData
        {
            Serial = "SIM-BACKUP-001",
            Model = "Pixel_5",
            Product = "redfin",
            State = DeviceState.Online
        };

        var source = new DeviceData
        {
            Serial = "SIM-SOURCE-001",
            Model = "Galaxy_S21",
            Product = "o1s",
            State = DeviceState.Online
        };

        _backupDevice = backup;
        _devices.Add(backup);
        _devices.Add(source);
        _originDevices.Add(source);

        _initialized = true;
        IsSimulation = true;
    }

    /// Scansiona i dispositivi connessi e separa il backup dagli altri
    public async Task ScanDevicesAsync()
    {
        _devices.Clear();
        _originDevices.Clear();
        _backupDevice = null;

        try
        {
            foreach (var device in await _adbClient.GetDevicesAsync())
            {
                if (device.State == DeviceState.Online)
                {
                    if (_configManager.IsBackupDevice(device.Model, device.Product))
                        _backupDevice = device;
                    else
                        _originDevices.Add(device);

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

    // Connette un dispositivo via ADB wireless
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

    // Associa un dispositivo via ADB wireless con codice di pairing
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

    // Ferma il server ADB
    public async Task StopAsync()
    {
        try
        {
            if (_adbServer != null)
                await _adbServer.StopServerAsync();
        }
        catch { }
    }

    // Recupera la lista degli utenti dal dispositivo
    public async Task GetUsersAsync(DeviceData? device = null)
    {
        _users.Clear();
        _currentUser = null;
        if (IsSimulation) return;
        var targetDevice = device ?? _backupDevice;

        try
        {
            if (targetDevice == null) return;

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            var receiver = new ConsoleOutputReceiver();

            await _adbClient.ExecuteRemoteCommandAsync("pm list users", targetDevice, receiver, cts.Token);
            var output = receiver.ToString();

            // Parsing del formato: UserInfo{ID:NOME:FLAGS}
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
                        _users.Add(name, new MyUser(name, id));
                }
            }
        }
        catch
        {
            _users.Clear();
            _currentUser = null;
        }
    }

    // Recupera l'utente attualmente attivo sul dispositivo
    public async Task<MyUser?> GetCurrentUserAsync(DeviceData? device = null)
    {
        if (IsSimulation) return null;
        var targetDevice = device ?? _backupDevice;
        try
        {
            if (targetDevice == null) return null;

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            var receiver = new ConsoleOutputReceiver();

            await _adbClient.ExecuteRemoteCommandAsync("dumpsys activity activities", targetDevice, receiver, cts.Token);
            var output = receiver.ToString();

            // Cerca la riga "mCurrentUser=UserInfo{ID:NOME:FLAGS}"
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

    // Cambia l'utente attivo sul dispositivo e attende il completamento
    public async Task<bool> SetUserAsync(MyUser targetUser, DeviceData? device = null)
    {
        if (IsSimulation) { _currentUser = targetUser; return true; }
        var targetDevice = device ?? _backupDevice;
        try
        {
            if (targetUser == null || targetDevice == null) return false;

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var receiver = new ConsoleOutputReceiver();

            await _adbClient.ExecuteRemoteCommandAsync($"am switch-user {targetUser.Id}", targetDevice, receiver, cts.Token);

            // Attende che il cambio utente si completi
            await Task.Delay(USERSPACE_CHANGE_TIME);

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

    // Crea un nuovo utente sul dispositivo
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

    // Rimuove un utente dal dispositivo
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

    // Riavvia il dispositivo
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

    // Recupera informazioni dettagliate sul dispositivo (modello, Android, batteria, storage)
    public async Task<Dictionary<string, string>> GetDeviceInfoAsync(DeviceData device)
    {
        var info = new Dictionary<string, string>();

        info["Modello"] = device.Model;
        info["Prodotto"] = device.Product;
        info["Seriale"] = device.Serial;

        try
        {
            var r1 = new ConsoleOutputReceiver();
            await AdbClient.Instance.ExecuteRemoteCommandAsync("getprop ro.build.version.release", device, r1);
            info["Android"] = r1.ToString().Trim();

            var r2 = new ConsoleOutputReceiver();
            await AdbClient.Instance.ExecuteRemoteCommandAsync("getprop ro.build.version.sdk", device, r2);
            info["API"] = r2.ToString().Trim();

            // Estrae il livello batteria da "dumpsys battery"
            var r3 = new ConsoleOutputReceiver();
            await AdbClient.Instance.ExecuteRemoteCommandAsync("dumpsys battery | grep level", device, r3);
            string battLine = r3.ToString().Trim();
            if (battLine.Contains("level"))
            {
                string level = battLine.Split(':').LastOrDefault()?.Trim() ?? "?";
                info["Batteria"] = $"{level}%";
            }

            // Estrae lo spazio disco da "df /data"
            var r4 = new ConsoleOutputReceiver();
            await AdbClient.Instance.ExecuteRemoteCommandAsync("df /data | tail -1", device, r4);
            string dfLine = r4.ToString().Trim();
            if (!string.IsNullOrEmpty(dfLine))
            {
                var parts = dfLine.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 4)
                    info["Storage"] = $"Usato {parts[2]} / Totale {parts[1]}";
            }
        }
        catch { }

        return info;
    }

    // Fa uno screenshot del dispositivo e lo salva in locale
    public async Task<string> TakeScreenshotAsync(DeviceData device, string localPath)
    {
        try
        {
            string remotePath = "/sdcard/screenshot_temp.png";
            var receiver = new ConsoleOutputReceiver();
            await AdbClient.Instance.ExecuteRemoteCommandAsync($"screencap -p {remotePath}", device, receiver);

            var sync = new SyncService(AdbClient.Instance.EndPoint, device);
            using (var fs = File.OpenWrite(localPath))
            {
                await sync.PullAsync(remotePath, fs, null, CancellationToken.None);
            }

            // Rimuove lo screenshot temporaneo dal dispositivo
            await AdbClient.Instance.ExecuteRemoteCommandAsync($"rm {remotePath}", device, new ConsoleOutputReceiver());

            return localPath;
        }
        catch (Exception ex)
        {
            return $"Errore: {ex.Message}";
        }
    }

    // Elenca le cartelle nella root di /sdcard/
    public async Task<List<string>> GetRootFoldersAsync(DeviceData device)
    {
        var receiver = new ConsoleOutputReceiver();
        await AdbClient.Instance.ExecuteRemoteCommandAsync("ls -1 /sdcard/", device, receiver);

        var items = receiver.ToString()
            .Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(f => f.Trim())
            .Where(f => !string.IsNullOrWhiteSpace(f))
            .ToList();

        // Verifica quali sono effettivamente directory
        var folders = new List<string>();
        foreach (var item in items)
        {
            var checkReceiver = new ConsoleOutputReceiver();
            await AdbClient.Instance.ExecuteRemoteCommandAsync($"test -d /sdcard/\"{item}\" && echo 'DIR'", device, checkReceiver);
            if (checkReceiver.ToString().Trim() == "DIR")
                folders.Add(item);
        }

        return folders;
    }

    // Elenca solo i file (non le sottocartelle) in una cartella del dispositivo
    public async Task<List<string>> GetFolderFilesAsync(DeviceData device, string deviceFolder = "DCIM/Camera")
    {
        deviceFolder = deviceFolder.Trim('/');
        var receiver = new ConsoleOutputReceiver();

        // Usa find con -maxdepth 1 -type f per ottenere solo file, non sottocartelle
        await AdbClient.Instance.ExecuteRemoteCommandAsync(
            $"find /sdcard/{deviceFolder} -maxdepth 1 -type f -printf '%f\\n'", device, receiver);

        var result = receiver.ToString()
            .Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(f => f.Trim())
            .Where(f => !string.IsNullOrWhiteSpace(f))
            .ToList();

        // Fallback: se find non e' disponibile, usa ls -1p e filtra le directory
        if (result.Count == 0)
        {
            var fallbackReceiver = new ConsoleOutputReceiver();
            await AdbClient.Instance.ExecuteRemoteCommandAsync($"ls -1p /sdcard/{deviceFolder}", device, fallbackReceiver);

            result = fallbackReceiver.ToString()
                .Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(f => f.Trim())
                .Where(f => !string.IsNullOrWhiteSpace(f) && !f.EndsWith("/"))
                .ToList();
        }

        return result;
    }

    // Trasferisce file locali nella cartella Documents del dispositivo
    public async Task<int> PushToDocumentsAsync(DeviceData device, List<string> localFilePaths, IProgress<(int current, int total, string fileName)>? progress = null)
    {
        return await PushFilesAsync(device, localFilePaths, "Documents", progress);
    }

    // Trasferisce file locali in una cartella del dispositivo
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

    // Scarica file da una cartella del dispositivo al PC
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

            // Try/catch sul singolo file per non interrompere tutto il backup
            try
            {
                using (FileStream fs = File.OpenWrite(localPath))
                {
                    await sync.PullAsync(remotePath, fs, null, CancellationToken.None);
                }

                // Verifica che il file scaricato non sia vuoto
                if (new FileInfo(localPath).Length > 0)
                    pulledCount++;
            }
            catch
            {
                // Se il pull fallisce, rimuove il file parziale
                if (File.Exists(localPath))
                    File.Delete(localPath);
            }
        }

        return pulledCount;
    }

    // Cancella file da una cartella del dispositivo
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

    // Esegue il backup di una cartella dal dispositivo al PC
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

            // Crea la cartella locale con timestamp per evitare conflitti
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
        catch { }

        return result;
    }

    // Trasferisce le foto dal dispositivo sorgente a quello di backup
    public async Task<TransferResult> TransferPhotosAsync(DeviceData originDevice, DeviceData destinationDevice, bool deleteFromOrigin, IProgress<(int current, int total, string fileName)>? progress = null)
    {
        var result = new TransferResult();

        // In simulazione restituisce un risultato fittizio con breve attesa
        if (IsSimulation)
        {
            int fakeCount = 5;
            result.ToBePulledCount = fakeCount;
            result.ToBePushedCount = fakeCount;
            for (int i = 1; i <= fakeCount; i++)
            {
                progress?.Report((i, fakeCount, $"IMG_SIM_{i:D4}.jpg"));
                await Task.Delay(500);
            }
            result.PulledCount = fakeCount;
            result.PushedCount = fakeCount;
            result.AllFilesSynced = true;
            result.DeleteCompleted = deleteFromOrigin;
            result.FolderPath = Path.Combine(Directory.GetCurrentDirectory(), "SimulatedTransfer");
            return result;
        }

        List<string> originPhotos = await GetFolderFilesAsync(originDevice);
        List<string> destPhotos = await GetFolderFilesAsync(destinationDevice);

        // Filtra: solo le foto non gia' presenti sulla destinazione
        var destSet = new HashSet<string>(destPhotos);
        List<string> photosToTransfer = originPhotos.Where(p => !destSet.Contains(p)).ToList();

        result.ToBePulledCount = photosToTransfer.Count;

        if (photosToTransfer.Count == 0)
        {
            result.AllFilesSynced = true;
            return result;
        }

        // Crea la cartella temporanea locale
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string localDir = Path.Combine(
            Directory.GetCurrentDirectory(),
            "GooglePhotoTransfer",
            $"{originDevice.Model.ToUpper()}_to_{destinationDevice.Model.ToUpper()}_{timestamp}"
        );
        result.FolderPath = localDir;
        Directory.CreateDirectory(localDir);

        // Pull: scarica le foto dal dispositivo sorgente
        result.PulledCount = await PullFilesAsync(originDevice, photosToTransfer, localDir, "DCIM/Camera", progress);
        bool extractionCompleted = result.PulledCount == result.ToBePulledCount;

        // Push: carica le foto sul dispositivo di backup
        if (result.PulledCount > 0)
        {
            List<string> filesToPush = Directory.GetFiles(localDir).ToList();
            result.ToBePushedCount = filesToPush.Count;

            if (filesToPush.Count > 0)
                result.PushedCount = await PushFilesAsync(destinationDevice, filesToPush, "DCIM/Camera", progress);
        }

        result.AllFilesSynced = extractionCompleted && result.PushedCount == result.ToBePushedCount;

        // Cancella dalla sorgente solo se tutto il trasferimento e' riuscito
        if (deleteFromOrigin && result.AllFilesSynced)
        {
            int deletedCount = await DeleteFilesAsync(originDevice, photosToTransfer, "DCIM/Camera", progress);
            result.DeleteCompleted = deletedCount == result.PulledCount;
        }

        return result;
    }

    #endregion
}
