using System.Linq;
using System.Text;
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

    // Compact label for the narrow editor column; the full name lives in the tooltip.
    public string Label => Field.Name switch
    {
        "Special enemy / shop" => "Special enemy",
        "Join requirements" => "Join req",
        _ => Field.Name,
    };

    public int Offset => Field.Offset;
    public string OffsetLabel => $"{Field.Name}  ·  0x{Field.Offset:X2}  ·  {Field.Length}b";

    // Shown grouped into byte pairs ("10 32 15 58"); applied as compact hex.
    public string Hex
    {
        get => _hex;
        set
        {
            string compact = new(value.Where(c => !char.IsWhiteSpace(c)).ToArray());
            this.RaiseAndSetIfChanged(ref _hex, GroupBytes(compact));
            if (!_suppress)
                _apply(Field, compact);
        }
    }

    private static string GroupBytes(string compact)
    {
        if (compact.Length <= 2)
            return compact;

        var grouped = new StringBuilder(compact.Length + compact.Length / 2);
        for (int i = 0; i < compact.Length; i++)
        {
            if (i > 0 && i % 2 == 0)
                grouped.Append(' ');
            grouped.Append(compact[i]);
        }
        return grouped.ToString();
    }

    // Set the displayed value without applying it (used when loading a slot).
    public void Set(string hex)
    {
        _suppress = true;
        Hex = hex;
        _suppress = false;
    }
}
