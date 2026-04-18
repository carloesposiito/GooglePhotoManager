using AdvancedSharpAdbClient.Models;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using GooglePhotoManager.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace GooglePhotoManager.Views;

public partial class CommandsView : UserControl
{
    private AdbService _adbService = null!;
    private DeviceData? _selectedDevice;
    private List<string> _filesToTransfer = new();
    private string _selectedFolder = "";
    private Action<string>? _textKeypadCallback;

    private enum OpType { None, TransferDocs, BackupFolder, SwitchUser, CreateUser, DeviceInfo, Reboot }
    private OpType _currentOp = OpType.None;

    public CommandsView()
    {
        InitializeComponent();
    }

    public void Initialize(AdbService adbService)
    {
        _adbService = adbService;

        TextKbd.Confirmed += OnTextKeypadConfirmed;
        TextKbd.Cancelled += OnTextKeypadCancelled;
    }

    public void RefreshDeviceList()
    {
        DeviceSelectList.Items.Clear();
        _selectedDevice = null;
        SetCommandsEnabled(false);
        LabelSelectedDevice.Text = "Nessun dispositivo selezionato";

        foreach (var d in _adbService.Devices)
        {
            DeviceSelectList.Items.Add($"{d.Model} ({d.Product})");
        }

        if (_adbService.Devices.Count == 1)
            DeviceSelectList.SelectedIndex = 0;
    }

    private void SetCommandsEnabled(bool enabled)
    {
        BtnTransferDocs.IsEnabled = enabled;
        BtnBackupFolder.IsEnabled = enabled;
        BtnSwitchUser.IsEnabled = enabled;
        BtnCreateUser.IsEnabled = enabled;
        BtnDeviceInfo.IsEnabled = enabled;
        BtnReboot.IsEnabled = enabled;
    }

    private void OnDeviceSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        int idx = DeviceSelectList.SelectedIndex;
        if (idx < 0 || idx >= _adbService.Devices.Count)
        {
            _selectedDevice = null;
            SetCommandsEnabled(false);
            LabelSelectedDevice.Text = "Nessun dispositivo selezionato";
            return;
        }

        _selectedDevice = _adbService.Devices[idx];
        SetCommandsEnabled(true);
        LabelSelectedDevice.Text = $"{_selectedDevice.Model}";
    }

    // --- Navigation ---

    private void ShowOperation(string title)
    {
        MenuPanel.IsVisible = false;
        OperationPanel.IsVisible = true;
        LabelOpTitle.Text = title;
        FolderSelectPanel.IsVisible = false;
        FileListPanel.IsVisible = false;
        UserSelectPanel.IsVisible = false;
        DeviceInfoPanel.IsVisible = false;
        RebootConfirmPanel.IsVisible = false;
        ProgressPanel.IsVisible = false;
        ResultPanel.IsVisible = false;
        BtnBack.IsVisible = true;
    }

    private void OnBackClick(object? sender, RoutedEventArgs e)
    {
        _currentOp = OpType.None;
        MenuPanel.IsVisible = true;
        OperationPanel.IsVisible = false;
        RefreshDeviceList();
    }

    // --- Text keypad ---

    private void ShowTextKeypad(string title, Action<string> callback)
    {
        TextKeypadTitle.Text = title;
        TextKbd.Text = "";
        _textKeypadCallback = callback;
        TextKeypadOverlay.IsVisible = true;
    }

    private void OnTextKeypadConfirmed(object? sender, string value)
    {
        TextKeypadOverlay.IsVisible = false;
        _textKeypadCallback?.Invoke(value);
        _textKeypadCallback = null;
    }

    private void OnTextKeypadCancelled(object? sender, EventArgs e)
    {
        TextKeypadOverlay.IsVisible = false;
        _textKeypadCallback = null;
    }

    // ==================== Transfer to Documents ====================

    private void OnTransferDocsClick(object? sender, RoutedEventArgs e)
    {
        if (_selectedDevice == null) return;
        _currentOp = OpType.TransferDocs;
        ShowOperation("Trasferisci in Documents");

        string transferDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "ToTransfer");
        if (!Directory.Exists(transferDir)) Directory.CreateDirectory(transferDir);

        _filesToTransfer = Directory.GetFiles(transferDir).ToList();
        FileListPanel.IsVisible = true;
        FileList.Items.Clear();

        if (_filesToTransfer.Count == 0)
        {
            LabelFileListTitle.Text = $"Nessun file in {transferDir}";
            BtnStartTransfer.IsEnabled = false;
            BtnStartTransfer.Content = "Nessun file";
        }
        else
        {
            LabelFileListTitle.Text = $"{_filesToTransfer.Count} file:";
            foreach (var f in _filesToTransfer) FileList.Items.Add(Path.GetFileName(f));
            BtnStartTransfer.IsEnabled = true;
            BtnStartTransfer.Content = $"Trasferisci {_filesToTransfer.Count} file";
        }
    }

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

    // ==================== Backup Folder ====================

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
            if (result.ToBePulledCount == 0) { ShowResult("Cartella vuota", "Nessun file trovato.", false); return; }
            ShowResult(result.AllFilesSynced ? "Backup completato" : "Backup parziale",
                $"File: {result.PulledCount} / {result.ToBePulledCount}\nSalvati in: {result.FolderPath}",
                result.AllFilesSynced);
        }
        catch (Exception ex) { ShowResult("Errore", ex.Message, false); }
    }

    // ==================== Switch User ====================

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

    // ==================== Create User ====================

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

    // ==================== Device Info ====================

    private async void OnDeviceInfoClick(object? sender, RoutedEventArgs e)
    {
        if (_selectedDevice == null) return;
        _currentOp = OpType.DeviceInfo;
        ShowOperation("Info dispositivo");
        DeviceInfoPanel.IsVisible = true;

        InfoList.Items.Clear();
        InfoList.Items.Add("Caricamento...");

        var info = await _adbService.GetDeviceInfoAsync(_selectedDevice);

        InfoList.Items.Clear();
        foreach (var kvp in info)
        {
            InfoList.Items.Add(new TextBlock
            {
                Text = $"{kvp.Key}: {kvp.Value}",
                FontSize = 15,
                Foreground = new SolidColorBrush(Color.Parse("#E0E0E0")),
                Margin = new Avalonia.Thickness(0, 2)
            });
        }
    }

    // ==================== Reboot ====================

    private void OnRebootClick(object? sender, RoutedEventArgs e)
    {
        if (_selectedDevice == null) return;
        _currentOp = OpType.Reboot;
        ShowOperation("Riavvia dispositivo");
        RebootConfirmPanel.IsVisible = true;
    }

    private async void OnRebootConfirmYes(object? sender, RoutedEventArgs e)
    {
        RebootConfirmPanel.IsVisible = false;
        ShowProgress("Riavvio in corso...");
        string result = await _adbService.RebootDeviceAsync(_selectedDevice!);
        ShowResult("Riavvio", result, !result.StartsWith("Errore"));
    }

    // ==================== Helpers ====================

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

    private void ShowProgress(string status)
    {
        ProgressPanel.IsVisible = true;
        LabelProgressStatus.Text = status;
        ProgressBarOp.Value = 0;
        LabelProgressFile.Text = "";
        LabelProgressCount.Text = "";
        BtnBack.IsVisible = false;
    }

    private void ShowResult(string title, string details, bool success)
    {
        ProgressPanel.IsVisible = false;
        ResultPanel.IsVisible = true;
        LabelResultTitle.Text = title;
        LabelResultTitle.Foreground = new SolidColorBrush(Color.Parse(success ? "#66BB6A" : "#EF5350"));
        ResultPanel.Background = new SolidColorBrush(Color.Parse(success ? "#1B5E20" : "#3E2723"));
        LabelResultDetails.Text = details;
        BtnBack.IsVisible = true;
    }
}
