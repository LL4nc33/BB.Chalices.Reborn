using BB.Chalices.Core.Binary;
using ReactiveUI;

namespace BB.Chalices.ViewModels;

// One headstone field in the advanced editor. Editing the hex applies it to the
// save as soon as it's a complete, valid value for that field.
public class HeadstoneFieldViewModel : ViewModelBase
{
    private readonly Action<Headstone.HeadstoneField, string> _apply;
    private string _hex = string.Empty;
    private bool _suppress;

    public HeadstoneFieldViewModel(Headstone.HeadstoneField field, Action<Headstone.HeadstoneField, string> apply)
    {
        Field = field;
        _apply = apply;
    }

    public Headstone.HeadstoneField Field { get; }
    public string Label => Field.Name;
    public int Offset => Field.Offset;
    public string OffsetLabel => $"0x{Field.Offset:X2} · {Field.Length}b";

    public string Hex
    {
        get => _hex;
        set
        {
            this.RaiseAndSetIfChanged(ref _hex, value);
            if (!_suppress)
                _apply(Field, value);
        }
    }

    // Set the displayed value without applying it (used when loading a slot).
    public void Set(string hex)
    {
        _suppress = true;
        Hex = hex;
        _suppress = false;
    }
}
