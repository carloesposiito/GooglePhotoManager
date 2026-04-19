using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using System;
using System.Collections.Generic;

namespace GooglePhotoManager.Controls;

public partial class TextKeypad : UserControl
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

    public TextKeypad()
    {
        InitializeComponent();
    }

    // Cerca ricorsivamente tutti i Button all'interno di un controllo
    private static IEnumerable<Control> GetLetterButtons(Control parent)
    {
        foreach (var child in parent.GetVisualChildren())
        {
            if (child is Button btn)
                yield return btn;
            if (child is Control ctrl)
            {
                foreach (var sub in GetLetterButtons(ctrl))
                    yield return sub;
            }
        }
    }

    #endregion

    #region Eventi

    // Evento emesso quando l'utente conferma il valore
    public event EventHandler<string>? Confirmed;

    // Evento emesso quando l'utente annulla l'inserimento
    public event EventHandler? Cancelled;

    // Pressione di un tasto lettera
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

    // Inserisce uno spazio
    private void OnSpaceClick(object? sender, RoutedEventArgs e)
    {
        Display.Text = (Display.Text ?? "") + " ";
    }

    // Alterna maiuscole/minuscole su tutti i tasti lettera
    private void OnToggleCase(object? sender, RoutedEventArgs e)
    {
        foreach (var child in GetLetterButtons(this))
        {
            if (child is Button btn && btn.Tag is string tag && tag.Length == 1 && char.IsLetter(tag[0]))
            {
                bool isUpper = char.IsUpper(tag[0]);
                string newTag = isUpper ? tag.ToLower() : tag.ToUpper();
                btn.Tag = newTag;
                btn.Content = newTag;
            }
        }
    }

    // Placeholder per il toggle numeri (non implementato per semplicita')
    private void OnToggleNumbers(object? sender, RoutedEventArgs e)
    {
        Display.Text = (Display.Text ?? "") + "";
    }

    // Conferma il valore inserito
    private void OnOkClick(object? sender, RoutedEventArgs e)
    {
        Confirmed?.Invoke(this, Display.Text ?? "");
    }

    // Annulla e chiude la tastiera
    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Cancelled?.Invoke(this, EventArgs.Empty);
    }

    #endregion
}
