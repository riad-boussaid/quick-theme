using System.Security;
using Microsoft.Win32;

namespace QuickTheme.Services;

internal interface IRegistryStore
{
    int? ReadDword(string path, string name);
    void WriteDword(string path, string name, int value);
    byte[]? ReadBinary(string path, string name);
    void WriteBinary(string path, string name, byte[] value);
    void DeleteValue(string path, string name);
}

internal sealed class WindowsRegistryStore : IRegistryStore
{
    public int? ReadDword(string path, string name)
    {
        try
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(path);
            return key?.GetValue(name) is int value ? value : null;
        }
        catch (Exception ex) when (IsRegistryException(ex))
        {
            return null;
        }
    }

    public void WriteDword(string path, string name, int value)
    {
        using RegistryKey key = Registry.CurrentUser.CreateSubKey(path, writable: true)
            ?? throw new InvalidOperationException($"Could not open HKCU\\{path}.");

        key.SetValue(name, value, RegistryValueKind.DWord);
    }

    public byte[]? ReadBinary(string path, string name)
    {
        try
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(path);
            return key?.GetValue(name) as byte[];
        }
        catch (Exception ex) when (IsRegistryException(ex))
        {
            return null;
        }
    }

    public void WriteBinary(string path, string name, byte[] value)
    {
        using RegistryKey key = Registry.CurrentUser.CreateSubKey(path, writable: true)
            ?? throw new InvalidOperationException($"Could not open HKCU\\{path}.");

        key.SetValue(name, value, RegistryValueKind.Binary);
    }

    public void DeleteValue(string path, string name)
    {
        try
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(path, writable: true);
            key?.DeleteValue(name, throwOnMissingValue: false);
        }
        catch (Exception ex) when (IsRegistryException(ex))
        {
            // The value may not exist or may not be writable.
        }
    }

    private static bool IsRegistryException(Exception ex) =>
        ex is SecurityException or UnauthorizedAccessException or IOException;
}
