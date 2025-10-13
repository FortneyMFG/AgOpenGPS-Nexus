namespace Aog.Agio.Legacy;

/// <summary>
/// Known MCU identifiers reported by legacy firmware during discovery.
/// </summary>
public enum LegacyDeviceMcu : byte
{
    /// <summary>
    /// MCU type is unknown or not enumerated.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// AVR family (ATmega328p/ATmega2560).
    /// </summary>
    Avr = 1,

    /// <summary>
    /// Espressif ESP32 family.
    /// </summary>
    Esp32 = 2,

    /// <summary>
    /// PJRC Teensy / ARM microcontrollers.
    /// </summary>
    Teensy = 3,

    /// <summary>
    /// STMicroelectronics STM32 family.
    /// </summary>
    Stm32 = 4,
}
