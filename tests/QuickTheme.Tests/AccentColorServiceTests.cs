using System.Drawing;
using QuickTheme.Services;

namespace QuickTheme.Tests;

public sealed class AccentColorServiceTests
{
    private const string DwmPath = @"Software\Microsoft\Windows\DWM";
    private const string ExplorerAccentPath = @"Software\Microsoft\Windows\CurrentVersion\Explorer\Accent";
    private const string PersonalizePath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    private const string DesktopPath = @"Control Panel\Desktop";
    private const string ThemesHistoryPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\History";

    [Fact]
    public void ApplyToRegistry_WritesManualAccentValues()
    {
        var store = new FakeRegistryStore();
        using var scope = AccentColorService.UseRegistryForTests(store);

        AccentColorService.ApplyToRegistry(
            Color.FromArgb(0, 120, 212),
            accentOnTaskbar: true,
            accentOnTitleBars: true);

        Assert.Equal(unchecked((int)0xFFD47800), store.GetDword(DwmPath, "AccentColor"));
        Assert.Equal(unchecked((int)0xC40078D4), store.GetDword(DwmPath, "ColorizationColor"));
        Assert.Equal(unchecked((int)0xC40078D4), store.GetDword(DwmPath, "ColorizationAfterglow"));
        Assert.Equal(1, store.GetDword(DwmPath, "ColorPrevalence"));
        Assert.Equal(unchecked((int)0xFFD47800), store.GetDword(ExplorerAccentPath, "AccentColorMenu"));
        Assert.Equal(unchecked((int)0xFFC06700), store.GetDword(ExplorerAccentPath, "StartColorMenu"));
        Assert.Equal(1, store.GetDword(PersonalizePath, "ColorPrevalence"));
        Assert.Equal(0, store.GetDword(DesktopPath, "AutoColorization"));
        Assert.Equal(0, store.GetDword(ThemesHistoryPath, "AutoColor"));

        byte[] palette = store.GetBinary(ExplorerAccentPath, "AccentPalette");
        Assert.Equal(new byte[] { 0x99, 0xEB, 0xFF, 0xFF }, palette[..4]);
        Assert.Equal(new byte[] { 247, 99, 12, 255 }, palette[28..32]);
    }

    [Fact]
    public void PrepareAutomaticAccent_WritesAutomaticModeAndClearsHistory()
    {
        var store = new FakeRegistryStore();
        store.WriteDword(ThemesHistoryPath, "AutoColor", 123);
        using var scope = AccentColorService.UseRegistryForTests(store);

        AccentColorService.PrepareAutomaticAccent();

        Assert.Equal(1, store.GetDword(DesktopPath, "AutoColorization"));
        Assert.DoesNotContain((ThemesHistoryPath, "AutoColor"), store.Dwords.Keys);
        Assert.Contains((ThemesHistoryPath, "AutoColor"), store.Deleted);
    }

    [Fact]
    public void DisableAutomaticAccentInRegistry_WritesManualMode()
    {
        var store = new FakeRegistryStore();
        using var scope = AccentColorService.UseRegistryForTests(store);

        AccentColorService.DisableAutomaticAccentInRegistry();

        Assert.Equal(0, store.GetDword(DesktopPath, "AutoColorization"));
        Assert.Equal(0, store.GetDword(ThemesHistoryPath, "AutoColor"));
    }

    [Fact]
    public void AutomaticAccent_ReadsRegistry()
    {
        var store = new FakeRegistryStore();
        using var scope = AccentColorService.UseRegistryForTests(store);

        Assert.False(AccentColorService.AutomaticAccent);

        store.WriteDword(DesktopPath, "AutoColorization", 1);
        Assert.True(AccentColorService.AutomaticAccent);

        store.WriteDword(DesktopPath, "AutoColorization", 0);
        Assert.False(AccentColorService.AutomaticAccent);
    }

    [Fact]
    public void CurrentAccent_ReadsAbgrValue()
    {
        var store = new FakeRegistryStore();
        store.WriteDword(DwmPath, "AccentColor", unchecked((int)0xFFD47800));
        using var scope = AccentColorService.UseRegistryForTests(store);

        Assert.Equal(Color.FromArgb(0, 120, 212).ToArgb(), AccentColorService.CurrentAccent.ToArgb());
    }

    [Fact]
    public void SyncAccentFromColorization_CopiesRgb()
    {
        var store = new FakeRegistryStore();
        store.WriteDword(DwmPath, "ColorizationColor", unchecked((int)0xC40078D4));
        using var scope = AccentColorService.UseRegistryForTests(store);

        AccentColorService.SyncAccentFromColorization();

        Assert.Equal(unchecked((int)0xFFD47800), store.GetDword(DwmPath, "AccentColor"));
        Assert.Equal(unchecked((int)0xFFD47800), store.GetDword(ExplorerAccentPath, "AccentColorMenu"));
    }

    [Fact]
    public void AccentPrevalence_RoundTrips()
    {
        var store = new FakeRegistryStore();
        using var scope = AccentColorService.UseRegistryForTests(store);

        AccentColorService.AccentOnTaskbar = true;
        AccentColorService.AccentOnTitleBars = true;

        Assert.True(AccentColorService.AccentOnTaskbar);
        Assert.True(AccentColorService.AccentOnTitleBars);
        Assert.Equal(1, store.GetDword(PersonalizePath, "ColorPrevalence"));
        Assert.Equal(1, store.GetDword(DwmPath, "ColorPrevalence"));
    }

    private sealed class FakeRegistryStore : IRegistryStore
    {
        public Dictionary<(string Path, string Name), int> Dwords { get; } = new();
        public Dictionary<(string Path, string Name), byte[]> Binaries { get; } = new();
        public HashSet<(string Path, string Name)> Deleted { get; } = new();

        public int? ReadDword(string path, string name) =>
            Dwords.TryGetValue((path, name), out int value) ? value : null;

        public void WriteDword(string path, string name, int value)
        {
            Dwords[(path, name)] = value;
            Deleted.Remove((path, name));
        }

        public byte[]? ReadBinary(string path, string name) =>
            Binaries.TryGetValue((path, name), out byte[]? value) ? value : null;

        public void WriteBinary(string path, string name, byte[] value)
        {
            Binaries[(path, name)] = value;
            Deleted.Remove((path, name));
        }

        public void DeleteValue(string path, string name)
        {
            Dwords.Remove((path, name));
            Binaries.Remove((path, name));
            Deleted.Add((path, name));
        }

        public int GetDword(string path, string name) =>
            Dwords.TryGetValue((path, name), out int value)
                ? value
                : throw new KeyNotFoundException($"{path}\\{name}");

        public byte[] GetBinary(string path, string name) =>
            Binaries.TryGetValue((path, name), out byte[]? value)
                ? value
                : throw new KeyNotFoundException($"{path}\\{name}");
    }
}
