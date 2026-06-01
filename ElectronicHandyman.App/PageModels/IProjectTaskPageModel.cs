using CommunityToolkit.Mvvm.Input;
using ElectronicHandyman.App.Models;

namespace ElectronicHandyman.App.PageModels;

public interface IProjectTaskPageModel
{
    IAsyncRelayCommand<ProjectTask> NavigateToTaskCommand { get; }

    bool IsBusy { get; }
}