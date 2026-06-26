using BB.Chalices.Core.Binary;
using ReactiveUI;

namespace BB.Chalices.ViewModels;

// One of the four rite slots on a headstone. Changing the selection applies it
// to the save immediately (unless we're just loading the slot's current state).
public class RiteSlotViewModel : ViewModelBase
{
    private readonly Action<int, Headstone.Rite> _apply;
    private Headstone.Rite _rite = Headstone.Rite.None;
    private bool _suppress;

    public RiteSlotViewModel(int index, Action<int, Headstone.Rite> apply)
    {
        Index = index;
        _apply = apply;
    }

    public int Index { get; }
    public string Label => $"Rite {Index + 1}";

    public IReadOnlyList<Headstone.Rite> Options { get; } =
    [
        Headstone.Rite.None,
        Headstone.Rite.Fetid,
        Headstone.Rite.Rotted,
        Headstone.Rite.Cursed,
        Headstone.Rite.Sinister,
    ];

    public Headstone.Rite Rite
    {
        get => _rite;
        set
        {
            this.RaiseAndSetIfChanged(ref _rite, value);
            if (!_suppress)
                _apply(Index, value);
        }
    }

    // Set the displayed value without triggering an apply (used when loading a slot).
    public void Set(Headstone.Rite rite)
    {
        _suppress = true;
        Rite = rite;
        _suppress = false;
    }
}
