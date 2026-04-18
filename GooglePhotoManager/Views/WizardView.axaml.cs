using AdvancedSharpAdbClient.Models;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using GooglePhotoManager.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GooglePhotoManager.Views;

public partial class WizardView : UserControl
{
    private AdbService _adbService = null!;

    private DeviceData? _backupDevice;
    private MyUser? _selectedUser;
    private DeviceData? _sourceDevice;
    private bool _deleteFromSource;

    public WizardView()
    {
        InitializeComponent();
    }

    public void Initialize(AdbService adbService)
    {
        _adbService = adbService;
    }

    private void GoToStep(int step)
    {
        Step0.IsVisible = step == 0;
        Step1.IsVisible = step == 1;
        Step2.IsVisible = step == 2;
        Step3.IsVisible = step == 3;
        Step4.IsVisible = step == 4;
    }

    // ===================== STEP 0: Home =====================

    private void OnStartWizardClick(object? sender, RoutedEventArgs e)
    {
        if (!_adbService.IsInitialized)
        {
            LabelStep0Error.Text = "ADB non inizializzato! Vai al tab Dispositivi.";
            return;
        }

        if (_adbService.Devices.Count == 0)
        {
            LabelStep0Error.Text = "Nessun dispositivo connesso! Vai al tab Dispositivi.";
            return;
        }

        if (_adbService.Devices.Count < 2)
        {
            LabelStep0Error.Text = "Servono almeno 2 dispositivi (sorgente + backup).";
            return;
        }

        LabelStep0Error.Text = "";
        _backupDevice = null;
        _selectedUser = null;
        _sourceDevice = null;

        // Popola lista dispositivi backup
        BackupDeviceList.Items.Clear();
        foreach (var d in _adbService.Devices)
        {
            BackupDeviceList.Items.Add($"{d.Model} ({d.Product})");
        }

        GoToStep(1);
    }

    private void OnBackToStep0(object? sender, RoutedEventArgs e)
    {
        GoToStep(0);
        LabelStep0Error.Text = "";
    }

    // ===================== STEP 1: Backup Device =====================

    private async void OnStep1Next(object? sender, RoutedEventArgs e)
    {
        int idx = BackupDeviceList.SelectedIndex;
        if (idx < 0 || idx >= _adbService.Devices.Count)
        {
            LabelStep1Error.Text = "Seleziona un dispositivo!";
            return;
        }

        LabelStep1Error.Text = "";
        _backupDevice = _adbService.Devices[idx];

        GoToStep(2);
        await LoadUsersForStep2();
    }

    private void OnBackToStep1(object? sender, RoutedEventArgs e)
    {
        GoToStep(1);
        LabelStep1Error.Text = "";
    }

    // ===================== STEP 2: Users =====================

    private async Task LoadUsersForStep2()
    {
        LabelStep2Device.Text = $"su {_backupDevice!.Model} ({_backupDevice.Product})";
        LabelStep2Loading.IsVisible = true;
        UserSelectList.IsVisible = false;
        LabelStep2Warning.IsVisible = false;
        BtnStep2Next.IsEnabled = false;
        LabelStep2Error.Text = "";

        await _adbService.GetUsersAsync(_backupDevice);
        var currentUser = await _adbService.GetCurrentUserAsync(_backupDevice);
        _adbService.CurrentUser = currentUser;

        UserSelectList.Items.Clear();

        if (_adbService.Users.Count == 0)
        {
            LabelStep2Loading.Text = "Nessun utente trovato sul dispositivo.";
            // Permetti comunque di andare avanti (utente 0 di default)
            _selectedUser = null;
            BtnStep2Next.IsEnabled = true;
            return;
        }

        LabelStep2Loading.IsVisible = false;
        UserSelectList.IsVisible = true;

        int preSelectIdx = -1;
        int i = 0;
        foreach (var kvp in _adbService.Users)
        {
            string tag = currentUser != null && currentUser.Name == kvp.Value.Name
                ? "  [attivo]" : "";
            UserSelectList.Items.Add($"{kvp.Value.Name}{tag}");

            if (currentUser != null && currentUser.Name == kvp.Value.Name)
                preSelectIdx = i;
            i++;
        }

        if (preSelectIdx >= 0)
            UserSelectList.SelectedIndex = preSelectIdx;

        BtnStep2Next.IsEnabled = true;
    }

    private async void OnStep2Next(object? sender, RoutedEventArgs e)
    {
        LabelStep2Error.Text = "";

        // Determina utente selezionato
        int idx = UserSelectList.SelectedIndex;
        if (_adbService.Users.Count > 0 && idx >= 0)
        {
            var userList = _adbService.Users.Values.ToList();
            if (idx < userList.Count)
            {
                _selectedUser = userList[idx];

                // Se l'utente attivo e' noto e coincide, vai avanti senza switch
                var currentUser = _adbService.CurrentUser;
                if (currentUser != null && currentUser.Name == _selectedUser.Name)
                {
                    ProceedToStep3();
                    return;
                }

                // In tutti gli altri casi (utente diverso, o utente attivo sconosciuto)
                // esegui switch e chiedi conferma manuale
                await ExecuteUserSwitch();
                return;
            }
        }

        // Nessun utente nella lista, vai avanti
        ProceedToStep3();
    }

    private async Task ExecuteUserSwitch()
    {
        // Nascondi bottoni normali, mostra pannello conferma
        Step2NormalButtons.IsVisible = false;
        UserSelectList.IsEnabled = false;
        Step2ConfirmPanel.IsVisible = true;
        LabelSwitchStatus.Text = $"Cambio utente a {_selectedUser!.Name} in corso...";
        LabelStep2Error.Text = "";

        // Invia comando switch sul device scelto nel wizard
        await _adbService.SetUserAsync(_selectedUser, _backupDevice);

        // Ora chiedi all'utente se ha funzionato
        LabelSwitchStatus.Text = $"Comando di cambio a {_selectedUser.Name} inviato.";
    }

    private void OnSwitchConfirmYes(object? sender, RoutedEventArgs e)
    {
        // L'utente conferma che il cambio ha avuto successo
        _adbService.CurrentUser = _selectedUser;
        Step2ConfirmPanel.IsVisible = false;
        Step2NormalButtons.IsVisible = true;
        UserSelectList.IsEnabled = true;
        ProceedToStep3();
    }

    private async void OnSwitchConfirmRetry(object? sender, RoutedEventArgs e)
    {
        // Riprova il cambio utente
        LabelSwitchStatus.Text = $"Nuovo tentativo di cambio a {_selectedUser!.Name}...";
        LabelStep2Error.Text = "";

        await _adbService.SetUserAsync(_selectedUser, _backupDevice);

        LabelSwitchStatus.Text = $"Comando di cambio a {_selectedUser.Name} inviato.";
    }

    private void OnSwitchConfirmExit(object? sender, RoutedEventArgs e)
    {
        // Esci dal wizard, torna alla home
        Step2ConfirmPanel.IsVisible = false;
        Step2NormalButtons.IsVisible = true;
        UserSelectList.IsEnabled = true;
        GoToStep(0);
        LabelStep0Error.Text = "";
    }

    private void ProceedToStep3()
    {
        // Popola step 3 con dispositivi sorgente (tutti tranne il backup)
        var sourceDevices = _adbService.Devices
            .Where(d => d.Serial != _backupDevice!.Serial)
            .ToList();

        if (sourceDevices.Count == 0)
        {
            LabelStep2Error.Text = "Nessun dispositivo sorgente disponibile!";
            Step2NormalButtons.IsVisible = true;
            BtnStep2Next.IsEnabled = true;
            return;
        }

        SourceDeviceList.Items.Clear();
        foreach (var d in sourceDevices)
            SourceDeviceList.Items.Add($"{d.Model} ({d.Product}) - {d.Serial}");

        if (sourceDevices.Count == 1)
            SourceDeviceList.SelectedIndex = 0;

        ChkDeleteFromSource.IsChecked = false;
        GoToStep(3);
    }

    private void OnBackToStep2(object? sender, RoutedEventArgs e)
    {
        Step2ConfirmPanel.IsVisible = false;
        Step2NormalButtons.IsVisible = true;
        UserSelectList.IsEnabled = true;
        GoToStep(2);
        LabelStep2Error.Text = "";
        BtnStep2Next.IsEnabled = true;
    }

    // ===================== STEP 3: Source + Options =====================

    private void OnStep3Start(object? sender, RoutedEventArgs e)
    {
        // Trova il device sorgente selezionato
        int idx = SourceDeviceList.SelectedIndex;
        var sourceDevices = _adbService.Devices
            .Where(d => d.Serial != _backupDevice!.Serial)
            .ToList();

        if (idx < 0 || idx >= sourceDevices.Count)
        {
            LabelStep3Error.Text = "Seleziona un dispositivo sorgente!";
            return;
        }

        _sourceDevice = sourceDevices[idx];
        _deleteFromSource = ChkDeleteFromSource.IsChecked == true;

        LabelStep3Error.Text = "";
        GoToStep(4);
        _ = RunWizardTransfer();
    }

    // ===================== STEP 4: Transfer =====================

    private async Task RunWizardTransfer()
    {
        WizardProgressPanel.IsVisible = true;
        WizardResultPanel.IsVisible = false;
        WizardEndButtons.IsVisible = false;

        LabelWizardStatus.Text = "Scansione foto...";
        WizardProgressBar.Value = 0;
        LabelWizardFile.Text = "";
        LabelWizardCount.Text = "";

        var progress = new Progress<(int current, int total, string fileName)>(p =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                double pct = p.total > 0 ? (double)p.current / p.total * 100 : 0;
                WizardProgressBar.Value = pct;
                LabelWizardFile.Text = p.fileName;
                LabelWizardCount.Text = $"{p.current} / {p.total}";
            });
        });

        try
        {
            var result = await _adbService.TransferPhotosAsync(
                _sourceDevice!, _backupDevice!, _deleteFromSource, progress);

            WizardProgressPanel.IsVisible = false;
            WizardResultPanel.IsVisible = true;
            WizardEndButtons.IsVisible = true;

            if (result.ToBePulledCount == 0)
            {
                LabelWizardResultIcon.Text = "=";
                LabelWizardResultTitle.Text = "Nessuna foto nuova";
                LabelWizardResultTitle.Foreground = new SolidColorBrush(Color.Parse("#9E9E9E"));
                WizardResultPanel.Background = new SolidColorBrush(Color.Parse("#1E1E1E"));
                LabelWizardResultDetails.Text = "Tutte le foto sono gia presenti sul dispositivo di backup.";
                LabelWizardResultPath.Text = "";
            }
            else if (result.AllFilesSynced)
            {
                LabelWizardResultIcon.Text = "OK";
                LabelWizardResultTitle.Text = "Backup completato!";
                LabelWizardResultTitle.Foreground = new SolidColorBrush(Color.Parse("#66BB6A"));
                WizardResultPanel.Background = new SolidColorBrush(Color.Parse("#1B5E20"));

                string details = $"Foto estratte: {result.PulledCount} / {result.ToBePulledCount}\n" +
                                 $"Foto trasferite: {result.PushedCount} / {result.ToBePushedCount}";
                if (_deleteFromSource)
                    details += $"\nCancellazione: {(result.DeleteCompleted ? "completata" : "non completata")}";
                LabelWizardResultDetails.Text = details;
                LabelWizardResultPath.Text = $"Cartella temp: {result.FolderPath}";
            }
            else
            {
                LabelWizardResultIcon.Text = "!";
                LabelWizardResultTitle.Text = "Backup con errori";
                LabelWizardResultTitle.Foreground = new SolidColorBrush(Color.Parse("#FFB74D"));
                WizardResultPanel.Background = new SolidColorBrush(Color.Parse("#37474F"));

                string details = $"Foto estratte: {result.PulledCount} / {result.ToBePulledCount}\n" +
                                 $"Foto trasferite: {result.PushedCount} / {result.ToBePushedCount}";
                if (_deleteFromSource)
                    details += "\nCancellazione: non eseguita per sicurezza";
                LabelWizardResultDetails.Text = details;
                LabelWizardResultPath.Text = $"Cartella temp: {result.FolderPath}";
            }
        }
        catch (Exception ex)
        {
            WizardProgressPanel.IsVisible = false;
            WizardResultPanel.IsVisible = true;
            WizardEndButtons.IsVisible = true;

            LabelWizardResultIcon.Text = "X";
            LabelWizardResultTitle.Text = "Errore";
            LabelWizardResultTitle.Foreground = new SolidColorBrush(Color.Parse("#EF5350"));
            WizardResultPanel.Background = new SolidColorBrush(Color.Parse("#3E2723"));
            LabelWizardResultDetails.Text = ex.Message;
            LabelWizardResultPath.Text = "";
        }
    }

    private void OnNewWizardClick(object? sender, RoutedEventArgs e)
    {
        OnStartWizardClick(sender, e);
    }
}
