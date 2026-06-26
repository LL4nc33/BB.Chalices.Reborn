namespace BB.Chalices.ViewModels;

// A save file found by the shadPS4 locator, shown with a bit of path context.
public class DetectedSaveViewModel
{
    public DetectedSaveViewModel(string path)
    {
        Path = path;

        var folder = System.IO.Path.GetDirectoryName(path) ?? string.Empty;
        var saveDir = System.IO.Path.GetFileName(folder);                                    // SPRJ0005
        var titleId = System.IO.Path.GetFileName(System.IO.Path.GetDirectoryName(folder) ?? ""); // CUSAxxxxx
        Location = string.IsNullOrEmpty(titleId) ? saveDir : $"{titleId} / {saveDir}";
    }

    public string Path { get; }
    public string FileName => System.IO.Path.GetFileName(Path);
    public string Location { get; }
}
