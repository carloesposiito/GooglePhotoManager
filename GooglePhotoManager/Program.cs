using AdvancedSharpAdbClient.Models;
using GooglePhotoManager.Model;
using System.Net;
using System.Reflection;
using System.Xml.Schema;
using static System.Net.Mime.MediaTypeNames;

class Program
{
    #region "Private fields"

    /// <summary>
    /// ADB manager object.
    /// </summary>
    private static AdbManager _adbManager = new AdbManager();

    private static MyUser _activeUser;

    #endregion

    static async Task Main()
    {
        Console.WriteLine($"GooglePhotoManager v{Assembly.GetExecutingAssembly().GetName().Version}");
        Console.WriteLine();

        bool initializeResult = await _adbManager.Initialize();
        if (!initializeResult)
        {
            return;
        }
                
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
        do
        {
            Console.WriteLine($"Ricerca del dispositivo di backup in corso...");

            // Scan devices to find unlimited device
            await _adbManager.ScanDevicesAsync();

            // Check that unlimited backup device is connected
            if (_adbManager.UnlimitedDevice != null)
            {
                Console.WriteLine($"{AdbManager.UNLIMITED_BK_DEVICE_NAME} connesso.");
            }
            else
            {
                Console.WriteLine($"\"{AdbManager.UNLIMITED_BK_DEVICE_NAME}\" non trovato.");
                Console.WriteLine($"(1) Cerca ancora");
                Console.WriteLine($"(2) Connetti via ADB Wireless");
                Console.WriteLine($"(3) Associa via ADB Wireless (se mai connesso precedentemente)");
                Console.WriteLine();
                Console.Write($"Inserisci la scelta desiderata oppure (0) per chiudere il programma: ");

                // Switch according to user choice
                switch (Console.ReadLine())
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
                        Console.WriteLine("La scelta effettuata non è valida.");
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

        Console.WriteLine("Attenzione: per utilizzare ADB Wireless è necessario che l'opzione \"Wireless ADB\" sia attiva nelle impostazioni sviluppatore del dispositivo.\n" +
            "Dalla schermata ADB Wireless è possibile ottenere tutti i dati necessari per la connessione Wireless.");
        Console.WriteLine();
        
        // Request IP address and check its validity
        Console.Write("Inserisci l'indirizzo IP del dispositivo: ");
        string deviceIpAddress = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(deviceIpAddress) || !IPAddress.TryParse(deviceIpAddress, out _))
        {
            Console.WriteLine("L'indirizzo IP del dispositivo non sembra essere valido");
            return;
        }

        // Request port and check its validity
        Console.Write("Inserisci la porta del dispositivo: ");
        string devicePort = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(devicePort))
        {
            Console.WriteLine("La porta del dispositivo non può essere vuota");
            return;
        }

        if (pairingNeeded)
        {
            // Request pairing code and check its validity
            Console.Write("Inserisci il codice di associazione del dispositivo: ");
            string devicePairingCode = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(devicePairingCode))
            {
                Console.WriteLine("Il codice di associazione del dispositivo non può essere vuoto");
                return;
            }

            // Pair
            operationResult = await _adbManager.PairWirelessAsync(deviceIpAddress, devicePort, devicePairingCode);
        }
        else
        { 
            // Connect
            operationResult = await _adbManager.ConnectWirelessAsync(deviceIpAddress, devicePort);
        }

        if (!string.IsNullOrWhiteSpace(operationResult))
        {
            Console.WriteLine(operationResult);
        }
    }

    /// <summary>
    /// Starts wizard to select current user starting from found backup device user spaces.<br/>
    /// Exits when a user is selected and related device user space is opened on backup device.
    /// </summary>
    private static async Task UsersWizard()
    {
        do
        {
            Console.WriteLine($"Ricerca degli spazio utente sul dispositivo...");

            // Get users from unlimited backup device
            await _adbManager.GetUsersAsync();
            
            if (_adbManager.Users.Count > 0)
            {
                int usersCount = _adbManager.Users.Count;
                int userCounter;
                for (userCounter = 0; userCounter < usersCount; userCounter++)
                {
                    MyUser user = _adbManager.Users.Values.ElementAt(userCounter);
                    Console.WriteLine($"({userCounter + 1}) - {user.Name}");
                }

                Console.WriteLine();
                Console.Write($"Seleziona un utente oppure (0) per chiudere il programma: ");

                // Switch according to user choice
                string userChoiceStr = Console.ReadLine();
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
                            if (await _adbManager.SetUserAsync(selectedUser))
                            {
                                Console.WriteLine($"\"{selectedUser.Name}\" impostato come utente attivo");
                            }
                            else
                            {
                                Console.WriteLine("Non è stato possibile impostare l'utente selezionato");
                            }
                        }
                        else
                        {
                            Console.WriteLine("Scelta non valida");
                        }
                    }
                }
                else
                {
                    Console.WriteLine("Scelta non valida");
                }
            }
            else
            {
                Console.WriteLine("Nessuno spazio utente trovato sul dispositivo di backup");
                Console.WriteLine();
                break;
            }

            Console.WriteLine();
        }
        while (_adbManager.CurrentUser == null);
    }

  






    private async static Task TransferWizard()
    {
        try
        {
            // If no origin devices exists loop till at least one is detected
            while (_adbManager.OriginDevices.Count.Equals(0))
            {
                Console.WriteLine($"Non è stato trovato un dispositivo da cui estrarre le foto da sottoporre a backup.");
                Console.WriteLine($"(1) Cerca ancora");
                Console.WriteLine($"(2) Connetti via ADB Wireless");
                Console.WriteLine($"(3) Associa via ADB Wireless (se mai connesso precedentemente)");
                Console.WriteLine();
                Console.Write($"Inserisci la scelta desiderata oppure (0) per chiudere il programma: ");

                // Switch according to user choice
                switch (Console.ReadLine())
                {
                    case "0":
                        await Exit();
                        break;

                    case "1":
                        await _adbManager.ScanDevicesAsync();
                        break;

                    case "2":
                        await WirelessAdbWizard(pairingNeeded: false);
                        await _adbManager.ScanDevicesAsync();
                        break;

                    case "3":
                        await WirelessAdbWizard(pairingNeeded: true);
                        await _adbManager.ScanDevicesAsync();
                        break;

                    default:
                        Console.WriteLine("La scelta effettuata non è valida.");
                        break;
                }
                Console.WriteLine();
            }

            // Origin devices found
            if (_adbManager.OriginDevices.Count > 0)
            {
                // Select a device to get images from
                DeviceData originDevice = null;

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
                    Console.WriteLine($"Seleziona un dispositivo da cui estrarre le foto:");

                    int devicesCount = _adbManager.OriginDevices.Count;
                    int deviceCounter;
                    for (deviceCounter = 0; deviceCounter < devicesCount; deviceCounter++)
                    {
                        DeviceData dD = _adbManager.OriginDevices.ElementAt(deviceCounter);
                        Console.WriteLine($"({deviceCounter + 1}) - {dD.Model} ({dD.Product}){(string.IsNullOrWhiteSpace(dD.Name) ? "" : $" - \"{dD.Name}\"")}");
                    }

                    Console.WriteLine();
                    Console.Write($"Seleziona un dispositivo oppure (0) per chiudere il programma: ");

                    // Switch according to user choice
                    string deviceChoiceStr = Console.ReadLine();
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
                                Console.WriteLine("Scelta non valida");
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("Scelta non valida");
                    }
                    Console.WriteLine();
                }

                if (originDevice != null)
                {
                    bool validChoice = false;

                    do
                    {
                        // Print selected device info
                        Console.WriteLine($"Il dispositivo selezionato da sottoporre a backup è il seguente:");
                        Console.WriteLine($"({originDevice.Model} ({originDevice.Product}){(string.IsNullOrWhiteSpace(originDevice.Name) ? "" : $" - \"{originDevice.Name}\"")}");
                        Console.WriteLine("E' tutto pronto per procedere al backup delle foto.");
                        Console.WriteLine();
                        Console.Write($"Inserisci (1) per continuare oppure (0) per chiudere il programma: ");

                        // Start transfer
                        switch (Console.ReadLine())
                        {
                            case "0":
                                await Exit();
                                break;

                            case "1":
                                //await _adbManager.ScanDevicesAsync();
                                validChoice = true;
                                break;

                            default:
                                Console.WriteLine("La scelta effettuata non è valida.");
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
        catch (Exception exception)
        {
            Console.WriteLine("Errore generico nella ricerca dei dispositivi da sottoporre a backup.\n" +
                                "Il programma verrà chiuso per evitare situazioni impreviste.");
            await Exit();
        }        
    }






    /// <summary>
    /// Exits program killing ADB server.
    /// </summary>
    private static async Task Exit()
    {
        Console.WriteLine("Chiusura del servizio ADB ed uscita dal programma in corso...");
#if DEBUG
        // Just wait user input before close
        Console.WriteLine("Premi un tasto per chiudere");
        Console.ReadKey();
#endif
        await _adbManager.KillServiceAsync();
        Environment.Exit(0);
    }

#endregion

}
