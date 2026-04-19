using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace GooglePhotoManager.Views;

public partial class DevicesView : UserControl
{
    #region Campi privati

    private AdbService _adbService = null!;
    private string _ip = "";
    private string _port = "5555";
    private string _pairingCode = "";
    private Action<string>? _keypadCallback;

    #endregion

    #region Proprietà

    // Nessuna proprietà pubblica per questo controllo

    #endregion

    #region Metodi

    public DevicesView()
    {
        InitializeComponent();
    }

    // Inizializza il servizio ADB e registra gli eventi del tastierino numerico
    public void Initialize(AdbService adbService)
    {
        _adbService = adbService;

        Keypad.Confirmed += OnKeypadConfirmed;
        Keypad.Cancelled += OnKeypadCancelled;
    }

    // Inizializza ADB se necessario, poi scansiona i dispositivi connessi
    private async Task ScanDevicesAsync()
    {
        BtnScan.IsEnabled = false;
        DeviceList.Items.Clear();

        // Se ADB non e' ancora inizializzato, lo avvia automaticamente
        if (!_adbService.IsInitialized)
        {
            LabelStatus.Text = "Inizializzazione ADB...";
            LabelStatus.Foreground = new SolidColorBrush(Color.Parse("#9E9E9E"));

            bool initOk = await _adbService.InitializeAsync();
            if (!initOk)
            {
                string hint = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                    ? "ADB non trovato! Metti adb.exe in PlatformTools/ o installa Android SDK"
                    : "ADB non trovato! Installa con: sudo apt install adb";
                LabelStatus.Text = hint;
                LabelStatus.Foreground = new SolidColorBrush(Color.Parse("#EF5350"));
                BtnScan.IsEnabled = true;
                return;
            }
        }

        LabelStatus.Text = "Scansione in corso...";
        LabelStatus.Foreground = new SolidColorBrush(Color.Parse("#9E9E9E"));

        await _adbService.ScanDevicesAsync();
        var devices = _adbService.Devices;

        if (devices.Count > 0)
        {
            foreach (var device in devices)
            {
                string info = $"{device.Model} ({device.Product}) - {device.Serial}";
                DeviceList.Items.Add(info);
            }

            LabelStatus.Text = "";
            LabelDevicesHeader.Text = $"Dispositivi connessi [{devices.Count} dispositivi]";
        }
        else
        {
            LabelStatus.Text = "Nessun dispositivo trovato";
            LabelStatus.Foreground = new SolidColorBrush(Color.Parse("#FFB74D"));
            LabelDevicesHeader.Text = "Dispositivi connessi [0 dispositivi]";
        }

        BtnScan.IsEnabled = true;
    }

    // Mostra l'overlay del tastierino numerico
    private void ShowKeypad(string title, string currentValue, Action<string> callback)
    {
        KeypadTitle.Text = title;
        Keypad.Text = currentValue;
        _keypadCallback = callback;
        KeypadOverlay.IsVisible = true;
        MainContent.IsEnabled = false;
    }

    // Nasconde l'overlay del tastierino numerico
    private void HideKeypad()
    {
        KeypadOverlay.IsVisible = false;
        MainContent.IsEnabled = true;
        _keypadCallback = null;
    }

    #endregion

    #region Eventi

    // Avvia la scansione dei dispositivi (e inizializza ADB se serve)
    private async void OnScanClick(object? sender, RoutedEventArgs e)
    {
        await ScanDevicesAsync();
    }

    // Conferma del tastierino numerico
    private void OnKeypadConfirmed(object? sender, string value)
    {
        _keypadCallback?.Invoke(value);
        HideKeypad();
    }

    // Annullamento del tastierino numerico
    private void OnKeypadCancelled(object? sender, EventArgs e)
    {
        HideKeypad();
    }

    // Apre il tastierino per inserire l'indirizzo IP
    private void OnIpInputClick(object? sender, RoutedEventArgs e)
    {
        ShowKeypad("Inserisci indirizzo IP", _ip, value =>
        {
            _ip = value;
            BtnIpInput.Content = string.IsNullOrEmpty(value) ? "IP: tocca" : $"IP: {value}";
            BtnIpInput.Foreground = string.IsNullOrEmpty(value)
                ? new SolidColorBrush(Color.Parse("#9E9E9E"))
                : new SolidColorBrush(Color.Parse("#E0E0E0"));
        });
    }

    // Apre il tastierino per inserire la porta
    private void OnPortInputClick(object? sender, RoutedEventArgs e)
    {
        ShowKeypad("Inserisci porta", _port, value =>
        {
            _port = value;
            BtnPortInput.Content = string.IsNullOrEmpty(value) ? "Porta: 5555" : $"Porta: {value}";
        });
    }

    // Apre il tastierino per inserire il codice di pairing
    private void OnCodeInputClick(object? sender, RoutedEventArgs e)
    {
        ShowKeypad("Inserisci codice pairing", _pairingCode, value =>
        {
            _pairingCode = value;
            BtnCodeInput.Content = string.IsNullOrEmpty(value) ? "Codice: tocca" : $"Codice: {value}";
            BtnCodeInput.Foreground = string.IsNullOrEmpty(value)
                ? new SolidColorBrush(Color.Parse("#757575"))
                : new SolidColorBrush(Color.Parse("#E0E0E0"));

            // Il testo del bottone cambia in base alla presenza del codice
            BtnConnect.Content = string.IsNullOrEmpty(value) ? "Connetti" : "Associa";
        });
    }

    // Connette (e opzionalmente associa) il dispositivo via wireless
    private async void OnConnectClick(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_ip) || string.IsNullOrWhiteSpace(_port))
        {
            LabelWirelessResult.Text = "Compila IP e Porta";
            LabelWirelessResult.Foreground = new SolidColorBrush(Color.Parse("#EF5350"));
            return;
        }

        BtnConnect.IsEnabled = false;
        bool hasPairingCode = !string.IsNullOrWhiteSpace(_pairingCode);

        // Se il codice e' valorizzato, prima esegue il pairing
        if (hasPairingCode)
        {
            LabelWirelessResult.Text = "Associazione in corso...";
            LabelWirelessResult.Foreground = new SolidColorBrush(Color.Parse("#9E9E9E"));

            string pairResult = await _adbService.PairWirelessAsync(_ip, _port, _pairingCode);

            if (pairResult.StartsWith("Errore"))
            {
                LabelWirelessResult.Text = pairResult;
                LabelWirelessResult.Foreground = new SolidColorBrush(Color.Parse("#EF5350"));
                BtnConnect.IsEnabled = true;
                return;
            }
        }

        // Connessione
        LabelWirelessResult.Text = "Connessione in corso...";
        LabelWirelessResult.Foreground = new SolidColorBrush(Color.Parse("#9E9E9E"));

        string connectResult = await _adbService.ConnectWirelessAsync(_ip, _port);

        LabelWirelessResult.Text = connectResult;
        LabelWirelessResult.Foreground = connectResult.StartsWith("Errore")
            ? new SolidColorBrush(Color.Parse("#EF5350"))
            : new SolidColorBrush(Color.Parse("#66BB6A"));
        BtnConnect.IsEnabled = true;

        await ScanDevicesAsync();
    }

    #endregion
}
