namespace AIntern.Desktop.ViewModels;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ SHELL SELECTOR VIEWMODEL (v0.5.3f)                                      │
// │ ViewModel for shell profile selection dialog.                           │
// └─────────────────────────────────────────────────────────────────────────┘

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AIntern.Core.Interfaces;
using AIntern.Core.Models.Terminal;
using Microsoft.Extensions.Logging;

/// <summary>
/// ViewModel for the shell profile selector dialog.
/// </summary>
/// <remarks>
/// <para>Added in v0.5.3f.</para>
/// <para>
/// Provides functionality for:
/// <list type="bullet">
///   <item>Displaying available shell profiles</item>
///   <item>Selecting a profile to open terminal with</item>
///   <item>Creating new profiles with validation</item>
///   <item>Setting default profile</item>
///   <item>Duplicating and deleting profiles</item>
/// </list>
/// </para>
/// </remarks>
public partial class ShellSelectorViewModel : ViewModelBase
{
    // ─────────────────────────────────────────────────────────────────────
    // Dependencies
    // ─────────────────────────────────────────────────────────────────────

    private readonly IShellProfileService _profileService;
    private readonly IShellDetectionService _shellDetection;
    private readonly ILogger<ShellSelectorViewModel> _logger;

    // ─────────────────────────────────────────────────────────────────────
    // Observable Properties - Profile List
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Available shell profiles.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<ShellProfile> _profiles = new();

    /// <summary>
    /// Currently selected profile.
    /// </summary>
    [ObservableProperty]
    private ShellProfile? _selectedProfile;

    // ─────────────────────────────────────────────────────────────────────
    // Observable Properties - New Profile Form
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Whether the new profile form is visible.
    /// </summary>
    [ObservableProperty]
    private bool _showNewProfileForm;

    /// <summary>
    /// Name for the new profile.
    /// </summary>
    [ObservableProperty]
    private string _newProfileName = string.Empty;

    /// <summary>
    /// Shell path for the new profile.
    /// </summary>
    [ObservableProperty]
    private string _newProfilePath = string.Empty;

    /// <summary>
    /// Arguments for the new profile.
    /// </summary>
    [ObservableProperty]
    private string _newProfileArguments = string.Empty;

    /// <summary>
    /// Validation error message.
    /// </summary>
    [ObservableProperty]
    private string? _validationError;

    // ─────────────────────────────────────────────────────────────────────
    // Dialog Result Properties
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// The selected profile (null if dialog was cancelled).
    /// </summary>
    public ShellProfile? Result { get; private set; }

    /// <summary>
    /// Whether the dialog was confirmed.
    /// </summary>
    public bool IsConfirmed { get; private set; }

    // ─────────────────────────────────────────────────────────────────────
    // Constructor
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a new shell selector ViewModel.
    /// </summary>
    /// <param name="profileService">Shell profile service.</param>
    /// <param name="shellDetection">Shell detection service.</param>
    /// <param name="logger">Logger.</param>
    public ShellSelectorViewModel(
        IShellProfileService profileService,
        IShellDetectionService shellDetection,
        ILogger<ShellSelectorViewModel> logger)
    {
        _profileService = profileService ?? throw new ArgumentNullException(nameof(profileService));
        _shellDetection = shellDetection ?? throw new ArgumentNullException(nameof(shellDetection));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _logger.LogDebug("ShellSelectorViewModel created");
    }

    // ─────────────────────────────────────────────────────────────────────
    // Commands - Profile Loading
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Load available profiles from the profile service.
    /// </summary>
    [RelayCommand]
    public async Task LoadProfilesAsync()
    {
        _logger.LogDebug("Loading profiles for shell selector");

        try
        {
            var profiles = await _profileService.GetVisibleProfilesAsync();
            Profiles = new ObservableCollection<ShellProfile>(profiles);

            // Select default or first profile
            SelectedProfile = Profiles.FirstOrDefault(p => p.IsDefault)
                              ?? Profiles.FirstOrDefault();

            _logger.LogDebug("Loaded {Count} profiles, selected: {Selected}",
                Profiles.Count, SelectedProfile?.Name ?? "none");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load profiles");
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // Commands - Profile Selection
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Select a profile.
    /// </summary>
    [RelayCommand]
    private void SelectProfile(ShellProfile profile)
    {
        SelectedProfile = profile;
        _logger.LogDebug("Selected profile: {Name}", profile.Name);
    }

    /// <summary>
    /// Confirm selection and close dialog.
    /// </summary>
    [RelayCommand]
    private void ConfirmSelection()
    {
        Result = SelectedProfile;
        IsConfirmed = true;
        _logger.LogDebug("Selection confirmed: {Name}", SelectedProfile?.Name);
    }

    // ─────────────────────────────────────────────────────────────────────
    // Commands - New Profile Form
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Show the new profile form.
    /// </summary>
    [RelayCommand]
    private void ShowNewProfile()
    {
        ShowNewProfileForm = true;
        NewProfileName = string.Empty;
        NewProfilePath = string.Empty;
        NewProfileArguments = string.Empty;
        ValidationError = null;

        _logger.LogDebug("Showing new profile form");
    }

    /// <summary>
    /// Cancel new profile creation.
    /// </summary>
    [RelayCommand]
    private void CancelNewProfile()
    {
        ShowNewProfileForm = false;
        ValidationError = null;

        _logger.LogDebug("Cancelled new profile form");
    }

    /// <summary>
    /// Create a new profile with validation.
    /// </summary>
    [RelayCommand]
    private async Task CreateProfileAsync()
    {
        ValidationError = null;
        _logger.LogDebug("Creating new profile: {Name}", NewProfileName);

        // Validate name
        if (string.IsNullOrWhiteSpace(NewProfileName))
        {
            ValidationError = "Profile name is required";
            _logger.LogDebug("Validation failed: empty name");
            return;
        }

        // Validate path
        if (string.IsNullOrWhiteSpace(NewProfilePath))
        {
            ValidationError = "Shell path is required";
            _logger.LogDebug("Validation failed: empty path");
            return;
        }

        // Validate shell path exists
        if (!_shellDetection.ValidateShellPath(NewProfilePath))
        {
            ValidationError = "Invalid shell path - file does not exist or is not executable";
            _logger.LogDebug("Validation failed: invalid path {Path}", NewProfilePath);
            return;
        }

        try
        {
            // Detect shell type from path
            var shellType = _shellDetection.DetectShellType(NewProfilePath);

            var profile = new ShellProfile
            {
                Name = NewProfileName.Trim(),
                ShellPath = NewProfilePath.Trim(),
                Arguments = string.IsNullOrWhiteSpace(NewProfileArguments)
                    ? null
                    : NewProfileArguments.Trim(),
                ShellType = shellType
            };

            var created = await _profileService.CreateProfileAsync(profile);
            _logger.LogInformation("Created new profile: {Id} {Name}", created.Id, created.Name);

            // Reload and select the new profile
            await LoadProfilesAsync();
            ShowNewProfileForm = false;
            SelectedProfile = Profiles.FirstOrDefault(p => p.Id == created.Id);
        }
        catch (Exception ex)
        {
            ValidationError = ex.Message;
            _logger.LogError(ex, "Failed to create profile");
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // Commands - Profile Management
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Set a profile as the default.
    /// </summary>
    [RelayCommand]
    private async Task SetAsDefaultAsync(ShellProfile profile)
    {
        _logger.LogDebug("Setting default profile: {Name}", profile.Name);

        try
        {
            await _profileService.SetDefaultProfileAsync(profile.Id);
            await LoadProfilesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set default profile");
        }
    }

    /// <summary>
    /// Delete a profile (non-built-in only).
    /// </summary>
    [RelayCommand]
    private async Task DeleteProfileAsync(ShellProfile profile)
    {
        // Prevent deletion of built-in profiles
        if (profile.IsBuiltIn)
        {
            _logger.LogDebug("Cannot delete built-in profile: {Name}", profile.Name);
            return;
        }

        _logger.LogDebug("Deleting profile: {Name}", profile.Name);

        try
        {
            await _profileService.DeleteProfileAsync(profile.Id);
            await LoadProfilesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete profile");
        }
    }

    /// <summary>
    /// Duplicate a profile.
    /// </summary>
    [RelayCommand]
    private async Task DuplicateProfileAsync(ShellProfile profile)
    {
        _logger.LogDebug("Duplicating profile: {Name}", profile.Name);

        try
        {
            var duplicate = await _profileService.DuplicateProfileAsync(profile.Id);
            await LoadProfilesAsync();
            SelectedProfile = Profiles.FirstOrDefault(p => p.Id == duplicate.Id);

            _logger.LogInformation("Duplicated profile: {Original} -> {New}",
                profile.Name, duplicate.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to duplicate profile");
        }
    }
}
