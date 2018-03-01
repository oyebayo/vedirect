using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using System.Threading;

namespace mpptReader
{
    public class VEDirect
    {
        private string serialport;
        private SerialPort ser;
        private char header1;
        private char header2;
        private char hexmarker;
        private char delimiter;
        private string key;
        private string value;
        private ReadState state;
        private Dictionary<string, string> dict;
        private int bytes_sum;

        private enum ReadState
        {
            HEX,
            WAIT_HEADER,
            IN_KEY,
            IN_VALUE,
            IN_CHECKSUM
        }

        public VEDirect(string serialport, int timeout)
        {
            this.serialport = serialport;
            this.ser = new SerialPort(serialport, 19200, Parity.None, 8, StopBits.One);
            this.header1 = '\r';
            this.header2 = '\n';
            this.hexmarker = ':';
            this.delimiter = '\t';
            this.key = "";
            this.value = "";
            this.bytes_sum = 0;
            this.state = ReadState.WAIT_HEADER;
            this.dict = new Dictionary<string, string>();
        }

        public Dictionary<string, string> input(char byte1)
        {
            if (byte1 == hexmarker && state != ReadState.IN_CHECKSUM)
                state = ReadState.HEX;

            switch (state)
            {
                case ReadState.WAIT_HEADER:
                    bytes_sum += Convert.ToByte(byte1);
                    if (byte1 == header1)
                        state = ReadState.WAIT_HEADER;
                    else if (byte1 == header2)
                        state = ReadState.IN_KEY;

                    break;
                case ReadState.IN_KEY:
                    bytes_sum += Convert.ToByte(byte1);
                    if (byte1 == delimiter)
                    {
                        if (key == "Checksum")
                            state = ReadState.IN_CHECKSUM;
                        else
                            state = ReadState.IN_VALUE;
                    }
                    else
                    {
                        key += byte1;
                    }

                    break;
                case ReadState.IN_VALUE:
                    bytes_sum += Convert.ToByte(byte1);
                    if (byte1 == header1)
                    {
                        state = ReadState.WAIT_HEADER;
                        if (dict.ContainsKey(key))
                            dict[key] = value;
                        else
                            dict.Add(key, value);
                        key = "";
                        value = "";
                    }
                    else
                    {
                        value += byte1;
                    }
                    break;
                case ReadState.IN_CHECKSUM:
                    bytes_sum += Convert.ToByte(byte1);
                    key = "";
                    value = "";
                    state = ReadState.WAIT_HEADER;
                    if (bytes_sum % 256 == 0)
                    {
                        bytes_sum = 0;
                        return dict;
                    }
                    else
                    {
                        Console.WriteLine("Malformed packet");
                        bytes_sum = 0;
                    }
                    break;
                case ReadState.HEX:
                    bytes_sum = 0;
                    if (byte1 == header2)
                        state = ReadState.WAIT_HEADER;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(string.Format("Unknown readstate {0}", state));
            }
            return null;
        }

        public void read_data()
        {
            ser.Open();
            while (true)
            {
                char byte1 = (char)ser.ReadChar();
                var packet = input(byte1);
            }
        }

        public Dictionary<string, string> read_data_single()
        {
            ser.Open();
            while (true)
            {
                char byte1 = (char)ser.ReadChar();
                var packet = input(byte1);
                if (packet != null)
                {
                    ser.Close();
                    return packet;
                }
            }
        }

        public void read_data_callback(Action<Dictionary<string, string>> callbackFunction)
        {
            ser.Open();
            while (true)
            {
                Thread.Sleep(5);
                var byte1 = (char)ser.ReadChar();
                if (byte1 != 0)
                {
                    var packet = input(byte1);
                    if (packet != null) callbackFunction(packet);
                }
                else
                {
                    ser.Close();
                    break;
                }
            }
        }
        public void print_data_callback(Dictionary<string, string> data)
        {
            foreach (KeyValuePair<string, string> kvp in data)
            {
                Console.WriteLine("{0}:{1}", kvp.Key, kvp.Value);
            }
        }
    }
}
