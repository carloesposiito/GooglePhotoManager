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

public partial class WizardView : UserControl
{
    #region Campi privati

    private AdbService _adbService = null!;
    private DeviceData? _backupDevice;
    private MyUser? _selectedUser;
    private DeviceData? _sourceDevice;
    private bool _deleteFromSource;
    private string _lastTransferPath = "";

    #endregion

    #region Proprietà

    // Nessuna proprietà pubblica per questo controllo

    #endregion

    #region Metodi

    public WizardView()
    {
        InitializeComponent();
    }

    // Inizializza il servizio ADB e mostra il bottone simulazione in DEBUG
    public void Initialize(AdbService adbService)
    {
        _adbService = adbService;

#if DEBUG
        BtnSimulate.IsVisible = true;
#endif
    }

    // Cambia lo step visibile nel wizard
    private void GoToStep(int step)
    {
        Step0.IsVisible = step == 0;
        Step1.IsVisible = step == 1;
        Step2.IsVisible = step == 2;
        Step3.IsVisible = step == 3;
        Step4.IsVisible = step == 4;
    }

    // Carica la lista utenti dal dispositivo di backup
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
            _selectedUser = null;
            BtnStep2Next.IsEnabled = true;
            return;
        }

        LabelStep2Loading.IsVisible = false;
        UserSelectList.IsVisible = true;

        // Pre-seleziona l'utente attivo
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

    // Invia il comando di cambio utente e mostra il pannello di conferma
    private async Task ExecuteUserSwitch()
    {
        Step2NormalButtons.IsVisible = false;
        UserSelectList.IsEnabled = false;
        Step2ConfirmPanel.IsVisible = true;
        LabelSwitchStatus.Text = $"Cambio utente a {_selectedUser!.Name} in corso...";
        LabelStep2Error.Text = "";

        await _adbService.SetUserAsync(_selectedUser, _backupDevice);

        LabelSwitchStatus.Text = $"Comando di cambio a {_selectedUser.Name} inviato.";
    }

    // Prepara lo step 3 con i dispositivi sorgente disponibili
    private void ProceedToStep3()
    {
        // Filtra: tutti i dispositivi tranne quello di backup
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

    // Esegue il trasferimento foto tra dispositivo sorgente e backup
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

            ShowTransferResult(result);
        }
        catch (Exception ex)
        {
            WizardProgressPanel.IsVisible = false;
            WizardResultPanel.IsVisible = true;
            WizardEndButtons.IsVisible = true;

            LabelWizardResultIcon.Text = "Errore";
            LabelWizardResultIcon.Foreground = new SolidColorBrush(Color.Parse("#EF5350"));
            LabelWizardResultDetails.Text = ex.Message;
            LabelWizardResultPath.Text = "";
        }
    }

    // Mostra il risultato del trasferimento nel pannello finale
    private void ShowTransferResult(TransferResult result)
    {
        _lastTransferPath = result.FolderPath ?? "";

        if (result.ToBePulledCount == 0)
        {
            LabelWizardResultIcon.Text = "Nessuna foto nuova";
            LabelWizardResultIcon.Foreground = new SolidColorBrush(Color.Parse("#9E9E9E"));
            LabelWizardResultDetails.Text = "Tutte le foto sono gia presenti sul dispositivo di backup.";
            LabelWizardResultPath.Text = "";
        }
        else if (result.AllFilesSynced)
        {
            LabelWizardResultIcon.Text = "Backup completato!";
            LabelWizardResultIcon.Foreground = new SolidColorBrush(Color.Parse("#66BB6A"));

            string details = $"Foto estratte: {result.PulledCount} / {result.ToBePulledCount}\n" +
                             $"Foto trasferite: {result.PushedCount} / {result.ToBePushedCount}";
            if (_deleteFromSource)
                details += $"\nCancellazione: {(result.DeleteCompleted ? "completata" : "non completata")}";
            LabelWizardResultDetails.Text = details;
            LabelWizardResultPath.Text = $"Cartella temp: {result.FolderPath}";
        }
        else
        {
            LabelWizardResultIcon.Text = "Backup con errori";
            LabelWizardResultIcon.Foreground = new SolidColorBrush(Color.Parse("#FFB74D"));

            string details = $"Foto estratte: {result.PulledCount} / {result.ToBePulledCount}\n" +
                             $"Foto trasferite: {result.PushedCount} / {result.ToBePushedCount}";
            if (_deleteFromSource)
                details += "\nCancellazione: non eseguita per sicurezza";
            LabelWizardResultDetails.Text = details;
            LabelWizardResultPath.Text = $"Cartella temp: {result.FolderPath}";
        }

        // Abilita "Elimina cartella temp" solo se tutti i file sono stati trasferiti
        BtnDeleteTemp.IsEnabled = result.AllFilesSynced
            && !string.IsNullOrEmpty(_lastTransferPath)
            && Directory.Exists(_lastTransferPath);
    }

    #endregion

    #region Eventi

    // Carica dispositivi simulati e avvia il wizard
    private void OnSimulateClick(object? sender, RoutedEventArgs e)
    {
        _adbService.LoadSimulatedDevices();
        OnStartWizardClick(sender, e);
    }

    // Click su AVVIA nella schermata home
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

        BackupDeviceList.Items.Clear();
        foreach (var d in _adbService.Devices)
            BackupDeviceList.Items.Add($"{d.Model} ({d.Product})");

        GoToStep(1);
    }

    // Torna allo step 0
    private void OnBackToStep0(object? sender, RoutedEventArgs e)
    {
        GoToStep(0);
        LabelStep0Error.Text = "";
    }

    // Conferma la selezione del dispositivo di backup e passa allo step 2
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

    // Torna allo step 1
    private void OnBackToStep1(object? sender, RoutedEventArgs e)
    {
        GoToStep(1);
        LabelStep1Error.Text = "";
    }

    // Conferma la selezione dell'utente e gestisce l'eventuale switch
    private async void OnStep2Next(object? sender, RoutedEventArgs e)
    {
        LabelStep2Error.Text = "";

        int idx = UserSelectList.SelectedIndex;
        if (_adbService.Users.Count > 0 && idx >= 0)
        {
            var userList = _adbService.Users.Values.ToList();
            if (idx < userList.Count)
            {
                _selectedUser = userList[idx];

                // Se l'utente selezionato e' gia' attivo, salta lo switch
                var currentUser = _adbService.CurrentUser;
                if (currentUser != null && currentUser.Name == _selectedUser.Name)
                {
                    ProceedToStep3();
                    return;
                }

                await ExecuteUserSwitch();
                return;
            }
        }

        ProceedToStep3();
    }

    // L'utente conferma che il cambio utente ha avuto successo
    private void OnSwitchConfirmYes(object? sender, RoutedEventArgs e)
    {
        _adbService.CurrentUser = _selectedUser;
        Step2ConfirmPanel.IsVisible = false;
        Step2NormalButtons.IsVisible = true;
        UserSelectList.IsEnabled = true;
        ProceedToStep3();
    }

    // Riprova il cambio utente
    private async void OnSwitchConfirmRetry(object? sender, RoutedEventArgs e)
    {
        LabelSwitchStatus.Text = $"Nuovo tentativo di cambio a {_selectedUser!.Name}...";
        LabelStep2Error.Text = "";

        await _adbService.SetUserAsync(_selectedUser, _backupDevice);

        LabelSwitchStatus.Text = $"Comando di cambio a {_selectedUser.Name} inviato.";
    }

    // Annulla il wizard e torna alla home
    private void OnSwitchConfirmExit(object? sender, RoutedEventArgs e)
    {
        Step2ConfirmPanel.IsVisible = false;
        Step2NormalButtons.IsVisible = true;
        UserSelectList.IsEnabled = true;
        GoToStep(0);
        LabelStep0Error.Text = "";
    }

    // Torna allo step 2
    private void OnBackToStep2(object? sender, RoutedEventArgs e)
    {
        Step2ConfirmPanel.IsVisible = false;
        Step2NormalButtons.IsVisible = true;
        UserSelectList.IsEnabled = true;
        GoToStep(2);
        LabelStep2Error.Text = "";
        BtnStep2Next.IsEnabled = true;
    }

    // Avvia il backup dallo step 3
    private void OnStep3Start(object? sender, RoutedEventArgs e)
    {
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

    // Elimina la cartella temporanea usata per il trasferimento
    private void OnDeleteTempClick(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_lastTransferPath) || !Directory.Exists(_lastTransferPath))
            return;

        try
        {
            Directory.Delete(_lastTransferPath, true);
            BtnDeleteTemp.IsEnabled = false;
            BtnDeleteTemp.Content = "Cartella eliminata";
            LabelWizardResultPath.Text = "Cartella temp eliminata.";
        }
        catch (Exception ex)
        {
            LabelWizardResultPath.Text = $"Errore: {ex.Message}";
        }
    }

    #endregion
}
