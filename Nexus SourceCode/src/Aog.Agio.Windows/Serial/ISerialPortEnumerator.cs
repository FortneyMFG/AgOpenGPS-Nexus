namespace Aog.Agio.Windows;

/// <summary>
/// Enumerates available serial (COM) port names.
/// </summary>
public interface ISerialPortEnumerator
{
    /// <summary>
    /// Returns the available COM port identifiers on the machine.
    /// </summary>
    IEnumerable<string> GetPortNames();
}
