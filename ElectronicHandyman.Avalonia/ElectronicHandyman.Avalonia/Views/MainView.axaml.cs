using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;
using ElectronicHandyman.Avalonia.ViewModels;

namespace ElectronicHandyman.Avalonia.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
        AttachedToVisualTree += OnAttachedToVisualTree;
        DetachedFromVisualTree += OnDetachedFromVisualTree;
    }

    private async void OnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            await vm.StartAsync();
        }
    }

    private async void OnDetachedFromVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            await vm.StopAsync();
        }
    }
}