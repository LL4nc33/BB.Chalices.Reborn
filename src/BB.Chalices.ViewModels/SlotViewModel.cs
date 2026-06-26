using ReactiveUI;

namespace BB.Chalices.ViewModels;

// One of the six chalice altar slots, shown in the slot dropdown.
public class SlotViewModel : ViewModelBase
{
    private bool _occupied;
    private string _headline = "empty";

    public SlotViewModel(int number) => Number = number;

    public int Number { get; }
    public string Label => $"Slot {Number}";
    public string Display => $"Slot {Number}  —  {(_occupied ? _headline : "empty")}";

    public bool Occupied
    {
        get => _occupied;
        set
        {
            this.RaiseAndSetIfChanged(ref _occupied, value);
            this.RaisePropertyChanged(nameof(Display));
        }
    }

    public string Headline
    {
        get => _headline;
        set
        {
            this.RaiseAndSetIfChanged(ref _headline, value);
            this.RaisePropertyChanged(nameof(Display));
        }
    }
}
