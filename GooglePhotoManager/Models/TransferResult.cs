namespace GooglePhotoManager.Models;

public class TransferResult
{
    public int ToBePulledCount { get; set; }
    public int PulledCount { get; set; }
    public int ToBePushedCount { get; set; }
    public int PushedCount { get; set; }
    public bool AllFilesSynced { get; set; }
    public bool DeleteCompleted { get; set; }
    public string FolderPath { get; set; } = string.Empty;
}
