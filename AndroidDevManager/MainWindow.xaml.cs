using AdvancedSharpAdbClient.Models;
using GooglePhotoTransferTool.Model;
using GooglePhotoTransferTool.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace GooglePhotoTransferTool
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        #region "Private fields"

        private ConnectionManager _connectionManager;
        private bool _isInitialized = false;
        private bool _deviceConnected = false;
        private bool _readyForTransfer = false;
        private List<DeviceData> _devices = new List<DeviceData>();
        private DeviceData _originDevice = null;
        private DeviceData _destinationDevice = null;
        private Visibility _progressBarVisibility = Visibility.Collapsed;

        #endregion

        #region "Properties"      

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
#if DEBUG
                DeviceConnected = value.Count > 0;
#else
                DeviceConnected = value.Count >= 2;
#endif
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

        /// <summary>
        /// Describes if ADB server is ready.
        /// </summary>
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

        /// <summary>
        /// Describes if at least two devices are connected.
        /// </summary>
        public bool DeviceConnected
        {
            get
            {
                return _deviceConnected;
            }
            set
            {
                _deviceConnected = value;
                OnPropertyChanged(nameof(DeviceConnected));
            }
        }

        /// <summary>
        /// Describes if it's possible to start transferring.<br/>
        /// So two different devices are choosen in related comboboxes.
        /// </summary>
        public bool ReadyForTransfer
        {
            get
            {
                return _readyForTransfer;
            }
            set
            {
                _readyForTransfer = value;
                OnPropertyChanged(nameof(ReadyForTransfer));
            }
        }

        /// <summary>
        /// Holds progress bar visibility that describes if an operation is in progress.
        /// </summary>
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

#if DEBUG
                MenuItem_Advanced.Visibility = Visibility.Visible;
#endif

                DataContext = this;
            }
            catch (Exception exception)
            {
                MessageBox.Show("Errore nell'inizializzazione del programma:\n" +
                    $"{exception.Message}");                
            }            
        }

        #endregion

        #region "Methods"

        private async Task ScanDevices()
        {
            try
            {
                ReadyForTransfer = false;
                OriginDevice = DestinationDevice = null;
                ProgressBarVisibility = Visibility.Visible;
                Devices = await ConnectionManager.ScanDevicesAsync();
            }
            finally
            {
                ProgressBarVisibility = Visibility.Collapsed;
            }
        }

        private void RefreshWindowHeight()
        {
            this.Height = (OriginDevice != null || DestinationDevice != null) ? 340 : 210;
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
            // Remove event
            this.Loaded -= Window_Loaded;

            // Initialize server
            if (await ConnectionManager.InitializeServiceAsync())
            {
                Initialized = true;
                await ScanDevices();
            }
        }

        private async void Btn_ScanDevices_Click(object sender, RoutedEventArgs e)
        {
            await ScanDevices();
        }

        private async void Btn_RestartServer_Click(object sender, RoutedEventArgs e)
        {
            await ConnectionManager.RestartService();
        }

        private void Cb_Devices_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Origin and destination device must be not null, different and authorized
            ReadyForTransfer = OriginDevice != null && DestinationDevice != null && !OriginDevice.Equals(DestinationDevice) && !OriginDevice.State.Equals(DeviceState.Unauthorized) && !DestinationDevice.State.Equals(DeviceState.Unauthorized);
            RefreshWindowHeight();
        }

        private async void Btn_TransferPhotos_Click(object sender, RoutedEventArgs e)
        {
            // Save ready status in order to restore it later (useless I know)
            bool previousReadyState = ReadyForTransfer;

            try
            {
                ReadyForTransfer = false;
                ProgressBarVisibility = Visibility.Visible;

                // Check if delete photos from source is checked in order to pass it to function
                bool deletePhotosFromOriginDevice = (bool)Cb_DeleteFromOrigin.IsChecked;

                // Get origin device
                var tR = await ConnectionManager.TransferPhotos(OriginDevice, DestinationDevice, deletePhotosFromOriginDevice);
                if (tR != null)
                {
                    if (tR.AllFilesSynced)
                    {
                        // Show message
                        string message = $"Operazione completata: trasferite {tR.PushedCount} foto";
                        message += deletePhotosFromOriginDevice ? (tR.DeleteCompleted ? " e poi cancellate dal dispositivo di origine." : " ma alcune potrebbero non essere state cancellate dal dispositivo di origine.") : ".";
                        MessageBox.Show(message);

                        if (Directory.Exists(tR.FolderPath))
                        {
                            // Ask to delete photos from local PC only if all transferred successfully
                            bool deleteLocalFolder = MessageBox.Show(
                                $"Vuoi cancellare da questo computer le foto appena trasferite?",
                                "Attenzione",
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
                        string message = $"Estratte {tR.PulledCount}/{tR.ToBePulledCount} foto da {OriginDevice.Model}.\n" +
                            $"Trasferite {tR.PushedCount}/{tR.ToBePushedCount} foto a {DestinationDevice.Model}.\n";

                        if (deletePhotosFromOriginDevice)
                        {
                            message += $"Le foto non sono state cancellate dal dispositivo di origine per motivi di sicurezza.";
                        }

                        // Show messages
                        MessageBox.Show(message);
                    }
                }
            }
            catch (Exception exception)
            {
                MessageBox.Show($"Errore durante il trasferimento delle foto da {OriginDevice.Model} a {DestinationDevice.Model}:\n" +
                    $"{exception.Message}");
            }
            finally
            {
                ReadyForTransfer = previousReadyState;
                ProgressBarVisibility = Visibility.Collapsed;
            }            
        }

        private async void Btn_AuthorizeDevices_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Kill server and restart in order to show auth popup on connected devices
                MessageBoxResult result = MessageBox.Show(
                        $"Il server ADB verrà riavviato ed un popup di autorizzazione per fornire i permessi necessari dovrebbe apparire sul tuo dispositivo.",
                        "Attention",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );

                if (result.Equals(MessageBoxResult.Yes))
                {
                    if (!await ConnectionManager.RestartService())
                    {
                        MessageBox.Show("Errore nel riavvio del server ADB!");
                    }
                }
            }
            catch (Exception exception)
            {
                MessageBox.Show($"Errore nel riavvio del server ADB:\n" +
                    $"{exception.Message}");
            }
        }

        private async void Btn_Close_Click(object sender, RoutedEventArgs e)
        {
            await ConnectionManager.KillService();
            Close();
        }

        private async void Btn_ConnectWirelessDevice_Click(object sender, RoutedEventArgs e)
        {
            string operationResult = string.Empty;

            try
            {
                InputDialog iD;
                bool pairingNeeded = false;
                string deviceIpAddress = string.Empty;
                string devicePort = string.Empty;
                string devicePairingCode = string.Empty;

                this.Hide();

                // Ask for pairing
                MessageBoxResult pairingQuestion = MessageBox.Show(
                        $"Se il tuo dispositivo non è mai stato connesso a questo PC tramite ADB Wireless, occorre innanzitutto che venga associato.\n" +
                        $"E' la prima volta che lo smartphone viene connesso al PC?",
                        "Attenzione",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Information);

                pairingNeeded = pairingQuestion.Equals(MessageBoxResult.Yes);

                // Show info about where to find device IP and port for Wireless ADB connection
                if (pairingNeeded)
                {
                    MessageBox.Show($"Recati nella sezione \"ADB Wireless\" delle \"Impostazioni sviluppatore\" dello smartphone e premi la voce \"Abbina dispositivo tramite codice di accoppiamento\".\n" +
                        $"Si aprirà una finestra con i dati necessari alla connessione da inserire nelle schermate che seguono.",
                        "Attenzione",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show($"Recati nella sezione \"ADB Wireless\" delle \"Impostazioni sviluppatore\" dello smartphone.\n" +
                        $"Si aprirà una finestra con i dati necessari alla connessione da inserire nelle schermate che seguono.",
                        "Attenzione",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }

                // Ask for device IP address
                iD = new InputDialog
                {
                    DialogTitle = "ADB Wireless",
                    DialogMessage = "Inserissci l'indirizzo IP del dispositivo:"
                };

                if ((bool)iD.ShowDialog() && !string.IsNullOrWhiteSpace(iD.DialogValue))
                {
                    deviceIpAddress = iD.DialogValue;
                }
                else
                {
                    return;
                }

                // Ask for device port   
                iD = new InputDialog
                {
                    DialogTitle = "ADB Wireless",
                    DialogMessage = "Inserisci la porta del dispositivo:"
                };

                if ((bool)iD.ShowDialog() && !string.IsNullOrWhiteSpace(iD.DialogValue))
                {
                    devicePort = iD.DialogValue;
                }
                else
                {
                    return;
                }

                // Check if pairing is needed
                if (pairingNeeded)
                {
                    // Ask for device pairing code
                    iD = new InputDialog
                    {
                        DialogTitle = "ADB Wireless",
                        DialogMessage = "Inserisci il codice di associazione:"
                    };

                    if ((bool)iD.ShowDialog() && !string.IsNullOrWhiteSpace(iD.DialogValue))
                    {
                        devicePairingCode = iD.DialogValue;
                    }
                    else
                    {
                        return;
                    }

                    // Pair
                    operationResult = await ConnectionManager.PairWirelessAsync(deviceIpAddress, devicePort, devicePairingCode);
                }
                else
                {
                    // Connect
                    operationResult = await ConnectionManager.ConnectWirelessAsync(deviceIpAddress, devicePort);
                }
            }
            catch (Exception exception)
            {
                operationResult = "Errore durante la connessione ADB:\n" +
                    $"{exception.Message}";
            }
            finally
            {
                if (!string.IsNullOrWhiteSpace(operationResult))
                {
                    MessageBox.Show(operationResult, "Attenzione", MessageBoxButton.OK, MessageBoxImage.Information);
                    await ScanDevices();
                }

                this.Show();
            }
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
