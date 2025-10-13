namespace Aog.Agio.Serial;

/// <summary>
/// Enumerates available serial device identifiers.
/// </summary>
public interface ISerialPortEnumerator
{
    /// <summary>
    /// Returns the available serial device identifiers on the machine.
    /// </summary>
    IEnumerable<string> GetPortNames();
}
