using System.IO.Ports;
using System.Text;

namespace Aog.Agio.Serial;

/// <summary>
/// Creates <see cref="SerialPort"/> backed probe sessions.
/// </summary>
public sealed class SerialPortSessionFactory : ISerialPortSessionFactory
{
    /// <inheritdoc />
    public ISerialPortSession Create(string portName, int baudRate, TimeSpan readTimeout)
    {
        return new SerialPortSession(portName, baudRate, readTimeout);
    }

    private sealed class SerialPortSession : ISerialPortSession
    {
        private readonly SerialPort _serialPort;
        private bool _disposed;

        public SerialPortSession(string portName, int baudRate, TimeSpan readTimeout)
        {
            if (string.IsNullOrWhiteSpace(portName))
            {
                throw new ArgumentException("Port name is required.", nameof(portName));
            }

            if (baudRate <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(baudRate));
            }

            var timeoutMilliseconds = (int)Math.Clamp(readTimeout.TotalMilliseconds, 1, int.MaxValue);

            _serialPort = new SerialPort(portName, baudRate)
            {
                ReadTimeout = timeoutMilliseconds,
                WriteTimeout = timeoutMilliseconds,
                Encoding = Encoding.ASCII,
                NewLine = "\r\n",
                DtrEnable = true,
                RtsEnable = true,
            };
        }

        public string PortName => _serialPort.PortName;

        public int BaudRate => _serialPort.BaudRate;

        public void Open()
        {
            ThrowIfDisposed();

            if (_serialPort.IsOpen)
            {
                return;
            }

            _serialPort.Open();
            _serialPort.DiscardInBuffer();
        }

        public string? TryReadLine()
        {
            ThrowIfDisposed();

            if (!_serialPort.IsOpen)
            {
                throw new InvalidOperationException("Serial port must be opened before reading.");
            }

            try
            {
                return _serialPort.ReadLine();
            }
            catch (TimeoutException)
            {
                return null;
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            try
            {
                if (_serialPort.IsOpen)
                {
                    _serialPort.Close();
                }
            }
            catch
            {
                // Ignore shutdown exceptions.
            }

            _serialPort.Dispose();
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(SerialPortSession));
            }
        }
    }
}
