using Avalonia.Controls;
using Avalonia.Interactivity;
using System;

namespace GooglePhotoManager.Controls;

public partial class NumericKeypad : UserControl
{
    #region Campi privati

    // Nessun campo privato aggiuntivo

    #endregion

    #region Proprietà

    // Testo corrente visualizzato nel display
    public string Text
    {
        get => Display.Text ?? "";
        set => Display.Text = value;
    }

    #endregion

    #region Metodi

    public NumericKeypad()
    {
        InitializeComponent();
    }

    #endregion

    #region Eventi

    // Evento emesso quando l'utente conferma il valore
    public event EventHandler<string>? Confirmed;

    // Evento emesso quando l'utente annulla l'inserimento
    public event EventHandler? Cancelled;

    // Pressione di un tasto numerico o punto
    private void OnKeyClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string key)
            Display.Text = (Display.Text ?? "") + key;
    }

    // Cancella l'ultimo carattere
    private void OnBackspaceClick(object? sender, RoutedEventArgs e)
    {
        string current = Display.Text ?? "";
        if (current.Length > 0)
            Display.Text = current.Substring(0, current.Length - 1);
    }

    // Conferma il valore inserito
    private void OnOkClick(object? sender, RoutedEventArgs e)
    {
        Confirmed?.Invoke(this, Display.Text ?? "");
    }

    // Annulla e chiude il tastierino
    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Cancelled?.Invoke(this, EventArgs.Empty);
    }

    #endregion
}
