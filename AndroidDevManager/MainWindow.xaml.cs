using AdvancedSharpAdbClient.Models;
using AndroidDevManager.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace AndroidDevManager
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        #region "Private fields"
        
        private bool _isInitialized = false;
        private ConnectionManager _connectionManager;
        private List<DeviceData> _devices = new List<DeviceData>();
        private DeviceData _originDevice = null;
        private DeviceData _destinationDevice = null;
        private bool _areDeviceConnected = false;
        private bool _ready = false;
        private Visibility _progressBarVisibility = Visibility.Collapsed;
        private Cursor _windowCursor = Cursors.Arrow;
        private string _progressText = string.Empty;
        private bool _wirelessConnection = false;
        private bool _pairingNeeded = false;

        #endregion

        #region "Properties"      

        public new bool Initialized
        {
            get
            {
                return _isInitialized;
            }
            set
            {
                _isInitialized = value;
                OnPropertyChanged(nameof(Initialized));
            }
        }

        internal ConnectionManager ConnectionManager 
        { 
            get => _connectionManager; 
            set => _connectionManager = value; 
        }
        
        public List<DeviceData> Devices
        {
            get => _devices;
            set
            {
                _devices = value;
                OnPropertyChanged(nameof(Devices));

                // Refresh are devices connected value
                AreDeviceConnected = value.Count > 0;
            }
        }

        public DeviceData OriginDevice
        {
            get => _originDevice;
            set
            {
                _originDevice = value;
                OnPropertyChanged(nameof(OriginDevice));
            }
        }

        public DeviceData DestinationDevice
        {
            get => _destinationDevice;
            set
            {
                _destinationDevice = value;
                OnPropertyChanged(nameof(DestinationDevice));
            }
        }

        public bool AreDeviceConnected
        {
            get
            {
                return _areDeviceConnected;
            }
            set
            {
                _areDeviceConnected = value;
                OnPropertyChanged(nameof(AreDeviceConnected));
            }
        }

        public bool Ready
        {
            get
            {
                return _ready;
            }
            set
            {
                _ready = value;
                OnPropertyChanged(nameof(Ready));
            }
        }

        public Visibility ProgressBarVisibility
        {
            get
            {
                return _progressBarVisibility;
            }
            set
            {
                _progressBarVisibility = value;
                OnPropertyChanged(nameof(ProgressBarVisibility));
            }
        }

        public Cursor WindowCursor
        {
            get
            {
                return _windowCursor;
            }
            set
            {
                _windowCursor = value;
                OnPropertyChanged(nameof(WindowCursor));
            }
        }

        public string ProgressText
        {
            get
            {
                return _progressText;
            }
            set
            {
                _progressText = value;
                OnPropertyChanged(nameof(ProgressText));
            }
        }

        public bool WirelessConnection
        {
            get
            {
                return _wirelessConnection;
            }
            set
            {
                _wirelessConnection = value;
                OnPropertyChanged(nameof(WirelessConnection));
            }
        }

        public bool PairingNeeded
        {
            get
            {
                return _pairingNeeded;
            }
            set
            {
                _pairingNeeded = value;
                OnPropertyChanged(nameof(PairingNeeded));
            }
        }

        #endregion

        #region "Constructor"

        public MainWindow()
        {
            InitializeComponent();

            try
            {
                ConnectionManager = new ConnectionManager($"{Directory.GetCurrentDirectory()}\\Platform_Tools");

                // Check program dipendencies
                if (ConnectionManager.CheckDependencies())
                {
                    // If ok add event on window loading
                    this.Loaded += Window_Loaded;
                }

                DataContext = this;
            }
            catch (Exception exception)
            {
                MessageBox.Show("Error initializing program:\n" +
                    $"{exception.Message}");                
            }            
        }

        #endregion

        #region "Methods"

        private async Task ScanDevices()
        {
            try
            {
                ProgressText = "Scanning devices...";
                WindowCursor = Cursors.Wait;
                ProgressBarVisibility = Visibility.Visible;
                Ready = false;
                OriginDevice = DestinationDevice = null;
                Devices = await ConnectionManager.ScanDevicesAsync();
            }
            finally
            {
                ProgressBarVisibility = Visibility.Collapsed;
                WindowCursor = Cursors.Arrow;
                ProgressText = "Ready";
            }
        }

        #endregion

        #region "Events"

        /// <summary>
        /// Happens on window loaded:<br/>
        /// 1 - Starts ADB server<br/>
        /// 2 - Checks connected devices.
        /// </summary>
        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Remove event when done
            this.Loaded -= Window_Loaded;

            ProgressText = "Initializing service...";

            // Initialize server
            if (await ConnectionManager.InitializeServiceAsync())
            {
                Initialized = true;
                await ScanDevices();
            }
            else
            {
                ProgressText = "Not ready";
            }
        }

        private async void Btn_ScanDevices_Click(object sender, RoutedEventArgs e)
        {
            await ScanDevices();
        }

        private void Cb_Devices_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Ready = OriginDevice != null && DestinationDevice != null && !OriginDevice.Equals(DestinationDevice);
        }

        private async void Btn_TransferPhotos_Click(object sender, RoutedEventArgs e)
        {
            // Save ready status in order to restore it later (useless I know)
            bool previousReadyState = Ready;

            try
            {
                Ready = false;
                WindowCursor = Cursors.Wait;
                ProgressBarVisibility = Visibility.Visible;
                ProgressText = $"Transferring photos from {OriginDevice.Model} to {DestinationDevice.Model} in progress...";

                // Check if delete photos from source is checked in order to pass it to function
                bool deletePhotosFromOriginDevice = (bool)Cb_DeleteFromOrigin.IsChecked;

                // Get origin device
                var tR = await ConnectionManager.TransferPhotos(OriginDevice, DestinationDevice, deletePhotosFromOriginDevice);
                if (tR != null)
                {
                    if (tR.AllFilesSynced)
                    {
                        ProgressText = $"Operation completed ({tR.PushedCount})";

                        if (deletePhotosFromOriginDevice)
                        {
                            if (tR.DeleteCompleted)
                            {
                                ProgressText += "and photos deleted from origin";

                                if (Directory.Exists(tR.FolderPath))
                                {
                                    // Ask to delete photos from local PC only if all transferred successfully
                                    bool deleteLocalFolder = MessageBox.Show(
                                        $"Do you want to delete transferred photos from this computer?",
                                        "Attention",
                                        MessageBoxButton.YesNo,
                                        MessageBoxImage.Question
                                    ).Equals(MessageBoxResult.Yes);

                                    if (deleteLocalFolder)
                                    {
                                        Directory.Delete(tR.FolderPath, true);
                                    }
                                    else
                                    {
                                        // Open in explorer
                                        Process.Start(new ProcessStartInfo
                                        {
                                            FileName = "explorer.exe",
                                            Arguments = $"\"{tR.FolderPath}\"",
                                            UseShellExecute = true
                                        });
                                    }
                                }                                
                            }
                            else
                            {
                                MessageBox.Show($"Check photos on {OriginDevice.Model}: some files not deleted after transferring completed.");
                            }
                        }
                    }
                    else
                    {
                        string message = $"Extracted {tR.PulledCount}/{tR.ToBePulledCount} from {OriginDevice.Model}\n" +
                            $"Transferred {tR.PushedCount}/{tR.ToBePushedCount} to {DestinationDevice.Model}\n";

                        if (deletePhotosFromOriginDevice)
                        {
                            message += $"Photos not deleted from origin device for security reasons.";
                        }

                        // Show messages
                        MessageBox.Show(message);
                    }
                }
            }
            catch (Exception exception)
            {
                ProgressText = string.Empty;
                MessageBox.Show($"Error transferring photos from {OriginDevice.Model} to {DestinationDevice.Model}:\n" +
                    $"{exception.Message}");
            }
            finally
            {
                Ready = previousReadyState;
                WindowCursor = Cursors.Arrow;
                ProgressBarVisibility = Visibility.Collapsed;
            }            
        }

        private async void Btn_AuthorizeDevices_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Kill server and restart in order to show auth popup on connected devices
                MessageBoxResult result = MessageBox.Show(
                        $"ADB server will be restarted: an authorization popup should be appear on your device then please give authorization.\n" +
                        $"Click YES button to continue.",
                        "Attention",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Information
                    );

                if (result.Equals(MessageBoxResult.Yes))
                {
                    ProgressText = "Authorization in progress...";

                    if (!await ConnectionManager.RestartService())
                    {
                        MessageBox.Show("Error restarting ADB server: check server status.");
                    }
                }
            }
            catch (Exception exception)
            {
                MessageBox.Show($"Error restarting ADB server:\n" +
                    $"{exception.Message}");
            }
            finally
            {
                ProgressText = string.Empty;
            }
        }

        private void Btn_Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            ConnectionManager.KillService();
        }

        private async void Btn_ConnectWirelessDevice_Click(object sender, RoutedEventArgs e)
        {
            string operationResult = string.Empty;

            try
            {
                ProgressBarVisibility = Visibility.Visible;

                string deviceIp = Tb_DeviceIp.Text;
                string devicePort = Tb_DevicePort.Text;
                string devicePairingCode = Tb_DevicePairingCode.Text;

                // Check data
                if (string.IsNullOrWhiteSpace(deviceIp))
                {
                    MessageBox.Show("Device IP address not valid!");
                    return;
                }

                if (string.IsNullOrWhiteSpace(devicePort))
                {
                    MessageBox.Show("Device port not valid!");
                    return;
                }

                if (PairingNeeded)
                {
                    if (string.IsNullOrWhiteSpace(devicePairingCode))
                    {
                        MessageBox.Show("Paring code cannot be empty!");
                        return;
                    }
                    else
                    {
                        operationResult = await ConnectionManager.PairWirelessAsync(deviceIp, devicePort, devicePairingCode);
                    }
                }
                else
                {
                    operationResult = await ConnectionManager.ConnectWirelessAsync(deviceIp, devicePort);
                }
            }
            catch (Exception exception)
            {
                operationResult = "Error while connecting device via Wireless ADB:\n" +
                    $"{exception.Message}";
            }
            finally
            {
                WirelessConnection = false;
                PairingNeeded = false;
                Tb_DeviceIp.Text = Tb_DevicePort.Text = Tb_DevicePairingCode.Text = string.Empty;

                ProgressBarVisibility = Visibility.Collapsed;
                MessageBox.Show(operationResult);
            }
        }

        private void Lbl_TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        #endregion

        #region "Binding"

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

    }
}
