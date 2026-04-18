using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using System;
using System.Threading.Tasks;

namespace GooglePhotoManager.Views;

public partial class DevicesView : UserControl
{
    private AdbService _adbService = null!;
    private string _ip = "";
    private string _port = "5555";
    private string _pairingCode = "";
    private Action<string>? _keypadCallback;

    public DevicesView()
    {
        InitializeComponent();
    }

    public void Initialize(AdbService adbService)
    {
        _adbService = adbService;

        Keypad.Confirmed += OnKeypadConfirmed;
        Keypad.Cancelled += OnKeypadCancelled;
    }

    // --- ADB Init & Scan ---

    private async void OnInitClick(object? sender, RoutedEventArgs e)
    {
        BtnInit.IsEnabled = false;
        LabelStatus.Text = "Inizializzazione ADB...";
        LabelStatus.Foreground = new SolidColorBrush(Color.Parse("#9E9E9E"));

        bool result = await _adbService.InitializeAsync();

        if (result)
        {
            LabelStatus.Text = "ADB pronto";
            LabelStatus.Foreground = new SolidColorBrush(Color.Parse("#66BB6A"));
            EnableWirelessControls(true);
            await ScanDevicesAsync();
        }
        else
        {
            LabelStatus.Text = "ADB non trovato! Installa con: sudo apt install adb";
            LabelStatus.Foreground = new SolidColorBrush(Color.Parse("#EF5350"));
            BtnInit.IsEnabled = true;
        }
    }

    private async void OnScanClick(object? sender, RoutedEventArgs e)
    {
        await ScanDevicesAsync();
    }

    private async Task ScanDevicesAsync()
    {
        BtnScan.IsEnabled = false;
        LabelStatus.Text = "Scansione in corso...";
        DeviceList.Items.Clear();

        await _adbService.ScanDevicesAsync();
        var devices = _adbService.Devices;

        if (devices.Count > 0)
        {
            foreach (var device in devices)
            {
                string info = $"{device.Model} ({device.Product}) - {device.Serial}";
                DeviceList.Items.Add(info);
            }

            LabelStatus.Text = "ADB pronto";
            LabelStatus.Foreground = new SolidColorBrush(Color.Parse("#66BB6A"));
            LabelCount.Text = $"Trovati {devices.Count} dispositivo/i";
        }
        else
        {
            LabelStatus.Text = "Nessun dispositivo trovato";
            LabelStatus.Foreground = new SolidColorBrush(Color.Parse("#FFB74D"));
            LabelCount.Text = "Collega un dispositivo Android con USB debugging attivo";
        }

        BtnScan.IsEnabled = true;
    }

    // --- Numeric Keypad ---

    private void ShowKeypad(string title, string currentValue, Action<string> callback)
    {
        KeypadTitle.Text = title;
        Keypad.Text = currentValue;
        _keypadCallback = callback;
        KeypadOverlay.IsVisible = true;
        MainContent.IsEnabled = false;
    }

    private void HideKeypad()
    {
        KeypadOverlay.IsVisible = false;
        MainContent.IsEnabled = true;
        _keypadCallback = null;
    }

    private void OnKeypadConfirmed(object? sender, string value)
    {
        _keypadCallback?.Invoke(value);
        HideKeypad();
    }

    private void OnKeypadCancelled(object? sender, EventArgs e)
    {
        HideKeypad();
    }

    private void OnIpInputClick(object? sender, RoutedEventArgs e)
    {
        ShowKeypad("Inserisci indirizzo IP", _ip, value =>
        {
            _ip = value;
            BtnIpInput.Content = string.IsNullOrEmpty(value) ? "Tocca per inserire IP" : value;
            BtnIpInput.Foreground = string.IsNullOrEmpty(value) ? new SolidColorBrush(Color.Parse("#9E9E9E")) : new SolidColorBrush(Color.Parse("#E0E0E0"));
        });
    }

    private void OnPortInputClick(object? sender, RoutedEventArgs e)
    {
        ShowKeypad("Inserisci porta", _port, value =>
        {
            _port = value;
            BtnPortInput.Content = string.IsNullOrEmpty(value) ? "5555" : value;
        });
    }

    private void OnCodeInputClick(object? sender, RoutedEventArgs e)
    {
        ShowKeypad("Inserisci codice pairing", _pairingCode, value =>
        {
            _pairingCode = value;
            BtnCodeInput.Content = string.IsNullOrEmpty(value) ? "Tocca per inserire codice" : value;
            BtnCodeInput.Foreground = string.IsNullOrEmpty(value) ? new SolidColorBrush(Color.Parse("#9E9E9E")) : new SolidColorBrush(Color.Parse("#E0E0E0"));
        });
    }

    // --- Wireless Connect & Pair ---

    private async void OnPairClick(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_ip) || string.IsNullOrWhiteSpace(_port) || string.IsNullOrWhiteSpace(_pairingCode))
        {
            LabelWirelessResult.Text = "Compila IP, Porta e Codice pairing";
            LabelWirelessResult.Foreground = new SolidColorBrush(Color.Parse("#EF5350"));
            return;
        }

        BtnPair.IsEnabled = false;
        LabelWirelessResult.Text = "Associazione in corso...";
        LabelWirelessResult.Foreground = new SolidColorBrush(Color.Parse("#9E9E9E"));

        string result = await _adbService.PairWirelessAsync(_ip, _port, _pairingCode);

        LabelWirelessResult.Text = result;
        LabelWirelessResult.Foreground = result.StartsWith("Errore") ? new SolidColorBrush(Color.Parse("#EF5350")) : new SolidColorBrush(Color.Parse("#66BB6A"));
        BtnPair.IsEnabled = true;
    }

    private async void OnConnectClick(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_ip) || string.IsNullOrWhiteSpace(_port))
        {
            LabelWirelessResult.Text = "Compila IP e Porta";
            LabelWirelessResult.Foreground = new SolidColorBrush(Color.Parse("#EF5350"));
            return;
        }

        BtnConnect.IsEnabled = false;
        LabelWirelessResult.Text = "Connessione in corso...";
        LabelWirelessResult.Foreground = new SolidColorBrush(Color.Parse("#9E9E9E"));

        string result = await _adbService.ConnectWirelessAsync(_ip, _port);

        LabelWirelessResult.Text = result;
        LabelWirelessResult.Foreground = result.StartsWith("Errore") ? new SolidColorBrush(Color.Parse("#EF5350")) : new SolidColorBrush(Color.Parse("#66BB6A"));
        BtnConnect.IsEnabled = true;

        await ScanDevicesAsync();
    }

    private void EnableWirelessControls(bool enabled)
    {
        BtnScan.IsEnabled = enabled;
        BtnPair.IsEnabled = enabled;
        BtnConnect.IsEnabled = enabled;
        BtnIpInput.IsEnabled = enabled;
        BtnPortInput.IsEnabled = enabled;
        BtnCodeInput.IsEnabled = enabled;
    }
}
