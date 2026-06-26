using ReactiveUI;

namespace BB.Chalices.ViewModels;

// One of the six chalice altar slots, shown as a card: the dungeon type plus a
// short summary of its rites/poison/4th-layer state.
public class SlotViewModel : ViewModelBase
{
    private bool _occupied;
    private string _headline = "empty";
    private string _detail = string.Empty;

    public SlotViewModel(int number) => Number = number;

    public int Number { get; }
    public string Label => $"Slot {Number}";

    public bool Occupied
    {
        get => _occupied;
        set => this.RaiseAndSetIfChanged(ref _occupied, value);
    }

    public string Headline
    {
        get => _headline;
        set => this.RaiseAndSetIfChanged(ref _headline, value);
    }

    public string Detail
    {
        get => _detail;
        set => this.RaiseAndSetIfChanged(ref _detail, value);
    }
}
