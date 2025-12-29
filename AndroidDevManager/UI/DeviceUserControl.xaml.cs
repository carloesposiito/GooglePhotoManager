using AdvancedSharpAdbClient.Models;
using GooglePhotoTransferTool.Helpers;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace GooglePhotoTransferTool.UI
{
    public partial class DeviceUserControl : UserControl, INotifyPropertyChanged
    {
        #region "Private fields"

        private DeviceData _deviceData;
        private string _deviceName;

        #endregion

        #region "Properties"

        public BitmapImage DeviceImage 
        {
            get
            {
                return Utilities.BitmapToBitmapImage(Properties.Resources.Android);
            }
        }
        
        public string DeviceName
        {
            get
            {
                return _deviceName;
            }
            set
            {
                _deviceName = value.ToUpper();
                OnPropertyChanged(nameof(DeviceName));
            }
        }

        #region "Device dependency property"

        public static readonly DependencyProperty DeviceProperty = DependencyProperty.Register(
            nameof(Device),
            typeof(DeviceData), 
            typeof(DeviceUserControl),
            new PropertyMetadata(null, OnDeviceDataChanged));

        public DeviceData Device
        {
            get => (DeviceData)GetValue(DeviceProperty);
            set => SetValue(DeviceProperty, value);
        }

        private static void OnDeviceDataChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            DeviceUserControl userControl = (DeviceUserControl)dependencyObject;
            DeviceData deviceData = (DeviceData)e.NewValue;

            // Refresh name according to passed device
            if (userControl != null && deviceData != null)
            {
                userControl.DeviceName = string.IsNullOrWhiteSpace(deviceData.Model) ? deviceData.Serial : deviceData.Model;
            }
        }

        #endregion

        #endregion

        #region "Constructor"

        public DeviceUserControl()
        {
            InitializeComponent();
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
