using Avalonia.Controls;
using Avalonia.Interactivity;
using System;

namespace GooglePhotoManager.Controls;

public partial class NumericKeypad : UserControl
{
    public string Text
    {
        get => Display.Text ?? "";
        set => Display.Text = value;
    }

    public event EventHandler<string>? Confirmed;
    public event EventHandler? Cancelled;

    public NumericKeypad()
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
        {
            Display.Text = current.Substring(0, current.Length - 1);
        }
    }

    private void OnOkClick(object? sender, RoutedEventArgs e)
    {
        Confirmed?.Invoke(this, Display.Text ?? "");
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Cancelled?.Invoke(this, EventArgs.Empty);
    }
}
