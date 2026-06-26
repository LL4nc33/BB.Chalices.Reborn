using ReactiveUI;

namespace BB.Chalices.ViewModels;

// One of the six chalice altar slots, plus a short note on what's in it.
public class SlotViewModel : ViewModelBase
{
    private string _summary = "empty";
    private bool _occupied;

    public SlotViewModel(int number) => Number = number;

    public int Number { get; }
    public string Label => $"Slot {Number}";

    public string Summary
    {
        get => _summary;
        set => this.RaiseAndSetIfChanged(ref _summary, value);
    }

    public bool Occupied
    {
        get => _occupied;
        set => this.RaiseAndSetIfChanged(ref _occupied, value);
    }
}
