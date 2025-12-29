namespace GooglePhotoTransferTool.Model
{
    internal class TransferResult
    {
        private string _folderPath = string.Empty;
        private bool _allFilesSynced = false;
        private int toBePulledCount = 0;
        private int pulledCount = 0;
        private int toBePushedCount = 0;
        private int pushedCount = 0;
        private bool _deleteCompleted = false;

        public string FolderPath { get => _folderPath; set => _folderPath = value; }
        internal bool AllFilesSynced { get => _allFilesSynced; set => _allFilesSynced = value; }
        internal int ToBePulledCount { get => toBePulledCount; set => toBePulledCount = value; }
        internal int PulledCount { get => pulledCount; set => pulledCount = value; }
        internal int ToBePushedCount { get => toBePushedCount; set => toBePushedCount = value; }
        internal int PushedCount { get => pushedCount; set => pushedCount = value; }
        internal bool DeleteCompleted { get => _deleteCompleted; set => _deleteCompleted = value; }
    }
}
