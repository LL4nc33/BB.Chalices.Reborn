using BB.Chalices.Core.Binary;

namespace BB.Chalices.Core.Tests.Binary;

public class HexDumpTests
{
    [Fact]
    public void HexDump_NonAlignedRecord_FormatsRowsAndMasksOutOfRecordBytes()
    {
        var fileBytes = new byte[48];
        fileBytes[0] = 0x41; // printable 'A', but sits before recordStart so it is masked
        fileBytes[4] = 0x41; // 'A', inside the record
        fileBytes[5] = 0x01; // control byte, inside the record

        string dump = Headstone.HexDump(fileBytes, recordStart: 4, recordLength: 24);
        string[] lines = dump.Split('\n').Select(l => l.TrimEnd('\r')).ToArray();

        Assert.Equal("Offset(h)  00 01 02 03 04 05 06 07 08 09 0A 0B 0C 0D 0E 0F", lines[0]);

        // recordStart 4 with length 24 spans two 16-byte rows: 0x00000000 and 0x00000010.
        Assert.StartsWith("00000000", lines[2]);
        Assert.StartsWith("00000010", lines[3]);

        // index 0 holds a printable byte but is out of the record: ".." in hex, "." in ASCII.
        Assert.StartsWith("00000000   .. ", lines[2]);
        // in-record 0x41 -> "41", in-record control 0x01 -> "01".
        Assert.Contains(".. .. .. .. 41 01 ", lines[2]);
        // ASCII column: masked/control bytes show "."; the in-record 0x41 shows "A".
        Assert.EndsWith("....A...........", lines[2]);
    }
}
