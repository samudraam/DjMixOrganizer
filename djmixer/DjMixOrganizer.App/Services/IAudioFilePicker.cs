namespace DjMixOrganizer.App.Services;

/// <summary>
/// Opens the platform file picker for one audio file.
/// </summary>
public interface IAudioFilePicker
{
    /// <summary>
    /// Prompts for an audio file on the UI thread.
    /// </summary>
    /// <param name="cancellationToken">Cancels the pending picker request.</param>
    /// <returns>The chosen file, or <c>null</c> when the user cancels.</returns>
    Task<FileResult?> PickAsync(CancellationToken cancellationToken = default);
}
