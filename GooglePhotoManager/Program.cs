using AdvancedSharpAdbClient.Models;
using GooglePhotoManager.Model;
using GooglePhotoManager.Utils;
using System.Net;
using System.Reflection;

class Program
{
    #region "Private fields"

    /// <summary>
    /// ADB manager object.
    /// </summary>
    private static AdbManager _adbManager = new AdbManager();

    private static MyUser _activeUser;

    #endregion

    private static string AppVersion => Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";

    static async Task Main()
    {
        ConsoleUI.ShowBanner(AppVersion);

        ConsoleUI.StartSpinner("Inizializzazione ADB in corso...");
        bool initializeResult = await _adbManager.Initialize();
        ConsoleUI.StopSpinner();

        if (!initializeResult)
        {
            ConsoleUI.ShowError("Inizializzazione ADB fallita.");
            return;
        }

        ConsoleUI.ShowSuccess("ADB inizializzato correttamente.");
        Console.WriteLine();

        // Keep flow block untill unlimited device is connected
        await UnlimitedDeviceWizard();

        // At this point is possible to set an active user before going on
        await UsersWizard();

        // At this point start transfer wizard
        await TransferWizard();

        // Just to be sure, if arrived here kill ADB server
        await Exit();
    }

    #region "Methods"

    /// <summary>
    /// Loops untill unlimited device is connected.
    /// </summary>
    private async static Task UnlimitedDeviceWizard()
    {
        ConsoleUI.ShowSectionTitle("Ricerca Dispositivo di Backup");

        do
        {
            ConsoleUI.StartSpinner("Ricerca del dispositivo di backup in corso...");

            // Scan devices to find unlimited device
            await _adbManager.ScanDevicesAsync();

            ConsoleUI.StopSpinner();

            // Check that unlimited backup device is connected
            if (_adbManager.UnlimitedDevice != null)
            {
                ConsoleUI.ShowSuccess($"{AdbManager.UNLIMITED_BK_DEVICE_NAME} connesso.");
            }
            else
            {
                ConsoleUI.ShowWarning($"\"{AdbManager.UNLIMITED_BK_DEVICE_NAME}\" non trovato.");

                ConsoleUI.ShowMenu(
                    ("1", "Cerca ancora"),
                    ("2", "Connetti via ADB Wireless"),
                    ("3", "Associa via ADB Wireless (se mai connesso)"),
                    ("0", "Chiudi il programma")
                );

                // Switch according to user choice
                switch (ConsoleUI.Prompt("Inserisci la scelta: "))
                {
                    case "0":
                        await Exit();
                        break;

                    case "1":
                        break;

                    case "2":
                        await WirelessAdbWizard(pairingNeeded: false);
                        break;

                    case "3":
                        await WirelessAdbWizard(pairingNeeded: true);
                        break;

                    default:
                        ConsoleUI.ShowError("La scelta effettuata non è valida.");
                        break;
                }
            }

            Console.WriteLine();
        }
        while (_adbManager.UnlimitedDevice == null);
    }

    /// <summary>
    /// Starts wizard to connect or pair a device via ADB Wireless.
    /// </summary>
    /// <param name="pairingNeeded">True if pairing is needed.</param>
    private static async Task WirelessAdbWizard(bool pairingNeeded = false)
    {
        string operationResult = string.Empty;

        Console.WriteLine();
        ConsoleUI.ShowInfo("Per utilizzare ADB Wireless è necessario che l'opzione \"Wireless ADB\"");
        ConsoleUI.ShowInfo("sia attiva nelle impostazioni sviluppatore del dispositivo.");
        Console.WriteLine();

        // Request IP address and check its validity
        string? deviceIpAddress = ConsoleUI.Prompt("Indirizzo IP del dispositivo: ");
        if (string.IsNullOrWhiteSpace(deviceIpAddress) || !IPAddress.TryParse(deviceIpAddress, out _))
        {
            ConsoleUI.ShowError("L'indirizzo IP del dispositivo non sembra essere valido.");
            return;
        }

        // Request port and check its validity
        string? devicePort = ConsoleUI.Prompt("Porta del dispositivo: ");
        if (string.IsNullOrWhiteSpace(devicePort))
        {
            ConsoleUI.ShowError("La porta del dispositivo non può essere vuota.");
            return;
        }

        if (pairingNeeded)
        {
            // Request pairing code and check its validity
            string? devicePairingCode = ConsoleUI.Prompt("Codice di associazione: ");
            if (string.IsNullOrWhiteSpace(devicePairingCode))
            {
                ConsoleUI.ShowError("Il codice di associazione del dispositivo non può essere vuoto.");
                return;
            }

            // Pair
            ConsoleUI.StartSpinner("Associazione in corso...");
            operationResult = await _adbManager.PairWirelessAsync(deviceIpAddress, devicePort, devicePairingCode);
            ConsoleUI.StopSpinner();
        }
        else
        {
            // Connect
            ConsoleUI.StartSpinner("Connessione in corso...");
            operationResult = await _adbManager.ConnectWirelessAsync(deviceIpAddress, devicePort);
            ConsoleUI.StopSpinner();
        }

        if (!string.IsNullOrWhiteSpace(operationResult))
        {
            ConsoleUI.ShowInfo(operationResult);
        }
    }

    /// <summary>
    /// Starts wizard to select current user starting from found backup device user spaces.<br/>
    /// Exits when a user is selected and related device user space is opened on backup device.
    /// </summary>
    private static async Task UsersWizard()
    {
        ConsoleUI.ShowSectionTitle("Selezione Utente");

        do
        {
            ConsoleUI.StartSpinner("Ricerca degli spazi utente sul dispositivo...");

            // Get users from unlimited backup device
            await _adbManager.GetUsersAsync();

            ConsoleUI.StopSpinner();

            if (_adbManager.Users.Count > 0)
            {
                ConsoleUI.ShowSuccess($"Trovati {_adbManager.Users.Count} spazi utente.");
                Console.WriteLine();

                int usersCount = _adbManager.Users.Count;
                for (int userCounter = 0; userCounter < usersCount; userCounter++)
                {
                    MyUser user = _adbManager.Users.Values.ElementAt(userCounter);
                    ConsoleUI.ShowUserCard(userCounter + 1, user.Name);
                }

                Console.WriteLine();
                ConsoleUI.ShowMenu(("0", "Chiudi il programma"));

                // Switch according to user choice
                string? userChoiceStr = ConsoleUI.Prompt("Seleziona un utente: ");
                if (int.TryParse(userChoiceStr, out int userChoice))
                {
                    if (userChoice.Equals(0))
                    {
                        await Exit();
                    }
                    else
                    {
                        if (userChoice >= 1 && userChoice <= usersCount)
                        {
                            MyUser selectedUser = _adbManager.Users.ElementAt(userChoice - 1).Value;

                            ConsoleUI.StartSpinner($"Impostazione utente \"{selectedUser.Name}\"...");
                            bool setResult = await _adbManager.SetUserAsync(selectedUser);
                            ConsoleUI.StopSpinner();

                            if (setResult)
                            {
                                ConsoleUI.ShowSuccess($"\"{selectedUser.Name}\" impostato come utente attivo.");
                            }
                            else
                            {
                                ConsoleUI.ShowError("Non è stato possibile impostare l'utente selezionato.");
                            }
                        }
                        else
                        {
                            ConsoleUI.ShowError("Scelta non valida.");
                        }
                    }
                }
                else
                {
                    ConsoleUI.ShowError("Scelta non valida.");
                }
            }
            else
            {
                ConsoleUI.ShowWarning("Nessuno spazio utente trovato sul dispositivo di backup.");
                Console.WriteLine();
                break;
            }

            Console.WriteLine();
        }
        while (_adbManager.CurrentUser == null);
    }

    private async static Task TransferWizard()
    {
        ConsoleUI.ShowSectionTitle("Trasferimento Foto");

        try
        {
            // If no origin devices exists loop till at least one is detected
            while (_adbManager.OriginDevices.Count.Equals(0))
            {
                ConsoleUI.ShowWarning("Non è stato trovato un dispositivo da cui estrarre le foto.");

                ConsoleUI.ShowMenu(
                    ("1", "Cerca ancora"),
                    ("2", "Connetti via ADB Wireless"),
                    ("3", "Associa via ADB Wireless (se mai connesso)"),
                    ("0", "Chiudi il programma")
                );

                // Switch according to user choice
                switch (ConsoleUI.Prompt("Inserisci la scelta: "))
                {
                    case "0":
                        await Exit();
                        break;

                    case "1":
                        ConsoleUI.StartSpinner("Ricerca dispositivi...");
                        await _adbManager.ScanDevicesAsync();
                        ConsoleUI.StopSpinner();
                        break;

                    case "2":
                        await WirelessAdbWizard(pairingNeeded: false);
                        ConsoleUI.StartSpinner("Ricerca dispositivi...");
                        await _adbManager.ScanDevicesAsync();
                        ConsoleUI.StopSpinner();
                        break;

                    case "3":
                        await WirelessAdbWizard(pairingNeeded: true);
                        ConsoleUI.StartSpinner("Ricerca dispositivi...");
                        await _adbManager.ScanDevicesAsync();
                        ConsoleUI.StopSpinner();
                        break;

                    default:
                        ConsoleUI.ShowError("La scelta effettuata non è valida.");
                        break;
                }
                Console.WriteLine();
            }

            // Origin devices found
            if (_adbManager.OriginDevices.Count > 0)
            {
                // Select a device to get images from
                DeviceData? originDevice = null;

                // Only if not in debug mode
#if !DEBUG
                // There's only a device so select it as origin device
                if (_adbManager.OriginDevices.Count.Equals(1))
                {
                    originDevice = _adbManager.OriginDevices.First();
                }
#endif

                // If more than one device, loop till one is selected
                while (originDevice == null)
                {
                    ConsoleUI.ShowInfo("Seleziona un dispositivo da cui estrarre le foto:");
                    Console.WriteLine();

                    int devicesCount = _adbManager.OriginDevices.Count;
                    for (int deviceCounter = 0; deviceCounter < devicesCount; deviceCounter++)
                    {
                        DeviceData dD = _adbManager.OriginDevices.ElementAt(deviceCounter);
                        ConsoleUI.ShowDeviceCard(
                            deviceCounter + 1,
                            dD.Model,
                            dD.Product,
                            string.IsNullOrWhiteSpace(dD.Name) ? null : dD.Name
                        );
                    }

                    Console.WriteLine();
                    ConsoleUI.ShowMenu(("0", "Chiudi il programma"));

                    // Switch according to user choice
                    string? deviceChoiceStr = ConsoleUI.Prompt("Seleziona un dispositivo: ");
                    if (int.TryParse(deviceChoiceStr, out int deviceChoice))
                    {
                        if (deviceChoice.Equals(0))
                        {
                            await Exit();
                        }
                        else
                        {
                            if (deviceChoice >= 1 && deviceChoice <= devicesCount)
                            {
                                originDevice = _adbManager.OriginDevices.ElementAt(deviceChoice - 1);
                            }
                            else
                            {
                                ConsoleUI.ShowError("Scelta non valida.");
                            }
                        }
                    }
                    else
                    {
                        ConsoleUI.ShowError("Scelta non valida.");
                    }
                    Console.WriteLine();
                }

                if (originDevice != null)
                {
                    bool validChoice = false;

                    do
                    {
                        // Print selected device info
                        ConsoleUI.ShowInfo("Dispositivo selezionato per il backup:");
                        Console.WriteLine();
                        ConsoleUI.ShowDeviceCard(
                            1,
                            originDevice.Model,
                            originDevice.Product,
                            string.IsNullOrWhiteSpace(originDevice.Name) ? null : originDevice.Name
                        );

                        ConsoleUI.ShowSuccess("Tutto pronto per procedere al backup delle foto.");

                        ConsoleUI.ShowMenu(
                            ("1", "Avvia backup"),
                            ("0", "Chiudi il programma")
                        );

                        // Start transfer
                        switch (ConsoleUI.Prompt("Inserisci la scelta: "))
                        {
                            case "0":
                                await Exit();
                                break;

                            case "1":
                                validChoice = true;

                                // Chiedi se eliminare le foto dal dispositivo di origine
                                Console.WriteLine();
                                string? deleteResponse = ConsoleUI.Prompt("Eliminare le foto dal dispositivo di origine dopo il backup? (S/N): ");
                                bool deleteFromOrigin = deleteResponse?.Trim().ToUpper() == "S";

                                // Esegui il trasferimento
                                Console.WriteLine();
                                ConsoleUI.StartSpinner("Trasferimento in corso...");
                                var result = await _adbManager.TransferPhotos(originDevice, _adbManager.UnlimitedDevice, deleteFromOrigin);
                                ConsoleUI.StopSpinner();

                                // Mostra il risultato con summary box
                                var summaryItems = new List<(string, string)>
                                {
                                    ("Foto da estrarre", result.ToBePulledCount.ToString()),
                                    ("Foto estratte", result.PulledCount.ToString()),
                                    ("Foto da trasferire", result.ToBePushedCount.ToString()),
                                    ("Foto trasferite", result.PushedCount.ToString()),
                                    ("Sincronizzazione", result.AllFilesSynced ? "Completata" : "Fallita")
                                };

                                if (deleteFromOrigin)
                                {
                                    summaryItems.Add(("Eliminazione", result.DeleteCompleted ? "Completata" : "Fallita"));
                                }

                                summaryItems.Add(("Cartella locale", result.FolderPath ?? "N/A"));

                                ConsoleUI.ShowSummaryBox("Riepilogo Trasferimento", summaryItems.ToArray());

                                if (result.AllFilesSynced)
                                {
                                    ConsoleUI.ShowSuccess("Backup completato con successo!");
                                }
                                else
                                {
                                    ConsoleUI.ShowWarning("Backup completato con alcuni errori.");
                                }
                                break;

                            default:
                                ConsoleUI.ShowError("La scelta effettuata non è valida.");
                                break;
                        }
                        Console.WriteLine();
                    }
                    while (!validChoice);
                }
                else
                {
                    throw new Exception("Il dispositivo selezionato è cambiato o non più disponibile");
                }
            }
            else
            {
                throw new Exception("I dispositivi di origine connessi sono cambiato o non più disponibili");
            }
        }
        catch (Exception)
        {
            Console.WriteLine();
            ConsoleUI.ShowError("Errore nella ricerca dei dispositivi da sottoporre a backup.");
            ConsoleUI.ShowError("Il programma verrà chiuso per evitare situazioni impreviste.");
            await Exit();
        }
    }

    /// <summary>
    /// Exits program killing ADB server.
    /// </summary>
    private static async Task Exit()
    {
        Console.WriteLine();
        ConsoleUI.StartSpinner("Chiusura del servizio ADB in corso...");
        await _adbManager.KillServiceAsync();
        ConsoleUI.StopSpinner();
        ConsoleUI.ShowInfo("Arrivederci!");
#if DEBUG
        // Just wait user input before close
        Console.WriteLine();
        ConsoleUI.Prompt("Premi INVIO per chiudere...");
#endif
        Environment.Exit(0);
    }

#endregion

}
