// File location: DjMixOrganizer.App/ViewModels/LibraryViewModel.cs
//
// TEACHING NOTES
// ---------------
// IAudioFilePicker isolates platform behavior from this ViewModel. Its MAUI
// implementation guarantees PickAsync starts on the UI thread, while the
// command remains disabled until the picker closes to prevent competing dialogs.
//
// The Save error was NOT a XAML binding bug: MySQL still had CamelotKey
// while EF inserted MusicalKey. Pending migrations now run at app startup.

using System.Collections.ObjectModel;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DjMixOrganizer.App.Services;
using DjMixOrganizer.Core.Enums;
using DjMixOrganizer.Core.Models;
using DjMixOrganizer.Core.Repositories;

namespace DjMixOrganizer.App.ViewModels;

/// <summary>Current mode of the Library song form.</summary>
public enum SongFormMode
{
    None,
    Add,
    Edit,
}

public partial class LibraryViewModel : ObservableObject
{
    private readonly ITrackRepository _trackRepository;
    private readonly IAudioFilePicker _audioFilePicker;
    private bool _isPickingFile;
    private Track? _editingTrack;

    /// <summary>Letter keys for the Add Song key picker.</summary>
    public static IReadOnlyList<string> MusicalKeys { get; } = MusicalKey.All;

    [ObservableProperty]
    private ObservableCollection<Track> _tracks = [];

    [ObservableProperty]
    private Track? _selectedTrack;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private SongFormMode _formMode;

    [ObservableProperty]
    private bool _isSaving;

    [ObservableProperty]
    private string _newTitle = string.Empty;

    [ObservableProperty]
    private string _newArtist = string.Empty;

    [ObservableProperty]
    private string _newBpmText = string.Empty;

    [ObservableProperty]
    private string? _newKey;

    /// <summary>Path persisted on the Track (typed path or copied picker file).</summary>
    [ObservableProperty]
    private string _newFilePath = string.Empty;

    /// <summary>Short label under Browse for the last picked file name.</summary>
    [ObservableProperty]
    private string _newFileDisplayName = string.Empty;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    /// <summary>Whether the shared Add/Edit form is visible.</summary>
    public bool IsFormOpen => FormMode != SongFormMode.None;

    /// <summary>Heading for the current form operation.</summary>
    public string FormHeading => FormMode == SongFormMode.Edit ? "Edit song" : "Add song";

    /// <summary>Primary button label for the current form operation.</summary>
    public string SaveButtonText => FormMode == SongFormMode.Edit ? "Save changes" : "Save song";

    public LibraryViewModel(
        ITrackRepository trackRepository,
        IAudioFilePicker audioFilePicker)
    {
        _trackRepository = trackRepository;
        _audioFilePicker = audioFilePicker;
    }

    /// <summary>Loads (or reloads) every track from the repository into the sidebar.</summary>
    [RelayCommand]
    private async Task LoadTracksAsync()
    {
        if (IsLoading || _isPickingFile || IsFormOpen)
        {
            System.Diagnostics.Debug.WriteLine(
                $"[Library] LoadTracks skipped (loading={IsLoading}, picking={_isPickingFile}, form={FormMode}).");
            return;
        }

        IsLoading = true;
        try
        {
            var tracks = await _trackRepository.GetAllAsync();
            Tracks = new ObservableCollection<Track>(tracks);
            SelectedTrack ??= Tracks.FirstOrDefault();
            System.Diagnostics.Debug.WriteLine(
                $"[Library] Loaded {Tracks.Count} track(s).");
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>Shows the Add Song form and clears any leftover field values.</summary>
    [RelayCommand]
    private void BeginAddSong()
    {
        ResetForm();
        _editingTrack = null;
        FormMode = SongFormMode.Add;
        StatusMessage = string.Empty;
    }

    /// <summary>Populates the shared form from the selected persisted track.</summary>
    [RelayCommand]
    private void BeginEditSong()
    {
        if (SelectedTrack is null)
        {
            StatusMessage = "Select a song to edit.";
            return;
        }

        _editingTrack = SelectedTrack;
        NewTitle = SelectedTrack.Title;
        NewArtist = SelectedTrack.Artist ?? string.Empty;
        NewBpmText = SelectedTrack.Bpm?.ToString() ?? string.Empty;
        NewKey = SelectedTrack.MusicalKey?.ToString();
        NewFilePath = SelectedTrack.FilePath;
        NewFileDisplayName = Path.GetFileName(SelectedTrack.FilePath);
        FormMode = SongFormMode.Edit;
        StatusMessage = string.Empty;
    }

    /// <summary>Hides the Add/Edit form without changing the persisted track.</summary>
    [RelayCommand]
    private void CancelSongForm()
    {
        FormMode = SongFormMode.None;
        _editingTrack = null;
        StatusMessage = string.Empty;
        ResetForm();
    }

    partial void OnFormModeChanged(SongFormMode value)
    {
        OnPropertyChanged(nameof(IsFormOpen));
        OnPropertyChanged(nameof(FormHeading));
        OnPropertyChanged(nameof(SaveButtonText));
    }

    /// <summary>Opens the platform file picker and fills the path fields.</summary>
    [RelayCommand]
    private async Task PickAudioFileAsync()
    {
        if (_isPickingFile)
        {
            return;
        }

        _isPickingFile = true;
        StatusMessage = "Opening file picker...";
        try
        {
            var result = await _audioFilePicker.PickAsync();

            if (result is null)
            {
                StatusMessage = string.Empty;
                return;
            }

            if (!IsSupportedAudioFile(result.FileName))
            {
                StatusMessage = "Pick an audio file (.mp3, .wav, .flac, .aiff, .m4a).";
                return;
            }

            var localPath = await CopyIntoAppStorageAsync(result);

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                NewFilePath = localPath;
                NewFileDisplayName = result.FileName;

                if (string.IsNullOrWhiteSpace(NewTitle))
                {
                    NewTitle = Path.GetFileNameWithoutExtension(result.FileName);
                }

                FormMode = _editingTrack is null ? SongFormMode.Add : SongFormMode.Edit;
                StatusMessage = $"Selected {result.FileName}";
            });

            System.Diagnostics.Debug.WriteLine(
                $"[Library] Picked '{result.FileName}' -> '{localPath}'.");
        }
        catch (Exception ex)
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                FormMode = _editingTrack is null ? SongFormMode.Add : SongFormMode.Edit;
                StatusMessage = $"Could not open file picker: {ex.Message}";
            });
            System.Diagnostics.Debug.WriteLine($"[Library] FilePicker failed: {ex}");
        }
        finally
        {
            _isPickingFile = false;
        }
    }

    /// <summary>Validates and persists the current Add or Edit form.</summary>
    [RelayCommand]
    private async Task SaveSongAsync()
    {
        if (IsSaving)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(NewTitle))
        {
            StatusMessage = "Title is required.";
            return;
        }

        if (string.IsNullOrWhiteSpace(NewFilePath))
        {
            StatusMessage = "Pick a file or paste a path in Audio file.";
            return;
        }

        double? bpm = null;
        if (!string.IsNullOrWhiteSpace(NewBpmText))
        {
            if (!double.TryParse(NewBpmText.Trim(), out var parsedBpm) || parsedBpm <= 0)
            {
                StatusMessage = "BPM must be a positive number (or leave blank).";
                return;
            }

            bpm = parsedBpm;
        }

        IsSaving = true;
        StatusMessage = "Saving...";
        try
        {
            var upload = new TrackUpload(
                Title: NewTitle.Trim(),
                Artist: string.IsNullOrWhiteSpace(NewArtist) ? null : NewArtist.Trim(),
                Bpm: bpm,
                Key: NewKey,
                Duration: _editingTrack?.Duration ?? TimeSpan.Zero,
                FilePath: NewFilePath.Trim(),
                Format: GuessFormat(NewFilePath));

            System.Diagnostics.Debug.WriteLine(
                $"[Library] {FormMode} track '{upload.Title}' key='{upload.Key}' path='{upload.FilePath}'...");

            Track savedTrack;
            if (_editingTrack is null)
            {
                savedTrack = await _trackRepository.AddAsync(upload);
                Tracks.Add(savedTrack);
            }
            else
            {
                savedTrack = await _trackRepository.UpdateAsync(_editingTrack.Id, upload);
                var index = Tracks.IndexOf(_editingTrack);
                if (index >= 0)
                {
                    Tracks[index] = savedTrack;
                }
            }

            var completedMode = FormMode;
            FormMode = SongFormMode.None;
            _editingTrack = null;
            SelectedTrack = savedTrack;
            ResetForm();
            StatusMessage = completedMode == SongFormMode.Edit
                ? $"Updated \"{savedTrack.DisplayName}\"."
                : $"Added \"{savedTrack.DisplayName}\".";
            System.Diagnostics.Debug.WriteLine(
                $"[Library] {completedMode} succeeded Id={savedTrack.Id}, Key={savedTrack.MusicalKey}.");
        }
        catch (InvalidMusicalKeyException ex)
        {
            StatusMessage = ex.Message;
            System.Diagnostics.Debug.WriteLine($"[Library] Bad key: {ex.AttemptedKey}");
        }
        catch (Exception ex)
        {
            StatusMessage = $"Save failed: {FormatException(ex)}";
            System.Diagnostics.Debug.WriteLine($"[Library] Add FAILED: {ex}");
        }
        finally
        {
            IsSaving = false;
        }
    }

    private void ResetForm()
    {
        NewTitle = string.Empty;
        NewArtist = string.Empty;
        NewBpmText = string.Empty;
        NewKey = null;
        NewFilePath = string.Empty;
        NewFileDisplayName = string.Empty;
    }

    /// <summary>Walks InnerException so DbUpdateException shows the real MySQL reason.</summary>
    private static string FormatException(Exception ex)
    {
        var parts = new StringBuilder();
        for (var current = ex; current is not null; current = current.InnerException)
        {
            if (parts.Length > 0)
            {
                parts.Append(" → ");
            }

            parts.Append(current.Message);
        }

        return parts.ToString();
    }

    private static async Task<string> CopyIntoAppStorageAsync(FileResult result)
    {
        var folder = Path.Combine(FileSystem.AppDataDirectory, "tracks");
        Directory.CreateDirectory(folder);

        var safeName = string.Join("_", result.FileName.Split(Path.GetInvalidFileNameChars()));
        var destination = Path.Combine(folder, $"{Guid.NewGuid():N}_{safeName}");

        await using var source = await result.OpenReadAsync();
        await using var target = File.Create(destination);
        await source.CopyToAsync(target);

        return destination;
    }

    private static bool IsSupportedAudioFile(string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext is ".mp3" or ".wav" or ".flac" or ".aiff" or ".aif" or ".m4a";
    }

    private static AudioFormat GuessFormat(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        return ext switch
        {
            ".wav" => AudioFormat.Wav,
            ".flac" => AudioFormat.Flac,
            ".aiff" or ".aif" => AudioFormat.Aiff,
            _ => AudioFormat.Mp3,
        };
    }
}
