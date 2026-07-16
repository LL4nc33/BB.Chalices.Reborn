using ReactiveUI;

namespace BB.Chalices.ViewModels;

// An altar slot shown in the slot selector: slots 1-6 are the stored dungeons,
// slot 0 is the makeshift altar (the ritual-setup slot just before slot 1).
public class SlotViewModel : ViewModelBase
{
    private bool _occupied;

    public SlotViewModel(int number) => Number = number;

    public int Number { get; }

    public bool IsMakeshift => Number == 0;
    public string ShortLabel => IsMakeshift ? "M" : Number.ToString();
    public string Label => IsMakeshift ? "Makeshift altar" : $"Slot {Number}";

    public bool Occupied
    {
        get => _occupied;
        set => this.RaiseAndSetIfChanged(ref _occupied, value);
    }
}
