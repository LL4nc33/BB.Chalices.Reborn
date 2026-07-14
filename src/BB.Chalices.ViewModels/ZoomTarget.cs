using ReactiveUI;

namespace BB.Chalices.ViewModels;

// Which column the text-size +/- buttons act on.
public enum ZoomTarget
{
    All,
    Sidebar,
    Catalogue,
    Editor,
}

// A pickable zoom target with its own label. The middle option's label follows the
// active view (Catalogue / Settings / Backups); carrying the label on the item lets
// it show correctly inside the dropdown popup, which sits outside the window tree.
public sealed class ZoomTargetOption : ReactiveObject
{
    public ZoomTarget Target { get; }

    private string _label;
    public string Label
    {
        get => _label;
        private set => this.RaiseAndSetIfChanged(ref _label, value);
    }

    public ZoomTargetOption(ZoomTarget target, string label)
    {
        Target = target;
        _label = label;
    }

    internal void SetLabel(string label) => Label = label;
}
