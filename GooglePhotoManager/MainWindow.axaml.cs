using Avalonia.Controls;
using System;

namespace GooglePhotoManager;

public partial class MainWindow : Window
{
    private readonly AdbService _adbService = new();

    public MainWindow()
    {
        InitializeComponent();

        WizardTab.Initialize(_adbService);
        CommandsTab.Initialize(_adbService);
        DevicesTab.Initialize(_adbService);

        // Quando si cambia tab, aggiorna la lista dispositivi nel tab Comandi
        // Controlla che l'evento venga dal TabControl, non da ListBox interne (bubbling)
        MainTabs.SelectionChanged += (s, e) =>
        {
            if (e.Source == MainTabs && MainTabs.SelectedIndex == 1)
            {
                CommandsTab.RefreshDeviceList();
            }
        };
    }

    protected override async void OnClosed(EventArgs e)
    {
        await _adbService.StopAsync();
        base.OnClosed(e);
    }
}
