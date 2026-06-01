using System;
using Avalonia.Controls;
using ElectronicHandyman.Avalonia.ViewModels;

namespace ElectronicHandyman.Avalonia.Services;

public class NavigationService
{
    private readonly ContentControl _host;
    private readonly Func<ViewModelBase, Control> _viewFactory;

    public NavigationService(ContentControl host, Func<ViewModelBase, Control> viewFactory)
    {
        _host = host;
        _viewFactory = viewFactory;
    }

    public void Navigate(ViewModelBase viewModel)
    {
        var view = _viewFactory(viewModel);
        view.DataContext = viewModel;
        _host.Content = view;
    }
}
