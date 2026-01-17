using AdvancedSharpAdbClient.Models;
using GooglePhotoManager.Model;
using GooglePhotoManager.Utils;
using System.Reflection;
using L = GooglePhotoManager.Utils.Localization;

class Program
{
    #region "Private fields"

    private static AdbManager _adbManager = new AdbManager();

    #endregion

    private static string AppVersion => Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";

    static async Task Main()
    {
        ConsoleUI.ShowBanner(AppVersion);

        ConsoleUI.StartSpinner(L.InitializingAdb);
        bool initializeResult = await _adbManager.Initialize();
        ConsoleUI.StopSpinner();

        if (!initializeResult)
        {
            ConsoleUI.ShowError(L.AdbInitFailed);
            ConsoleUI.Prompt(L.PressEnterToClose);
            return;
        }

        ConsoleUI.ShowSuccess(L.AdbInitialized);
        Console.WriteLine();

        // Prima scansione automatica
        await ScanDevicesAsync();

        // Menu principale
        await MainMenuLoop();
    }

    #region "Main Menu"

    /// <summary>
    /// Loop principale del menu.
    /// </summary>
    private static async Task MainMenuLoop()
    {
        while (true)
        {
            ShowStatus();
            ShowMainMenu();

            string? choice = ConsoleUI.Prompt(L.SelectOption);

            switch (choice)
            {
                case "1":
                    await ScanDevicesAsync();
                    break;
                case "2":
                    await ConnectDeviceWirelessAsync();
                    break;
                case "3":
                    await PairDeviceWirelessAsync();
                    break;
                case "4":
                    await TransferToDocumentsAsync();
                    break;
                case "5":
                    await BackupDeviceFolderAsync();
                    break;
                case "6":
                    await TransferPhotosToBackupAsync();
                    break;
                case "7":
                    await SettingsMenuAsync();
                    break;
                case "8":
                    await ScanDevicesAsync();
                    break;
                case "9":
                case "0":
                    await ExitAsync();
                    return;
                default:
                    ConsoleUI.ShowError(L.InvalidOption);
                    break;
            }

            Console.WriteLine();
        }
    }

    /// <summary>
    /// Mostra lo stato attuale: dispositivo backup e lista dispositivi connessi.
    /// </summary>
    private static void ShowStatus()
    {
        Console.WriteLine();
        ConsoleUI.ShowSectionTitle(L.ConnectionStatus);

        // Stato dispositivo di backup
        if (_adbManager.IsBackupDeviceConnected)
        {
            ConsoleUI.ShowSuccess($"{L.BackupDevice}: {_adbManager.BackupDeviceName} [{L.Connected}]");
        }
        else
        {
            ConsoleUI.ShowWarning($"{L.BackupDevice}: {_adbManager.BackupDeviceName} [{L.NotConnected}]");
        }

        Console.WriteLine();

        // Lista dispositivi connessi
        if (_adbManager.Devices.Count > 0)
        {
            ConsoleUI.ShowInfo($"{L.ConnectedDevices}: {_adbManager.Devices.Count}");
            Console.WriteLine();

            for (int i = 0; i < _adbManager.Devices.Count; i++)
            {
                var device = _adbManager.Devices[i];
                bool isBackup = _adbManager.BackupDevice != null &&
                               device.Serial == _adbManager.BackupDevice.Serial;

                string label = isBackup ? $" [{L.Backup}]" : "";
                ConsoleUI.ShowDeviceCard(
                    i + 1,
                    device.Model,
                    device.Product,
                    $"{device.Serial}{label}"
                );
            }
        }
        else
        {
            ConsoleUI.ShowWarning(L.NoDeviceConnected);
        }
    }

    /// <summary>
    /// Mostra il menu principale.
    /// </summary>
    private static void ShowMainMenu()
    {
        Console.WriteLine();
        ConsoleUI.ShowSectionTitle(L.MainMenu);

        ConsoleUI.ShowMenu(
            ("1", L.MenuScanDevices),
            ("2", L.MenuConnectDevice),
            ("3", L.MenuPairDevice),
            ("4", L.MenuTransferToDocuments),
            ("5", L.MenuBackupFolder),
            ("6", L.MenuTransferPhotos),
            ("7", L.MenuSettings),
            ("8", L.MenuReloadDevices),
            ("9", L.MenuExit)
        );
    }

    #endregion

    #region "Settings Menu"

    /// <summary>
    /// Menu Impostazioni.
    /// </summary>
    private static async Task SettingsMenuAsync()
    {
        while (true)
        {
            Console.WriteLine();
            ConsoleUI.ShowSectionTitle(L.SettingsMenu);

            // Mostra impostazioni correnti
            ConsoleUI.ShowInfo($"{L.CurrentLanguageLabel}: {L.GetLanguageDisplayName(_adbManager.Config.Language)}");
            ConsoleUI.ShowInfo($"{L.BackupDevice}: {_adbManager.BackupDeviceName}");

            Console.WriteLine();
            ConsoleUI.ShowMenu(
                ("1", L.SettingsSetBackupDevice),
                ("2", L.SettingsLanguage),
                ("0", L.SettingsBack)
            );

            string? choice = ConsoleUI.Prompt(L.SelectOption);

            switch (choice)
            {
                case "1":
                    await SetBackupDeviceAsync();
                    break;
                case "2":
                    ChangeLanguage();
                    break;
                case "0":
                    return;
                default:
                    ConsoleUI.ShowError(L.InvalidOption);
                    break;
            }
        }
    }

    /// <summary>
    /// Cambia la lingua dell'applicazione.
    /// </summary>
    private static void ChangeLanguage()
    {
        Console.WriteLine();
        ConsoleUI.ShowInfo(L.SelectLanguage);
        Console.WriteLine();

        ConsoleUI.ShowMenu(
            ("1", "Italiano"),
            ("2", "English"),
            ("0", L.Cancel)
        );

        string? choice = ConsoleUI.Prompt(L.SelectOption);

        L.Language newLanguage;
        switch (choice)
        {
            case "1":
                newLanguage = L.Language.Italian;
                break;
            case "2":
                newLanguage = L.Language.English;
                break;
            case "0":
                return;
            default:
                ConsoleUI.ShowError(L.InvalidOption);
                return;
        }

        _adbManager.Config.SetLanguage(newLanguage);
        ConsoleUI.ShowSuccess($"{L.LanguageChanged} {L.GetLanguageDisplayName(newLanguage)}");
    }

    #endregion

    #region "Commands"

    /// <summary>
    /// Scansiona i dispositivi connessi.
    /// </summary>
    private static async Task ScanDevicesAsync()
    {
        ConsoleUI.StartSpinner(L.ScanningDevices);
        await _adbManager.ScanDevicesAsync();
        ConsoleUI.StopSpinner();

        if (_adbManager.Devices.Count > 0)
        {
            ConsoleUI.ShowSuccess(L.FoundDevices(_adbManager.Devices.Count));
        }
        else
        {
            ConsoleUI.ShowWarning(L.NoDeviceFound);
        }
    }

    /// <summary>
    /// Connette un dispositivo via ADB Wireless.
    /// </summary>
    private static async Task ConnectDeviceWirelessAsync()
    {
        Console.WriteLine();
        ConsoleUI.ShowInfo(L.WirelessAdbInfo1);
        ConsoleUI.ShowInfo(L.WirelessAdbInfo2);
        Console.WriteLine();

        string? ip = ConsoleUI.Prompt(L.EnterIpAddress);
        if (string.IsNullOrWhiteSpace(ip))
        {
            ConsoleUI.ShowError(L.InvalidIpAddress);
            return;
        }

        string? port = ConsoleUI.Prompt(L.EnterPort);
        if (string.IsNullOrWhiteSpace(port))
        {
            ConsoleUI.ShowError(L.InvalidPort);
            return;
        }

        ConsoleUI.StartSpinner(L.Connecting);
        string result = await _adbManager.ConnectWirelessAsync(ip, port);
        ConsoleUI.StopSpinner();

        if (!string.IsNullOrWhiteSpace(result))
        {
            ConsoleUI.ShowInfo(result);
        }

        // Ricarica dispositivi dopo la connessione
        await ScanDevicesAsync();
    }

    /// <summary>
    /// Abbina un dispositivo via ADB Wireless.
    /// </summary>
    private static async Task PairDeviceWirelessAsync()
    {
        Console.WriteLine();
        ConsoleUI.ShowInfo(L.WirelessPairInfo1);
        ConsoleUI.ShowInfo(L.WirelessPairInfo2);
        Console.WriteLine();

        string? ip = ConsoleUI.Prompt(L.EnterIpAddress);
        if (string.IsNullOrWhiteSpace(ip))
        {
            ConsoleUI.ShowError(L.InvalidIpAddress);
            return;
        }

        string? port = ConsoleUI.Prompt(L.EnterPairingPort);
        if (string.IsNullOrWhiteSpace(port))
        {
            ConsoleUI.ShowError(L.InvalidPort);
            return;
        }

        string? pairingCode = ConsoleUI.Prompt(L.EnterPairingCode);
        if (string.IsNullOrWhiteSpace(pairingCode))
        {
            ConsoleUI.ShowError(L.InvalidPairingCode);
            return;
        }

        ConsoleUI.StartSpinner(L.Pairing);
        string result = await _adbManager.PairWirelessAsync(ip, port, pairingCode);
        ConsoleUI.StopSpinner();

        if (!string.IsNullOrWhiteSpace(result))
        {
            ConsoleUI.ShowInfo(result);
        }
    }

    /// <summary>
    /// Trasferisce file dal PC alla cartella Documents del dispositivo.
    /// </summary>
    private static async Task TransferToDocumentsAsync()
    {
        if (_adbManager.Devices.Count == 0)
        {
            ConsoleUI.ShowWarning(L.NoDeviceConnected);
            return;
        }

        // Seleziona dispositivo se ce ne sono piu' di uno
        DeviceData? targetDevice = SelectDevice(L.SelectDestinationDevice);
        if (targetDevice == null) return;

        Console.WriteLine();
        ConsoleUI.ShowInfo(L.EnterFilePaths);
        ConsoleUI.ShowInfo(L.EnterEmptyLineToFinish);
        Console.WriteLine();

        var filePaths = new List<string>();
        while (true)
        {
            string? path = ConsoleUI.Prompt(L.FilePath);
            if (string.IsNullOrWhiteSpace(path)) break;

            if (File.Exists(path))
            {
                filePaths.Add(path);
                ConsoleUI.ShowSuccess(L.FileAdded(Path.GetFileName(path)));
            }
            else
            {
                ConsoleUI.ShowError(L.FileNotFound(path));
            }
        }

        if (filePaths.Count == 0)
        {
            ConsoleUI.ShowWarning(L.NoFileSelected);
            return;
        }

        Console.WriteLine();
        ConsoleUI.StartSpinner(L.Transferring);
        int transferred = await _adbManager.PushToDocumentsAsync(targetDevice, filePaths);
        ConsoleUI.StopSpinner();

        ConsoleUI.ShowSummaryBox(L.TransferResult,
            (L.FilesToTransfer, filePaths.Count.ToString()),
            (L.FilesTransferred, transferred.ToString()),
            (L.Result, transferred == filePaths.Count ? L.Completed : L.Partial)
        );
    }

    /// <summary>
    /// Esegue il backup di una cartella del dispositivo sul PC.
    /// </summary>
    private static async Task BackupDeviceFolderAsync()
    {
        if (_adbManager.Devices.Count == 0)
        {
            ConsoleUI.ShowWarning(L.NoDeviceConnected);
            return;
        }

        // Seleziona dispositivo se ce ne sono piu' di uno
        DeviceData? targetDevice = SelectDevice(L.SelectBackupSourceDevice);
        if (targetDevice == null) return;

        // Ottieni lista cartelle
        Console.WriteLine();
        ConsoleUI.StartSpinner(L.RetrievingFolders);
        var folders = await _adbManager.GetRootFoldersAsync(targetDevice);
        ConsoleUI.StopSpinner();

        if (folders.Count == 0)
        {
            ConsoleUI.ShowWarning(L.NoFolderFound);
            return;
        }

        // Mostra cartelle disponibili
        Console.WriteLine();
        ConsoleUI.ShowInfo(L.AvailableFolders);
        Console.WriteLine();

        for (int i = 0; i < folders.Count; i++)
        {
            ConsoleUI.ShowMenu(((i + 1).ToString(), folders[i]));
        }

        Console.WriteLine();
        string? folderChoice = ConsoleUI.Prompt(L.SelectFolder);

        if (!int.TryParse(folderChoice, out int folderIndex) ||
            folderIndex < 1 || folderIndex > folders.Count)
        {
            ConsoleUI.ShowError(L.InvalidSelection);
            return;
        }

        string selectedFolder = folders[folderIndex - 1];

        // Esegui backup
        Console.WriteLine();
        ConsoleUI.StartSpinner(L.BackingUp(selectedFolder));
        var result = await _adbManager.BackupFolderAsync(targetDevice, selectedFolder);
        ConsoleUI.StopSpinner();

        if (result.ToBePulledCount == 0)
        {
            ConsoleUI.ShowWarning(L.FolderEmpty);
            return;
        }

        ConsoleUI.ShowSummaryBox(L.BackupResult,
            (L.FilesToCopy, result.ToBePulledCount.ToString()),
            (L.FilesCopied, result.PulledCount.ToString()),
            (L.Result, result.AllFilesSynced ? L.Completed : L.Partial),
            (L.LocalFolder, result.FolderPath ?? "N/A")
        );
    }

    /// <summary>
    /// Trasferisce foto dal dispositivo origine al dispositivo di backup.
    /// </summary>
    private static async Task TransferPhotosToBackupAsync()
    {
        if (!_adbManager.IsBackupDeviceConnected)
        {
            ConsoleUI.ShowError(L.BackupDeviceNotConnected);
            ConsoleUI.ShowInfo($"{L.ExpectedDevice}: {_adbManager.BackupDeviceName}");
            return;
        }

        if (_adbManager.OriginDevices.Count == 0)
        {
            ConsoleUI.ShowWarning(L.NoSourceDeviceConnected);
            return;
        }

        // Scansiona gli utenti sul dispositivo di backup
        Console.WriteLine();
        ConsoleUI.StartSpinner(L.ScanningUsers);
        await _adbManager.GetUsersAsync();
        ConsoleUI.StopSpinner();

        if (_adbManager.Users.Count == 0)
        {
            ConsoleUI.ShowWarning(L.NoUsersFound);
            return;
        }

        // Seleziona utente sul dispositivo di backup
        var selectedUser = await SelectUserAsync();
        if (selectedUser == null) return;

        // Seleziona dispositivo origine se ce ne sono piu' di uno
        DeviceData? originDevice;
        if (_adbManager.OriginDevices.Count == 1)
        {
            originDevice = _adbManager.OriginDevices[0];
            ConsoleUI.ShowInfo($"{L.SourceDevice}: {originDevice.Model} ({originDevice.Product})");
        }
        else
        {
            originDevice = SelectDevice(L.SelectSourceDevice, _adbManager.OriginDevices);
            if (originDevice == null) return;
        }

        Console.WriteLine();
        string? deleteResponse = ConsoleUI.Prompt(L.DeletePhotosAfterBackup);
        bool deleteFromOrigin = deleteResponse?.Trim().ToUpper() == "S" ||
                                deleteResponse?.Trim().ToUpper() == "Y";

        Console.WriteLine();
        ConsoleUI.StartSpinner(L.Transferring);
        var result = await _adbManager.TransferPhotos(originDevice, _adbManager.BackupDevice!, deleteFromOrigin);
        ConsoleUI.StopSpinner();

        var summaryItems = new List<(string, string)>
        {
            (L.PhotosToExtract, result.ToBePulledCount.ToString()),
            (L.PhotosExtracted, result.PulledCount.ToString()),
            (L.PhotosToTransfer, result.ToBePushedCount.ToString()),
            (L.PhotosTransferred, result.PushedCount.ToString()),
            (L.Synchronization, result.AllFilesSynced ? L.Completed : L.Failed)
        };

        if (deleteFromOrigin)
        {
            summaryItems.Add((L.Deletion, result.DeleteCompleted ? L.Completed : L.Failed));
        }

        summaryItems.Add((L.LocalFolder, result.FolderPath ?? "N/A"));

        ConsoleUI.ShowSummaryBox(L.TransferSummary, summaryItems.ToArray());

        if (result.AllFilesSynced)
        {
            ConsoleUI.ShowSuccess(L.BackupCompletedSuccess);

            // Se tutti i file sono stati trasferiti con successo, chiedi se cancellare la cartella locale
            if (!string.IsNullOrEmpty(result.FolderPath) && Directory.Exists(result.FolderPath))
            {
                Console.WriteLine();
                string? deleteLocalResponse = ConsoleUI.Prompt(L.DeleteLocalPhotosAfterTransfer);
                bool deleteLocalFiles = deleteLocalResponse?.Trim().ToUpper() == "S" ||
                                        deleteLocalResponse?.Trim().ToUpper() == "Y";

                if (deleteLocalFiles)
                {
                    try
                    {
                        ConsoleUI.StartSpinner(L.DeletingLocalFiles);
                        Directory.Delete(result.FolderPath, true);
                        ConsoleUI.StopSpinner();
                        ConsoleUI.ShowSuccess(L.LocalFilesDeleted);
                    }
                    catch
                    {
                        ConsoleUI.StopSpinner();
                        ConsoleUI.ShowError(L.LocalFilesDeletionFailed);
                    }
                }
            }
        }
        else
        {
            ConsoleUI.ShowWarning(L.BackupCompletedWithErrors);
        }
    }

    /// <summary>
    /// Seleziona un utente dal dispositivo di backup.
    /// </summary>
    private static async Task<MyUser?> SelectUserAsync()
    {
        var users = _adbManager.Users.Values.ToList();

        // Ottieni l'utente attualmente attivo sul dispositivo
        var currentUser = await _adbManager.GetCurrentUserAsync();

        Console.WriteLine();
        ConsoleUI.ShowInfo(L.SelectUser);

        if (currentUser != null)
        {
            ConsoleUI.ShowInfo($"{L.CurrentActiveUser}: {currentUser.Name}");
        }

        Console.WriteLine();

        for (int i = 0; i < users.Count; i++)
        {
            var user = users[i];
            string activeMarker = (currentUser != null && user.Id == currentUser.Id) ? " [*]" : "";
            ConsoleUI.ShowMenu(((i + 1).ToString(), $"{user.Name}{activeMarker}"));
        }

        Console.WriteLine();
        ConsoleUI.ShowMenu(("0", L.Cancel));
        Console.WriteLine();

        string? choice = ConsoleUI.Prompt(L.SelectOption);

        if (!int.TryParse(choice, out int index) || index < 0 || index > users.Count)
        {
            ConsoleUI.ShowError(L.InvalidSelection);
            return null;
        }

        if (index == 0)
        {
            return null;
        }

        var selectedUser = users[index - 1];
        ConsoleUI.ShowInfo($"{L.SelectedUser}: {selectedUser.Name}");

        // Se l'utente selezionato non è quello attivo, effettua il cambio
        if (currentUser == null || selectedUser.Id != currentUser.Id)
        {
            Console.WriteLine();
            ConsoleUI.StartSpinner(L.SwitchingUser);
            bool switchResult = await _adbManager.SetUserAsync(selectedUser);
            ConsoleUI.StopSpinner();

            if (switchResult)
            {
                ConsoleUI.ShowSuccess(L.UserSwitchedSuccessfully);
            }
            else
            {
                ConsoleUI.ShowError(L.UserSwitchFailed);
                return null;
            }
        }

        return selectedUser;
    }

    /// <summary>
    /// Imposta il dispositivo di backup leggendo model e product dall'utente.
    /// </summary>
    private static async Task SetBackupDeviceAsync()
    {
        Console.WriteLine();
        ConsoleUI.ShowInfo(L.SetBackupDeviceInfo);
        ConsoleUI.ShowInfo(L.ValuesSavedInConfig);
        Console.WriteLine();

        ConsoleUI.ShowInfo($"{L.CurrentConfig}: {_adbManager.BackupDeviceName}");
        Console.WriteLine();

        string? model = ConsoleUI.Prompt(L.EnterModel);
        if (string.IsNullOrWhiteSpace(model))
        {
            ConsoleUI.ShowError(L.InvalidModel);
            return;
        }

        string? product = ConsoleUI.Prompt(L.EnterProduct);
        if (string.IsNullOrWhiteSpace(product))
        {
            ConsoleUI.ShowError(L.InvalidProduct);
            return;
        }

        _adbManager.Config.SetBackupDevice(model.Trim(), product.Trim());

        ConsoleUI.ShowSuccess($"{L.BackupDeviceSet}: {_adbManager.BackupDeviceName}");

        // Ricarica dispositivi per aggiornare lo stato
        await ScanDevicesAsync();
    }

    /// <summary>
    /// Seleziona un dispositivo dalla lista.
    /// </summary>
    private static DeviceData? SelectDevice(string prompt, List<DeviceData>? deviceList = null)
    {
        var devices = deviceList ?? _adbManager.Devices;

        if (devices.Count == 0)
        {
            ConsoleUI.ShowWarning(L.NoDeviceAvailable);
            return null;
        }

        if (devices.Count == 1)
        {
            return devices[0];
        }

        Console.WriteLine();
        ConsoleUI.ShowInfo(prompt);
        Console.WriteLine();

        for (int i = 0; i < devices.Count; i++)
        {
            var device = devices[i];
            ConsoleUI.ShowDeviceCard(i + 1, device.Model, device.Product, device.Serial);
        }

        Console.WriteLine();
        ConsoleUI.ShowMenu(("0", L.Cancel));
        Console.WriteLine();

        string? choice = ConsoleUI.Prompt(L.SelectDevice);

        if (!int.TryParse(choice, out int index) || index < 0 || index > devices.Count)
        {
            ConsoleUI.ShowError(L.InvalidSelection);
            return null;
        }

        if (index == 0)
        {
            return null;
        }

        return devices[index - 1];
    }

    /// <summary>
    /// Esce dal programma.
    /// </summary>
    private static async Task ExitAsync()
    {
        Console.WriteLine();
        ConsoleUI.StartSpinner(L.ClosingAdb);
        await _adbManager.KillServiceAsync();
        ConsoleUI.StopSpinner();
        ConsoleUI.ShowInfo(L.Goodbye);
#if DEBUG
        Console.WriteLine();
        ConsoleUI.Prompt(L.PressEnterToClose);
#endif
    }

    #endregion
}
