namespace BB.Chalices.ViewModels;

// A save file found by the shadPS4 locator, shown with a bit of path context.
public class DetectedSaveViewModel
{
    public DetectedSaveViewModel(string path, string? characterName = null)
    {
        Path = path;
        CharacterName = characterName;

        var folder = System.IO.Path.GetDirectoryName(path) ?? string.Empty;
        var saveDir = System.IO.Path.GetFileName(folder);                                    // SPRJ0005
        var titleId = System.IO.Path.GetFileName(System.IO.Path.GetDirectoryName(folder) ?? ""); // CUSAxxxxx
        Location = string.IsNullOrEmpty(titleId) ? saveDir : $"{titleId} / {saveDir}";
    }

    public string Path { get; }
    public string? CharacterName { get; }
    public string FileName => System.IO.Path.GetFileName(Path);
    public string Location { get; }

    // The hunter name when we could read it, otherwise the file name.
    public string Title => string.IsNullOrWhiteSpace(CharacterName) ? FileName : CharacterName;
    public string Subtitle => string.IsNullOrWhiteSpace(CharacterName) ? Location : $"{FileName} - {Location}";
}
