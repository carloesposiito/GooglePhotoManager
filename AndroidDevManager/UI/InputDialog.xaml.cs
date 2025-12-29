using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace GooglePhotoTransferTool.UI
{
    public partial class InputDialog : Window, INotifyPropertyChanged
    {
        #region "Private fields"

        private string dialogTitle;
        private string dialogMessage;
        private string dialogValue;

        #endregion

        #region "Properties"

        public string DialogTitle
        {
            get => dialogTitle;
            set
            {
                dialogTitle = value.ToUpper();
                OnPropertyChanged(nameof(DialogTitle));
            }
        }
        
        public string DialogMessage
        {
            get => dialogMessage;
            set
            {
                dialogMessage = value;
                OnPropertyChanged(nameof(DialogMessage));
            }
        }
        
        public string DialogValue 
        { 
            get => dialogValue;
            set
            {
                dialogValue = value;
                OnPropertyChanged(nameof(DialogValue));
            }
        }

        #endregion

        #region "Constructor"

        public InputDialog()
        {
            InitializeComponent();
            DataContext = this;
        }

        #endregion

        #region "Events"

        private void Btn_Ok_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();    
        }

        private void Btn_Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
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
