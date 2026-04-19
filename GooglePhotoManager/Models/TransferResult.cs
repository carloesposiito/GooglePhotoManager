namespace GooglePhotoManager.Models;

public class TransferResult
{
    #region Proprietà

    // Numero di file da estrarre dal dispositivo sorgente
    public int ToBePulledCount { get; set; }

    // Numero di file effettivamente estratti
    public int PulledCount { get; set; }

    // Numero di file da trasferire al dispositivo di backup
    public int ToBePushedCount { get; set; }

    // Numero di file effettivamente trasferiti
    public int PushedCount { get; set; }

    // Indica se tutti i file sono stati sincronizzati correttamente
    public bool AllFilesSynced { get; set; }

    // Indica se la cancellazione dalla sorgente e' stata completata
    public bool DeleteCompleted { get; set; }

    // Percorso della cartella temporanea locale usata per il trasferimento
    public string FolderPath { get; set; } = string.Empty;

    #endregion
}
