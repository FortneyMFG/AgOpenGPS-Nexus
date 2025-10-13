namespace Aog.Agio.Serial;

/// <summary>
/// Enumerates available serial port names.
/// </summary>
public interface ISerialPortEnumerator
{
    /// <summary>
    /// Returns the available serial port identifiers on the machine.
    /// </summary>
    IEnumerable<string> GetPortNames();
}
