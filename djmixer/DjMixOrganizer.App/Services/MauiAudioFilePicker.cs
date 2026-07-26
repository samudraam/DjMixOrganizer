namespace DjMixOrganizer.App.Services;

/// <summary>
/// Cross-platform audio picker backed by .NET MAUI Essentials.
/// </summary>
public sealed class MauiAudioFilePicker : IAudioFilePicker
{
    /// <inheritdoc />
    public Task<FileResult?> PickAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var options = new PickOptions
        {
            PickerTitle = "Select an audio file",
        };

        if (MainThread.IsMainThread)
        {
            return FilePicker.Default.PickAsync(options);
        }

        return MainThread.InvokeOnMainThreadAsync(
            () => FilePicker.Default.PickAsync(options));
    }
}
