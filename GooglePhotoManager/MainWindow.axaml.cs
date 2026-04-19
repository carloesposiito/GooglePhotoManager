using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using System;

namespace GooglePhotoManager;

public partial class MainWindow : Window
{
    #region Campi privati

    private readonly AdbService _adbService = new();
    private int _selectedTab;
    private readonly Button[] _tabButtons;

    #endregion

    #region Proprietà

    // Nessuna proprietà pubblica

    #endregion

    #region Metodi

    public MainWindow()
    {
        InitializeComponent();

        _tabButtons = new[] { TabBtn0, TabBtn1, TabBtn2 };

        WizardTab.Initialize(_adbService);
        CommandsTab.Initialize(_adbService);
        DevicesTab.Initialize(_adbService);
    }

    // Cambia il tab attivo e aggiorna lo stile dei bottoni
    private void SelectTab(int index)
    {
        _selectedTab = index;

        WizardTab.IsVisible = index == 0;
        CommandsTab.IsVisible = index == 1;
        DevicesTab.IsVisible = index == 2;

        // Aggiorna lo stile: attivo = sfondo colorato, inattivo = trasparente
        for (int i = 0; i < _tabButtons.Length; i++)
        {
            if (i == index)
            {
                _tabButtons[i].Background = new SolidColorBrush(Color.Parse("#5C6BC0"));
                _tabButtons[i].Foreground = new SolidColorBrush(Color.Parse("#FFFFFF"));
            }
            else
            {
                _tabButtons[i].Background = new SolidColorBrush(Color.Parse("#00000000"));
                _tabButtons[i].Foreground = new SolidColorBrush(Color.Parse("#9E9E9E"));
            }
        }

        // Quando si entra nel tab Comandi, reinizializza da capo
        if (index == 1)
            CommandsTab.Reset();
    }

    #endregion

    #region Eventi

    // Click su uno dei bottoni tab
    private void OnTabClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string tag && int.TryParse(tag, out int index))
            SelectTab(index);
    }

    // Chiude la finestra
    private void OnCloseClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    // Alla chiusura della finestra, ferma il server ADB solo se inizializzato
    protected override async void OnClosed(EventArgs e)
    {
        if (_adbService.IsInitialized)
            await _adbService.StopAsync();

        base.OnClosed(e);
    }

    // Permette di trascinare la finestra dalla title bar custom
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        // Trascina solo se il click e' nella zona della title bar
        var pos = e.GetPosition(this);
        if (pos.Y <= 50 && e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            BeginMoveDrag(e);
    }

    #endregion
}
