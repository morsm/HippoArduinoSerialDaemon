using System;
using System.IO.Ports;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Termors.Serivces.HippoArduinoSerialDaemon
{
    public sealed class SerialDaemon
    {
        private readonly SerialPort _port;
        private readonly SemaphoreSlim _mutex = new SemaphoreSlim(1, 1);

        private static SerialDaemon _instance;

        protected SerialDaemon(string device, int baudrate)
        {
            _port = new SerialPort
            {
                PortName = device,
                BaudRate = baudrate,
                DataBits = 8,
                Parity = Parity.None,
                StopBits = StopBits.One,
                ReadTimeout = 500,
                WriteTimeout = 500
            };

            _port.Open();
        }

        public static SerialDaemon Instance
        {
            get
            {
                return _instance;
            }
        }

        public static void Initialize(Configuration cfg)
        {
            _instance = new SerialDaemon(cfg.Device, cfg.Baudrate);
        }

        public async Task<string> SendCommand(string command)
        {
            // Get exclusive access to the port
            await _mutex.WaitAsync();

            try
            {
                byte[] cmdBytes = Encoding.ASCII.GetBytes(command);
                byte[] SOH = Encoding.ASCII.GetBytes("_");

                // Send SOH to reset everything
                await _port.BaseStream.WriteAsync(SOH, 0, SOH.Length);

                // Send the command
                await _port.BaseStream.WriteAsync(cmdBytes, 0, cmdBytes.Length);

                // Read the response and return it
                return await ReadResponse();

            }
            finally
            {
                _mutex.Release();
            }
        }

        private async Task<string> ReadResponse()
        {
            byte[] buffer = new byte[10];       // Very short responses
            byte[] nextByte = new byte[1];

            byte[] SOH = Encoding.ASCII.GetBytes("_");
            bool bSOH = false;

            int bufIdx = 0;
            DateTime startTime = DateTime.Now;

            // Read at most 10 characters within 2 seconds
            while (bufIdx < buffer.Length && DateTime.Now.Subtract(startTime).Milliseconds < 2000)
            {
                int read = await _port.BaseStream.ReadAsync(nextByte, 0, 1);
                if (read == 0) break;       // No more bytes within 500 ms

                if (! bSOH)
                {
                    // No SOH received yet, so ignore everything unless it's an SOH
                    if (nextByte[0] == SOH[0]) bSOH = true;
                }
                else
                {
                    // SOH already received. If this is another SOH character, we're done
                    if (nextByte[0] == SOH[0]) break;

                    // This is a normal byte
                    buffer[bufIdx++] = nextByte[0];
                }

            }

            return Encoding.ASCII.GetString(buffer);
        }

    }
}
