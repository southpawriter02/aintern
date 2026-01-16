namespace AIntern.Desktop.ViewModels;

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using AIntern.Core.Interfaces;

/// <summary>
/// ViewModel for the recent workspaces menu.
/// </summary>
/// <remarks>Added in v0.3.5e.</remarks>
public partial class RecentWorkspacesViewModel : ViewModelBase
{
    private readonly IWorkspaceService _workspaceService;
    private readonly ILogger<RecentWorkspacesViewModel> _logger;

    /// <summary>
    /// List of recent workspaces.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<RecentWorkspaceItemViewModel> _workspaces = new();

    /// <summary>
    /// Whether there are any workspaces.
    /// </summary>
    [ObservableProperty]
    private bool _hasWorkspaces;

    /// <summary>
    /// Raised when a workspace is selected for opening.
    /// </summary>
    public event EventHandler<string>? WorkspaceSelected;

    /// <summary>
    /// Raised when the menu should close.
    /// </summary>
    public event EventHandler? CloseRequested;

    /// <summary>
    /// Creates a new RecentWorkspacesViewModel.
    /// </summary>
    public RecentWorkspacesViewModel(
        IWorkspaceService workspaceService,
        ILogger<RecentWorkspacesViewModel> logger)
    {
        _workspaceService = workspaceService;
        _logger = logger;
        _logger.LogDebug("[INIT] RecentWorkspacesViewModel created");
    }

    /// <summary>
    /// Loads recent workspaces from service.
    /// </summary>
    public async Task LoadAsync()
    {
        _logger.LogDebug("[ENTRY] LoadAsync");

        try
        {
            var recent = await _workspaceService.GetRecentWorkspacesAsync(10);

            Workspaces.Clear();
            foreach (var workspace in recent
                .OrderByDescending(w => w.IsPinned)
                .ThenByDescending(w => w.LastAccessedAt))
            {
                Workspaces.Add(new RecentWorkspaceItemViewModel(workspace));
            }

            HasWorkspaces = Workspaces.Count > 0;
            _logger.LogDebug("[EXIT] LoadAsync: count={Count}", Workspaces.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ERROR] LoadAsync failed");
        }
    }

    /// <summary>
    /// Opens the selected workspace.
    /// </summary>
    [RelayCommand]
    public void OpenWorkspace(RecentWorkspaceItemViewModel? item)
    {
        if (item == null) return;
        _logger.LogDebug("[CMD] OpenWorkspace: {Path}", item.RootPath);
        WorkspaceSelected?.Invoke(this, item.RootPath);
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Toggles pin state of workspace.
    /// </summary>
    [RelayCommand]
    public async Task TogglePinAsync(RecentWorkspaceItemViewModel? item)
    {
        if (item == null) return;

        _logger.LogDebug("[CMD] TogglePin: {Id}, current={IsPinned}", item.Id, item.IsPinned);

        item.IsPinned = !item.IsPinned;
        await _workspaceService.SetPinnedAsync(item.Id, item.IsPinned);
        await LoadAsync();
    }

    /// <summary>
    /// Removes workspace from recent list.
    /// </summary>
    [RelayCommand]
    public async Task RemoveAsync(RecentWorkspaceItemViewModel? item)
    {
        if (item == null) return;

        _logger.LogDebug("[CMD] Remove: {Id}", item.Id);

        await _workspaceService.RemoveFromRecentAsync(item.Id);
        Workspaces.Remove(item);
        HasWorkspaces = Workspaces.Count > 0;
    }

    /// <summary>
    /// Clears all recent workspaces.
    /// </summary>
    [RelayCommand]
    public async Task ClearAllAsync()
    {
        _logger.LogDebug("[CMD] ClearAll");

        await _workspaceService.ClearRecentWorkspacesAsync();
        Workspaces.Clear();
        HasWorkspaces = false;
    }
}
