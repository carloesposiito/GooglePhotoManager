using AdvancedSharpAdbClient.Models;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using GooglePhotoManager.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace GooglePhotoManager.Views;

public partial class CommandsView : UserControl
{
    #region Campi privati

    private AdbService _adbService = null!;
    private DeviceData? _selectedDevice;
    private List<string> _filesToTransfer = new();
    private string _selectedFolder = "";
    private string _lastResultPath = "";
    private Action<string>? _textKeypadCallback;

    private enum OpType { None, TransferDocs, BackupFolder, SwitchUser, CreateUser, Reboot }
    private OpType _currentOp = OpType.None;

    #endregion

    #region Proprietà

    // Nessuna proprietà pubblica per questo controllo

    #endregion

    #region Metodi

    public CommandsView()
    {
        InitializeComponent();
    }

    // Inizializza il servizio ADB e registra gli eventi della tastiera virtuale
    public void Initialize(AdbService adbService)
    {
        _adbService = adbService;

        TextKbd.Confirmed += OnTextKeypadConfirmed;
        TextKbd.Cancelled += OnTextKeypadCancelled;
    }

    // Riporta il tab allo stato iniziale (menu comandi visibile, operazione nascosta)
    public void Reset()
    {
        _currentOp = OpType.None;
        MenuPanel.IsVisible = true;
        OperationPanel.IsVisible = false;
        TextKeypadOverlay.IsVisible = false;
        LabelMenuError.Text = "";
        RefreshDeviceList();
    }

    // Aggiorna la lista dei dispositivi connessi
    public void RefreshDeviceList()
    {
        DeviceSelectCombo.Items.Clear();
        _selectedDevice = null;
        SetCommandsEnabled(false);

        foreach (var d in _adbService.Devices)
            DeviceSelectCombo.Items.Add($"{d.Model} ({d.Product})");

        if (_adbService.Devices.Count == 1)
            DeviceSelectCombo.SelectedIndex = 0;
    }

    // Abilita o disabilita tutti i bottoni comando
    private void SetCommandsEnabled(bool enabled)
    {
        BtnTransferDocs.IsEnabled = enabled;
        BtnBackupFolder.IsEnabled = enabled;
        BtnSwitchUser.IsEnabled = enabled;
        BtnCreateUser.IsEnabled = enabled;
        BtnReboot.IsEnabled = enabled;
    }

    // Mostra il pannello operazione con il titolo specificato e nasconde tutti i sotto-pannelli
    private void ShowOperation(string title)
    {
        MenuPanel.IsVisible = false;
        OperationPanel.IsVisible = true;
        LabelOpTitle.Text = title;
        FolderSelectPanel.IsVisible = false;
        FileListPanel.IsVisible = false;
        UserSelectPanel.IsVisible = false;
        RebootConfirmPanel.IsVisible = false;
        ProgressPanel.IsVisible = false;
        ResultPanel.IsVisible = false;
        EndButtonsPanel.IsVisible = false;
        EndButtonsWithFolderPanel.IsVisible = false;
        BtnBack.IsVisible = false;
        _lastResultPath = "";
    }

    // Mostra l'overlay della tastiera virtuale con un titolo e una callback
    private void ShowTextKeypad(string title, Action<string> callback)
    {
        TextKeypadTitle.Text = title;
        TextKbd.Text = "";
        _textKeypadCallback = callback;
        TextKeypadOverlay.IsVisible = true;
    }

    // Crea un oggetto Progress per aggiornare la barra di avanzamento
    private IProgress<(int current, int total, string fileName)> CreateProgress()
    {
        return new Progress<(int current, int total, string fileName)>(p =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                double pct = p.total > 0 ? (double)p.current / p.total * 100 : 0;
                ProgressBarOp.Value = pct;
                LabelProgressFile.Text = p.fileName;
                LabelProgressCount.Text = $"{p.current} / {p.total}";
            });
        });
    }

    // Mostra il pannello di progresso con un messaggio di stato
    private void ShowProgress(string status)
    {
        ProgressPanel.IsVisible = true;
        LabelProgressStatus.Text = status;
        ProgressBarOp.Value = 0;
        LabelProgressFile.Text = "";
        LabelProgressCount.Text = "";
        EndButtonsPanel.IsVisible = false;
        EndButtonsWithFolderPanel.IsVisible = false;
    }

    // Mostra il pannello risultato con titolo, dettagli e colore in base al successo
    private void ShowResult(string title, string details, bool success, string folderPath = "")
    {
        ProgressPanel.IsVisible = false;
        ResultPanel.IsVisible = true;
        LabelResultTitle.Text = title;
        LabelResultTitle.Foreground = new SolidColorBrush(Color.Parse(success ? "#66BB6A" : "#EF5350"));
        LabelResultDetails.Text = details;

        // Mostra i bottoni finali: con "Apri cartella" se c'e' un percorso valido
        _lastResultPath = folderPath;
        bool showFolder = !string.IsNullOrEmpty(folderPath) && Directory.Exists(folderPath);
        EndButtonsPanel.IsVisible = !showFolder;
        EndButtonsWithFolderPanel.IsVisible = showFolder;
    }

    #endregion

    #region Eventi

    // Selezione di un dispositivo dalla combobox
    private void OnDeviceSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        int idx = DeviceSelectCombo.SelectedIndex;
        if (idx < 0 || idx >= _adbService.Devices.Count)
        {
            _selectedDevice = null;
            SetCommandsEnabled(false);
            return;
        }

        _selectedDevice = _adbService.Devices[idx];
        SetCommandsEnabled(true);
    }

    // Torna al menu comandi
    private void OnBackClick(object? sender, RoutedEventArgs e)
    {
        _currentOp = OpType.None;
        MenuPanel.IsVisible = true;
        OperationPanel.IsVisible = false;
        RefreshDeviceList();
    }

    // Apre la cartella del backup nel file manager
    private void OnOpenFolderClick(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_lastResultPath) || !Directory.Exists(_lastResultPath))
            return;

        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                Process.Start("explorer.exe", _lastResultPath);
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                Process.Start("open", _lastResultPath);
            else
                Process.Start("xdg-open", _lastResultPath);
        }
        catch { }
    }

    // Conferma della tastiera virtuale
    private void OnTextKeypadConfirmed(object? sender, string value)
    {
        TextKeypadOverlay.IsVisible = false;
        _textKeypadCallback?.Invoke(value);
        _textKeypadCallback = null;
    }

    // Annullamento della tastiera virtuale
    private void OnTextKeypadCancelled(object? sender, EventArgs e)
    {
        TextKeypadOverlay.IsVisible = false;
        _textKeypadCallback = null;
    }

    // Apre il file picker per scegliere i file da trasferire in Documents
    private async void OnTransferDocsClick(object? sender, RoutedEventArgs e)
    {
        if (_selectedDevice == null) return;

        // Apre il file picker nativo per selezionare uno o piu' file
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Seleziona file da trasferire",
            AllowMultiple = true
        });

        if (files == null || files.Count == 0) return;

        _filesToTransfer = files
            .Select(f => f.TryGetLocalPath())
            .Where(p => p != null)
            .Cast<string>()
            .ToList();

        if (_filesToTransfer.Count == 0) return;

        _currentOp = OpType.TransferDocs;
        ShowOperation("Trasferisci in Documents");

        FileListPanel.IsVisible = true;
        FileList.Items.Clear();

        LabelFileListTitle.Text = $"{_filesToTransfer.Count} file selezionati:";
        foreach (var f in _filesToTransfer)
            FileList.Items.Add(Path.GetFileName(f));

        BtnStartTransfer.IsEnabled = true;
        BtnStartTransfer.Content = $"Trasferisci {_filesToTransfer.Count} file";
    }

    // Avvia il trasferimento dei file verso il dispositivo
    private async void OnStartTransferClick(object? sender, RoutedEventArgs e)
    {
        if (_currentOp != OpType.TransferDocs || _selectedDevice == null) return;
        FileListPanel.IsVisible = false;
        ShowProgress("Trasferimento in corso...");
        try
        {
            int pushed = await _adbService.PushToDocumentsAsync(_selectedDevice, _filesToTransfer, CreateProgress());
            ShowResult("Trasferimento completato", $"File trasferiti: {pushed} / {_filesToTransfer.Count}",
                pushed == _filesToTransfer.Count);
        }
        catch (Exception ex) { ShowResult("Errore", ex.Message, false); }
    }

    // Carica la lista delle cartelle dal dispositivo per il backup
    private async void OnBackupFolderClick(object? sender, RoutedEventArgs e)
    {
        if (_selectedDevice == null) return;
        _currentOp = OpType.BackupFolder;
        ShowOperation("Backup cartella");
        FolderSelectPanel.IsVisible = true;
        FolderSelectList.Items.Clear();
        FolderSelectList.Items.Add("Caricamento...");

        try
        {
            var folders = await _adbService.GetRootFoldersAsync(_selectedDevice);
            FolderSelectList.Items.Clear();
            if (folders.Count == 0) { FolderSelectList.Items.Add("Nessuna cartella"); return; }
            foreach (var f in folders) FolderSelectList.Items.Add(f);
        }
        catch { FolderSelectList.Items.Clear(); FolderSelectList.Items.Add("Errore"); }
    }

    // Selezione di una cartella per il backup
    private async void OnFolderSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (FolderSelectList.SelectedItem is not string folder) return;
        if (folder is "Caricamento..." or "Nessuna cartella" or "Errore") return;

        _selectedFolder = folder;
        FolderSelectPanel.IsVisible = false;
        ShowProgress($"Backup di '{folder}'...");
        try
        {
            var result = await _adbService.BackupFolderAsync(_selectedDevice!, _selectedFolder, CreateProgress());
            if (result.ToBePulledCount == 0)
            {
                ShowResult("Cartella vuota", "Nessun file trovato.", false);
                return;
            }

            ShowResult(
                result.AllFilesSynced ? "Backup completato" : "Backup parziale",
                $"File: {result.PulledCount} / {result.ToBePulledCount}",
                result.AllFilesSynced,
                result.FolderPath);
        }
        catch (Exception ex) { ShowResult("Errore", ex.Message, false); }
    }

    // Carica la lista utenti per il cambio utente
    private async void OnSwitchUserClick(object? sender, RoutedEventArgs e)
    {
        if (_selectedDevice == null) return;
        _currentOp = OpType.SwitchUser;
        ShowOperation("Cambia utente");
        UserSelectPanel.IsVisible = true;
        UserSelectList.Items.Clear();
        LabelUserStatus.Text = "Caricamento utenti...";

        await _adbService.GetUsersAsync(_selectedDevice);

        UserSelectList.Items.Clear();
        if (_adbService.Users.Count == 0)
        {
            LabelUserStatus.Text = "Nessun utente trovato";
            return;
        }

        foreach (var kvp in _adbService.Users)
            UserSelectList.Items.Add($"{kvp.Value.Name} (ID: {kvp.Value.Id})");
        LabelUserStatus.Text = $"{_adbService.Users.Count} utenti trovati";
    }

    // Selezione di un utente per lo switch
    private async void OnUserSelected(object? sender, SelectionChangedEventArgs e)
    {
        int idx = UserSelectList.SelectedIndex;
        if (idx < 0) return;
        var userList = _adbService.Users.Values.ToList();
        if (idx >= userList.Count) return;

        var targetUser = userList[idx];
        UserSelectPanel.IsVisible = false;
        ShowProgress($"Cambio utente a {targetUser.Name}...");

        await _adbService.SetUserAsync(targetUser, _selectedDevice);

        ShowResult("Comando inviato", $"Cambio utente a {targetUser.Name} eseguito.\nVerifica sul dispositivo.", true);
    }

    // Apre la tastiera virtuale per inserire il nome del nuovo utente
    private void OnCreateUserClick(object? sender, RoutedEventArgs e)
    {
        if (_selectedDevice == null) return;
        _currentOp = OpType.CreateUser;

        ShowTextKeypad("Nome nuovo utente", async (name) =>
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                LabelMenuError.Text = "Nome utente vuoto!";
                return;
            }

            ShowOperation("Crea utente");
            ShowProgress($"Creazione utente '{name}'...");

            string result = await _adbService.CreateUserAsync(_selectedDevice, name.Trim());

            bool success = result.Contains("Success", StringComparison.OrdinalIgnoreCase);
            ShowResult(success ? "Utente creato" : "Errore", result, success);
        });
    }

    // Mostra la conferma per il riavvio del dispositivo
    private void OnRebootClick(object? sender, RoutedEventArgs e)
    {
        if (_selectedDevice == null) return;
        _currentOp = OpType.Reboot;
        ShowOperation("Riavvia dispositivo");
        RebootConfirmPanel.IsVisible = true;
    }

    // Conferma il riavvio del dispositivo
    private async void OnRebootConfirmYes(object? sender, RoutedEventArgs e)
    {
        RebootConfirmPanel.IsVisible = false;
        ShowProgress("Riavvio in corso...");
        string result = await _adbService.RebootDeviceAsync(_selectedDevice!);
        ShowResult("Riavvio", result, !result.StartsWith("Errore"));
    }

    #endregion
}
