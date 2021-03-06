using System;
using Microsoft.SPOT;
using System.IO.Ports;
using CoreCommunication;

using SecretLabs.NETMF.Hardware;
using SecretLabs.NETMF.Hardware.Netduino;

using System.Threading;

namespace XBee
{
    public delegate void ReceivedRemoteFrameEventHandler(object sender, Frame frame);

    public class FrameDroppedByChecksumEventArgs : EventArgs
    {
        private byte[] rawBytes;

        public FrameDroppedByChecksumEventArgs(byte[] bytes)
        {
            this.rawBytes = bytes;
        }

        public byte[] RawBytes
        {
            get { return this.rawBytes; }
        }
    }

    public delegate void FrameDroppedByChecksumEventHandler(object sender, FrameDroppedByChecksumEventArgs e);

    public class BytesReadFromSerialEventArgs
    {
        private byte[] rawBytes;

        public BytesReadFromSerialEventArgs(byte[] bytes)
        {
            this.rawBytes = bytes;
        }

        public byte[] RawBytes
        {
            get { return this.rawBytes; }
        }
    }

    public delegate void BytesReadFromSerialEventHandler(object sender, BytesReadFromSerialEventArgs e);

    class XBeeCoordinator
    {
        public event ReceivedRemoteFrameEventHandler ReceivedRemoteFrame;
        public event FrameDroppedByChecksumEventHandler FrameDroppedByChecksum;
        public event BytesReadFromSerialEventHandler BytesReadFromSerial;

        private SerialPort serialPort;
        private ByteBuffer rx_buffer;
        private FrameQueueService RequestResponseService;

        public XBeeCoordinator(SerialPort serialPort)
        {
            this.serialPort = serialPort;
            this.serialPort.Open();
            this.serialPort.ErrorReceived += new SerialErrorReceivedEventHandler(ErrorReceivedHandler);
            this.serialPort.DataReceived += new SerialDataReceivedEventHandler(DropIncomingBytes);

            rx_buffer = new ByteBuffer();
            RequestResponseService = new FrameQueueService();
            ReceivedRemoteFrame += RequestResponseService.onReceivedRemoteFrame;
            RequestResponseService.SendFrame += WriteFrame;
        }

        private void WriteFrame(Frame frame)
        {
            byte[] rawFrame = FrameSerializer.Serialize(frame);

            serialPort.Write(rawFrame, 0, rawFrame.Length);
            serialPort.Flush();
        }

        public void EnqueueFrame(ATCommandFrame frame, Callback callback)
        {
            RequestResponseService.EnqueueFrame(frame, callback);
        }

        public void StartListen()
        {
            serialPort.DataReceived -= (SerialDataReceivedEventHandler)DropIncomingBytes;
            serialPort.DataReceived += (SerialDataReceivedEventHandler)DataReceivedHandler;
        }

        public void StopListen()
        {
            serialPort.DataReceived -= (SerialDataReceivedEventHandler)DataReceivedHandler;
            serialPort.DataReceived += (SerialDataReceivedEventHandler)DropIncomingBytes;
        }

        private void DropIncomingBytes(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort serialPort = (SerialPort)sender;
            if (serialPort != this.serialPort) { return; }

            if (serialPort != this.serialPort) { return; }

            int nBytes = serialPort.BytesToRead;
            if (nBytes > 0)
            {
                readBytesFromSerial(serialPort, nBytes);
            }
        }

        private void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort serialPort = (SerialPort)sender;
            if (serialPort != this.serialPort) { return; }

            int nBytes = serialPort.BytesToRead;
            if (nBytes > 0)
            {
                // Merge RxBuffer and incoming bytes to buffer
                byte[] bytes = readBytesFromSerial(serialPort, nBytes);
                rx_buffer.AddBytes(bytes);

                // Slice and Parse frames
                int index = 0;
                byte[] rawFrame = FrameSlicer.nextFrameFromBuffer(rx_buffer.RawBytes, index);
                while (rawFrame.Length > 0)
                {
                    handleRawFrameRead(rawFrame);

                    index += rawFrame.Length;
                    rawFrame = FrameSlicer.nextFrameFromBuffer(rx_buffer.RawBytes, index);
                }

                // Save partial last Frame
                rx_buffer.RemoveFirstNBytes(index);
            }
        }

        private void ErrorReceivedHandler(object sender, SerialErrorReceivedEventArgs e)
        {
            Debug.Print("Serial error received with type: " + e.EventType);
        }

        private void handleRawFrameRead(byte[] rawFrame)
        {
            if (isValidChecksum(rawFrame))
            {
                Frame frame = FrameParser.FrameFromRawBytes(rawFrame);
                if (frame != null)
                {
                    OnRecievedFrame(frame);
                }
            }
            else
            {
                OnFrameDropped(new FrameDroppedByChecksumEventArgs(rawFrame));
            }
        }

        private void OnRecievedFrame(Frame frame)
        {
            ReceivedRemoteFrameEventHandler handler = ReceivedRemoteFrame;
            if (handler != null)
            {
                handler(this, frame);
            }
        }

        private void OnFrameDropped(FrameDroppedByChecksumEventArgs e)
        {
            FrameDroppedByChecksumEventHandler handler = FrameDroppedByChecksum;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        private void OnBytesReadFromSerial(BytesReadFromSerialEventArgs e)
        {
            BytesReadFromSerialEventHandler handler = BytesReadFromSerial;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        private byte[] readBytesFromSerial(SerialPort port, int nBytes)
        {
            byte[] buff = new byte[nBytes];
            int nRead = serialPort.Read(buff, 0, buff.Length);

            OnBytesReadFromSerial(new BytesReadFromSerialEventArgs(buff));
            
            return buff;
        }

        private bool isValidChecksum(byte[] rawFrame)
        {
            int sum = 0;
            int checksumIndex = rawFrame.Length - 1;
            for (int i = 3; i < checksumIndex; ++i)
            {
                sum += rawFrame[i];
            }

            return rawFrame[checksumIndex] == 0xFF - (sum & 0xFF);
        }
    }
}
