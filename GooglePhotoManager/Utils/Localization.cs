namespace GooglePhotoManager.Utils
{
    /// <summary>
    /// Gestisce la localizzazione dell'applicazione.
    /// </summary>
    internal static class Localization
    {
        /// <summary>
        /// Lingue supportate.
        /// </summary>
        public enum Language
        {
            Italian,
            English
        }

        private static Language _currentLanguage = Language.Italian;

        /// <summary>
        /// Lingua corrente dell'applicazione.
        /// </summary>
        public static Language CurrentLanguage
        {
            get => _currentLanguage;
            set => _currentLanguage = value;
        }

        /// <summary>
        /// Converte una stringa in Language.
        /// </summary>
        public static Language ParseLanguage(string? value)
        {
            return value?.ToLower() switch
            {
                "english" or "en" => Language.English,
                _ => Language.Italian
            };
        }

        /// <summary>
        /// Converte Language in stringa per il salvataggio.
        /// </summary>
        public static string LanguageToString(Language lang)
        {
            return lang switch
            {
                Language.English => "English",
                _ => "Italian"
            };
        }

        /// <summary>
        /// Nome visualizzato della lingua.
        /// </summary>
        public static string GetLanguageDisplayName(Language lang)
        {
            return lang switch
            {
                Language.English => "English",
                _ => "Italiano"
            };
        }

        #region "Strings"

        // === GENERAL ===
        public static string AppName => CurrentLanguage switch
        {
            Language.English => "Google Photo Manager",
            _ => "Google Photo Manager"
        };

        public static string Yes => CurrentLanguage switch
        {
            Language.English => "Yes",
            _ => "Si"
        };

        public static string No => CurrentLanguage switch
        {
            Language.English => "No",
            _ => "No"
        };

        public static string Completed => CurrentLanguage switch
        {
            Language.English => "Completed",
            _ => "Completato"
        };

        public static string Failed => CurrentLanguage switch
        {
            Language.English => "Failed",
            _ => "Fallito"
        };

        public static string Partial => CurrentLanguage switch
        {
            Language.English => "Partial",
            _ => "Parziale"
        };

        public static string Cancel => CurrentLanguage switch
        {
            Language.English => "Cancel",
            _ => "Annulla"
        };

        public static string Back => CurrentLanguage switch
        {
            Language.English => "Back",
            _ => "Indietro"
        };

        public static string NotConfigured => CurrentLanguage switch
        {
            Language.English => "Not configured",
            _ => "Non configurato"
        };

        // === INITIALIZATION ===
        public static string InitializingAdb => CurrentLanguage switch
        {
            Language.English => "Initializing ADB...",
            _ => "Inizializzazione ADB in corso..."
        };

        public static string AdbInitialized => CurrentLanguage switch
        {
            Language.English => "ADB initialized successfully.",
            _ => "ADB inizializzato correttamente."
        };

        public static string AdbInitFailed => CurrentLanguage switch
        {
            Language.English => "ADB initialization failed.",
            _ => "Inizializzazione ADB fallita."
        };

        public static string PressEnterToClose => CurrentLanguage switch
        {
            Language.English => "Press ENTER to close...",
            _ => "Premi INVIO per chiudere..."
        };

        public static string ClosingAdb => CurrentLanguage switch
        {
            Language.English => "Closing ADB service...",
            _ => "Chiusura del servizio ADB in corso..."
        };

        public static string Goodbye => CurrentLanguage switch
        {
            Language.English => "Goodbye!",
            _ => "Arrivederci!"
        };

        // === STATUS ===
        public static string ConnectionStatus => CurrentLanguage switch
        {
            Language.English => "Connection Status",
            _ => "Stato Connessione"
        };

        public static string BackupDevice => CurrentLanguage switch
        {
            Language.English => "Backup device",
            _ => "Dispositivo di backup"
        };

        public static string Connected => CurrentLanguage switch
        {
            Language.English => "CONNECTED",
            _ => "CONNESSO"
        };

        public static string NotConnected => CurrentLanguage switch
        {
            Language.English => "NOT CONNECTED",
            _ => "NON CONNESSO"
        };

        public static string ConnectedDevices => CurrentLanguage switch
        {
            Language.English => "Connected devices",
            _ => "Dispositivi connessi"
        };

        public static string NoDeviceConnected => CurrentLanguage switch
        {
            Language.English => "No device connected.",
            _ => "Nessun dispositivo connesso."
        };

        public static string Backup => CurrentLanguage switch
        {
            Language.English => "BACKUP",
            _ => "BACKUP"
        };

        // === MAIN MENU ===
        public static string MainMenu => CurrentLanguage switch
        {
            Language.English => "Main Menu",
            _ => "Menu Principale"
        };

        public static string MenuScanDevices => CurrentLanguage switch
        {
            Language.English => "Scan devices",
            _ => "Scansiona dispositivi"
        };

        public static string MenuConnectDevice => CurrentLanguage switch
        {
            Language.English => "Connect device (ADB Wireless)",
            _ => "Connetti dispositivo (ADB Wireless)"
        };

        public static string MenuPairDevice => CurrentLanguage switch
        {
            Language.English => "Pair device (ADB Wireless)",
            _ => "Abbina dispositivo (ADB Wireless)"
        };

        public static string MenuTransferToDocuments => CurrentLanguage switch
        {
            Language.English => "Transfer files to Documents",
            _ => "Trasferisci file in Documents"
        };

        public static string MenuBackupFolder => CurrentLanguage switch
        {
            Language.English => "Backup device folder",
            _ => "Backup cartella dispositivo"
        };

        public static string MenuTransferPhotos => CurrentLanguage switch
        {
            Language.English => "Transfer photos to backup device",
            _ => "Trasferisci foto a dispositivo backup"
        };

        public static string MenuSettings => CurrentLanguage switch
        {
            Language.English => "Settings",
            _ => "Impostazioni"
        };

        public static string MenuReloadDevices => CurrentLanguage switch
        {
            Language.English => "Reload devices",
            _ => "Ricarica dispositivi"
        };

        public static string MenuExit => CurrentLanguage switch
        {
            Language.English => "Exit",
            _ => "Esci"
        };

        public static string SelectOption => CurrentLanguage switch
        {
            Language.English => "Select an option: ",
            _ => "Seleziona un'opzione: "
        };

        public static string InvalidOption => CurrentLanguage switch
        {
            Language.English => "Invalid option.",
            _ => "Opzione non valida."
        };

        // === SETTINGS MENU ===
        public static string SettingsMenu => CurrentLanguage switch
        {
            Language.English => "Settings",
            _ => "Impostazioni"
        };

        public static string SettingsSetBackupDevice => CurrentLanguage switch
        {
            Language.English => "Set backup device",
            _ => "Imposta dispositivo di backup"
        };

        public static string SettingsLanguage => CurrentLanguage switch
        {
            Language.English => "Language",
            _ => "Lingua"
        };

        public static string SettingsBack => CurrentLanguage switch
        {
            Language.English => "Back to main menu",
            _ => "Torna al menu principale"
        };

        public static string SelectLanguage => CurrentLanguage switch
        {
            Language.English => "Select language:",
            _ => "Seleziona la lingua:"
        };

        public static string LanguageChanged => CurrentLanguage switch
        {
            Language.English => "Language changed to",
            _ => "Lingua cambiata in"
        };

        public static string CurrentLanguageLabel => CurrentLanguage switch
        {
            Language.English => "Current language",
            _ => "Lingua attuale"
        };

        // === SCAN DEVICES ===
        public static string ScanningDevices => CurrentLanguage switch
        {
            Language.English => "Scanning devices...",
            _ => "Scansione dispositivi in corso..."
        };

        public static string FoundDevices(int count) => CurrentLanguage switch
        {
            Language.English => $"Found {count} device(s).",
            _ => $"Trovati {count} dispositivo/i."
        };

        public static string NoDeviceFound => CurrentLanguage switch
        {
            Language.English => "No device found.",
            _ => "Nessun dispositivo trovato."
        };

        // === WIRELESS ADB ===
        public static string WirelessAdbInfo1 => CurrentLanguage switch
        {
            Language.English => "To use ADB Wireless, the \"Wireless ADB\" option must be",
            _ => "Per utilizzare ADB Wireless e' necessario che l'opzione \"Wireless ADB\""
        };

        public static string WirelessAdbInfo2 => CurrentLanguage switch
        {
            Language.English => "enabled in the device's developer settings.",
            _ => "sia attiva nelle impostazioni sviluppatore del dispositivo."
        };

        public static string WirelessPairInfo1 => CurrentLanguage switch
        {
            Language.English => "To pair a device, go to developer settings",
            _ => "Per abbinare un dispositivo, vai nelle impostazioni sviluppatore"
        };

        public static string WirelessPairInfo2 => CurrentLanguage switch
        {
            Language.English => "and select 'Pair device with pairing code'.",
            _ => "e seleziona 'Associa dispositivo con codice di associazione'."
        };

        public static string EnterIpAddress => CurrentLanguage switch
        {
            Language.English => "Device IP address: ",
            _ => "Indirizzo IP del dispositivo: "
        };

        public static string EnterPort => CurrentLanguage switch
        {
            Language.English => "Device port: ",
            _ => "Porta del dispositivo: "
        };

        public static string EnterPairingPort => CurrentLanguage switch
        {
            Language.English => "Pairing port: ",
            _ => "Porta di associazione: "
        };

        public static string EnterPairingCode => CurrentLanguage switch
        {
            Language.English => "Pairing code: ",
            _ => "Codice di associazione: "
        };

        public static string InvalidIpAddress => CurrentLanguage switch
        {
            Language.English => "Invalid IP address.",
            _ => "Indirizzo IP non valido."
        };

        public static string InvalidPort => CurrentLanguage switch
        {
            Language.English => "Invalid port.",
            _ => "Porta non valida."
        };

        public static string InvalidPairingCode => CurrentLanguage switch
        {
            Language.English => "Invalid pairing code.",
            _ => "Codice di associazione non valido."
        };

        public static string Connecting => CurrentLanguage switch
        {
            Language.English => "Connecting...",
            _ => "Connessione in corso..."
        };

        public static string Pairing => CurrentLanguage switch
        {
            Language.English => "Pairing...",
            _ => "Associazione in corso..."
        };

        // === DEVICE SELECTION ===
        public static string SelectDevice => CurrentLanguage switch
        {
            Language.English => "Select a device: ",
            _ => "Seleziona un dispositivo: "
        };

        public static string SelectDestinationDevice => CurrentLanguage switch
        {
            Language.English => "Select destination device:",
            _ => "Seleziona il dispositivo di destinazione:"
        };

        public static string SelectSourceDevice => CurrentLanguage switch
        {
            Language.English => "Select source device:",
            _ => "Seleziona il dispositivo di origine:"
        };

        public static string SelectBackupSourceDevice => CurrentLanguage switch
        {
            Language.English => "Select device to backup:",
            _ => "Seleziona il dispositivo da cui fare backup:"
        };

        public static string NoDeviceAvailable => CurrentLanguage switch
        {
            Language.English => "No device available.",
            _ => "Nessun dispositivo disponibile."
        };

        public static string InvalidSelection => CurrentLanguage switch
        {
            Language.English => "Invalid selection.",
            _ => "Selezione non valida."
        };

        public static string SourceDevice => CurrentLanguage switch
        {
            Language.English => "Source device",
            _ => "Dispositivo origine"
        };

        // === TRANSFER TO DOCUMENTS ===
        public static string EnterFilePaths => CurrentLanguage switch
        {
            Language.English => "Enter file paths to transfer (one per line).",
            _ => "Inserisci i percorsi dei file da trasferire (uno per riga)."
        };

        public static string EnterEmptyLineToFinish => CurrentLanguage switch
        {
            Language.English => "Enter an empty line to finish.",
            _ => "Inserisci una riga vuota per terminare."
        };

        public static string FilePath => CurrentLanguage switch
        {
            Language.English => "File path: ",
            _ => "Percorso file: "
        };

        public static string FileAdded(string name) => CurrentLanguage switch
        {
            Language.English => $"Added: {name}",
            _ => $"Aggiunto: {name}"
        };

        public static string FileNotFound(string path) => CurrentLanguage switch
        {
            Language.English => $"File not found: {path}",
            _ => $"File non trovato: {path}"
        };

        public static string NoFileSelected => CurrentLanguage switch
        {
            Language.English => "No file selected.",
            _ => "Nessun file selezionato."
        };

        public static string Transferring => CurrentLanguage switch
        {
            Language.English => "Transferring...",
            _ => "Trasferimento in corso..."
        };

        public static string TransferResult => CurrentLanguage switch
        {
            Language.English => "Transfer Result",
            _ => "Risultato Trasferimento"
        };

        public static string FilesToTransfer => CurrentLanguage switch
        {
            Language.English => "Files to transfer",
            _ => "File da trasferire"
        };

        public static string FilesTransferred => CurrentLanguage switch
        {
            Language.English => "Files transferred",
            _ => "File trasferiti"
        };

        public static string Result => CurrentLanguage switch
        {
            Language.English => "Result",
            _ => "Esito"
        };

        // === BACKUP FOLDER ===
        public static string RetrievingFolders => CurrentLanguage switch
        {
            Language.English => "Retrieving folders...",
            _ => "Recupero cartelle..."
        };

        public static string NoFolderFound => CurrentLanguage switch
        {
            Language.English => "No folder found on device.",
            _ => "Nessuna cartella trovata sul dispositivo."
        };

        public static string AvailableFolders => CurrentLanguage switch
        {
            Language.English => "Available folders:",
            _ => "Cartelle disponibili:"
        };

        public static string SelectFolder => CurrentLanguage switch
        {
            Language.English => "Select a folder (number): ",
            _ => "Seleziona una cartella (numero): "
        };

        public static string BackingUp(string folder) => CurrentLanguage switch
        {
            Language.English => $"Backing up '{folder}'...",
            _ => $"Backup di '{folder}' in corso..."
        };

        public static string FolderEmpty => CurrentLanguage switch
        {
            Language.English => "Folder is empty.",
            _ => "La cartella e' vuota."
        };

        public static string BackupResult => CurrentLanguage switch
        {
            Language.English => "Backup Result",
            _ => "Risultato Backup"
        };

        public static string FilesToCopy => CurrentLanguage switch
        {
            Language.English => "Files to copy",
            _ => "File da copiare"
        };

        public static string FilesCopied => CurrentLanguage switch
        {
            Language.English => "Files copied",
            _ => "File copiati"
        };

        public static string LocalFolder => CurrentLanguage switch
        {
            Language.English => "Local folder",
            _ => "Cartella locale"
        };

        // === TRANSFER PHOTOS ===
        public static string BackupDeviceNotConnected => CurrentLanguage switch
        {
            Language.English => "Backup device is not connected.",
            _ => "Il dispositivo di backup non e' connesso."
        };

        public static string ExpectedDevice => CurrentLanguage switch
        {
            Language.English => "Expected device",
            _ => "Dispositivo atteso"
        };

        public static string NoSourceDeviceConnected => CurrentLanguage switch
        {
            Language.English => "No source device connected.",
            _ => "Nessun dispositivo di origine connesso."
        };

        public static string DeletePhotosAfterBackup => CurrentLanguage switch
        {
            Language.English => "Delete photos from source device after backup? (Y/N): ",
            _ => "Eliminare le foto dal dispositivo di origine dopo il backup? (S/N): "
        };

        public static string TransferSummary => CurrentLanguage switch
        {
            Language.English => "Transfer Summary",
            _ => "Riepilogo Trasferimento"
        };

        public static string PhotosToExtract => CurrentLanguage switch
        {
            Language.English => "Photos to extract",
            _ => "Foto da estrarre"
        };

        public static string PhotosExtracted => CurrentLanguage switch
        {
            Language.English => "Photos extracted",
            _ => "Foto estratte"
        };

        public static string PhotosToTransfer => CurrentLanguage switch
        {
            Language.English => "Photos to transfer",
            _ => "Foto da trasferire"
        };

        public static string PhotosTransferred => CurrentLanguage switch
        {
            Language.English => "Photos transferred",
            _ => "Foto trasferite"
        };

        public static string Synchronization => CurrentLanguage switch
        {
            Language.English => "Synchronization",
            _ => "Sincronizzazione"
        };

        public static string Deletion => CurrentLanguage switch
        {
            Language.English => "Deletion",
            _ => "Eliminazione"
        };

        public static string BackupCompletedSuccess => CurrentLanguage switch
        {
            Language.English => "Backup completed successfully!",
            _ => "Backup completato con successo!"
        };

        public static string BackupCompletedWithErrors => CurrentLanguage switch
        {
            Language.English => "Backup completed with some errors.",
            _ => "Backup completato con alcuni errori."
        };

        // === SET BACKUP DEVICE ===
        public static string SetBackupDeviceInfo => CurrentLanguage switch
        {
            Language.English => "Set the device to use as backup.",
            _ => "Imposta il dispositivo da usare come backup."
        };

        public static string ValuesSavedInConfig => CurrentLanguage switch
        {
            Language.English => "These values will be saved in config.xml.",
            _ => "Questi valori saranno salvati nel file config.xml."
        };

        public static string CurrentConfig => CurrentLanguage switch
        {
            Language.English => "Current configuration",
            _ => "Configurazione attuale"
        };

        public static string EnterModel => CurrentLanguage switch
        {
            Language.English => "Device Model (e.g. Pixel_5): ",
            _ => "Model del dispositivo (es. Pixel_5): "
        };

        public static string EnterProduct => CurrentLanguage switch
        {
            Language.English => "Device Product (e.g. redfin): ",
            _ => "Product del dispositivo (es. redfin): "
        };

        public static string InvalidModel => CurrentLanguage switch
        {
            Language.English => "Invalid model.",
            _ => "Model non valido."
        };

        public static string InvalidProduct => CurrentLanguage switch
        {
            Language.English => "Invalid product.",
            _ => "Product non valido."
        };

        public static string BackupDeviceSet => CurrentLanguage switch
        {
            Language.English => "Backup device set",
            _ => "Dispositivo di backup impostato"
        };

        // === ADB ERRORS ===
        public static string AdbNotFound => CurrentLanguage switch
        {
            Language.English => "ERROR: ADB not found in the system.",
            _ => "ERRORE: ADB non trovato nel sistema."
        };

        public static string AdbInstallLinux => CurrentLanguage switch
        {
            Language.English => "On Linux/Raspberry Pi install ADB with: sudo apt install adb",
            _ => "Su Linux/Raspberry Pi installa ADB con: sudo apt install adb"
        };

        public static string AdbInstallMac => CurrentLanguage switch
        {
            Language.English => "On macOS install ADB with: brew install android-platform-tools",
            _ => "Su macOS installa ADB con: brew install android-platform-tools"
        };

        public static string AdbFileNotFound(string path) => CurrentLanguage switch
        {
            Language.English => $"ADB file not found: {path}",
            _ => $"File ADB non trovato: {path}"
        };

        public static string TimeoutExpired => CurrentLanguage switch
        {
            Language.English => "Timeout expired",
            _ => "Timeout scaduto"
        };

        // === PROGRESS BAR ===
        public static string Download => CurrentLanguage switch
        {
            Language.English => "Download",
            _ => "Download"
        };

        public static string Upload => CurrentLanguage switch
        {
            Language.English => "Upload",
            _ => "Upload"
        };

        public static string DeletingFiles => CurrentLanguage switch
        {
            Language.English => "Deleting",
            _ => "Eliminazione"
        };

        // === ADB MANAGER MESSAGES ===
        public static string SettingUser => CurrentLanguage switch
        {
            Language.English => "Setting user...",
            _ => "Impostazione utente in corso..."
        };

        public static string OperationCompletedSkippedFiles => CurrentLanguage switch
        {
            Language.English => "Operation completed. Skipped the following files (not found):",
            _ => "Operazione completata. Saltati i seguenti file perche' non trovati:"
        };

        public static string SomePhotosSkippedDuringExtraction(int pulled, int total) => CurrentLanguage switch
        {
            Language.English => $"Some photos were skipped during extraction from source device ({pulled}/{total})!",
            _ => $"Alcune foto sono state saltate durante l'estrazione dal dispositivo di origine ({pulled}/{total})!"
        };

        public static string SomePhotosSkippedDuringTransfer(int pushed, int total) => CurrentLanguage switch
        {
            Language.English => $"Some photos were skipped during transfer to destination device ({pushed}/{total})!",
            _ => $"Alcune foto sono state saltate durante il trasferimento al dispositivo di destinazione ({pushed}/{total})!"
        };

        public static string PhotosNotDeletedForSafety => CurrentLanguage switch
        {
            Language.English => "Photos will not be deleted from source device for safety reasons.",
            _ => "Le foto non saranno eliminate dal dispositivo di origine per motivi di sicurezza."
        };

        public static string OperationCompletedPhotosNotFound => CurrentLanguage switch
        {
            Language.English => "Operation completed. The following photos were not found on the device:",
            _ => "Operazione completata. Le seguenti foto non sono state trovate nel dispositivo:"
        };

        public static string ContinueAnyway => CurrentLanguage switch
        {
            Language.English => "Do you want to continue anyway? (Y/N): ",
            _ => "Vuoi procedere ugualmente? (S/N): "
        };

        // === USER SELECTION ===
        public static string ScanningUsers => CurrentLanguage switch
        {
            Language.English => "Scanning users on device...",
            _ => "Scansione utenti sul dispositivo..."
        };

        public static string SelectUser => CurrentLanguage switch
        {
            Language.English => "Select a user:",
            _ => "Seleziona un utente:"
        };

        public static string NoUsersFound => CurrentLanguage switch
        {
            Language.English => "No users found on device.",
            _ => "Nessun utente trovato sul dispositivo."
        };

        public static string SwitchingUser => CurrentLanguage switch
        {
            Language.English => "Switching user profile...",
            _ => "Cambio profilo utente in corso..."
        };

        public static string CurrentActiveUser => CurrentLanguage switch
        {
            Language.English => "Current active user",
            _ => "Utente attualmente attivo"
        };

        public static string SelectedUser => CurrentLanguage switch
        {
            Language.English => "Selected user",
            _ => "Utente selezionato"
        };

        public static string UserSwitchedSuccessfully => CurrentLanguage switch
        {
            Language.English => "User profile switched successfully.",
            _ => "Profilo utente cambiato con successo."
        };

        public static string UserSwitchFailed => CurrentLanguage switch
        {
            Language.English => "Failed to switch user profile.",
            _ => "Impossibile cambiare profilo utente."
        };

        // === LOCAL FILE DELETION ===
        public static string DeleteLocalPhotosAfterTransfer => CurrentLanguage switch
        {
            Language.English => "Delete photos from local folder? (Y/N): ",
            _ => "Eliminare le foto dalla cartella locale? (S/N): "
        };

        public static string DeletingLocalFiles => CurrentLanguage switch
        {
            Language.English => "Deleting local files...",
            _ => "Eliminazione file locali in corso..."
        };

        public static string LocalFilesDeleted => CurrentLanguage switch
        {
            Language.English => "Local files deleted successfully.",
            _ => "File locali eliminati con successo."
        };

        public static string LocalFilesDeletionFailed => CurrentLanguage switch
        {
            Language.English => "Error while deleting local files.",
            _ => "Errore durante l'eliminazione dei file locali."
        };

        #endregion
    }
}
