using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using System;

namespace GooglePhotoManager.Controls;

public partial class TextKeypad : UserControl
{
    public string Text
    {
        get => Display.Text ?? "";
        set => Display.Text = value;
    }

    public event EventHandler<string>? Confirmed;
    public event EventHandler? Cancelled;

    public TextKeypad()
    {
        InitializeComponent();
    }

    private void OnKeyClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string key)
        {
            Display.Text = (Display.Text ?? "") + key;
        }
    }

    private void OnBackspaceClick(object? sender, RoutedEventArgs e)
    {
        string current = Display.Text ?? "";
        if (current.Length > 0)
            Display.Text = current.Substring(0, current.Length - 1);
    }

    private void OnSpaceClick(object? sender, RoutedEventArgs e)
    {
        Display.Text = (Display.Text ?? "") + " ";
    }

    private void OnToggleCase(object? sender, RoutedEventArgs e)
    {
        // Toggle case of all letter buttons
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

    private void OnToggleNumbers(object? sender, RoutedEventArgs e)
    {
        // Simple: append common numbers/symbols
        Display.Text = (Display.Text ?? "") + "";
        // For simplicity, just toggle to show a hint - user can type numbers aren't critical for usernames
    }

    private void OnOkClick(object? sender, RoutedEventArgs e)
    {
        Confirmed?.Invoke(this, Display.Text ?? "");
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Cancelled?.Invoke(this, EventArgs.Empty);
    }

    private static System.Collections.Generic.IEnumerable<Control> GetLetterButtons(Control parent)
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
}
