using System;
using Microsoft.SPOT;

namespace CoreCommunication
{
    class FrameBuilder
    {
        // Private variables
        private FrameType frameType;
        private byte[] destinationAddress16Bit;
        private byte[] destinationAddress64Bit;
        private RemoteATCommandOptions CmdOptions = RemoteATCommandOptions.ApplyChanges; // Bit 1: Apply changes on remote device. NOTE: If this bit is not set, an AC (or WR+FR) command must be sent before changes will take effect.
        private string commandName;
        private byte[] commandData;
        private CommandStatus commandStatus;

        // Public variables
        public FrameType FrameType
        {
            get { return frameType; }
        }

        public byte[] DestinationAddress16Bit
        {
            get { return destinationAddress16Bit; }
        }

        public byte[] DestinationAddress64Bit
        {
            get { return destinationAddress64Bit; }
        }

        public RemoteATCommandOptions CommandOptions
        {
            get { return CmdOptions; }
        }

        public string ATCommandName
        {
            get { return commandName; }
        }

        public byte[] ATCommandData
        {
            get { return commandData; }
        }

        public CommandStatus CommandStatus
        {
            get { return commandStatus; }
        }

        // Constructor
        public FrameBuilder(FrameType frameType)
        {
            this.frameType = frameType;
            setDefaultValues();
        }

        // Builder methods
        public Frame Build()
        {
            Frame frame = null;
            switch (frameType)
            {
                case FrameType.RemoteATCommand:
                {
                    RemoteATCommandRequestFrame _frame = new RemoteATCommandRequestFrame();
                    _frame.type = FrameType;
                    _frame.variableDataLength = ATCommandData.Length;
                    _frame.DestinationAddress16Bit = destinationAddress16Bit;
                    _frame.DestinationAddress64Bit = destinationAddress64Bit;
                    _frame.CommandOptions = CmdOptions;
                    _frame.ATCommandName = ATCommandName;
                    _frame.ATCommandData = ATCommandData;

                    frame = _frame;
                } break;

                case FrameType.ATCommand:
                {
                    ATCommandRequestFrame _frame = new ATCommandRequestFrame();
                    _frame.type = FrameType;
                    _frame.variableDataLength = ATCommandData == null ? 0 : ATCommandData.Length;
                    _frame.ATCommandName = ATCommandName;
                    _frame.ATCommandData = ATCommandData;

                    frame = _frame;
                } break;

                case FrameType.ATCommandResponse:
                {
                    ATCommandResponseFrame _frame = new ATCommandResponseFrame();
                    _frame.type = FrameType;
                    _frame.variableDataLength = ATCommandData==null ? 0 : ATCommandData.Length;
                    _frame.ATCommandName = ATCommandName;
                    _frame.ATCommandData = ATCommandData;
                    _frame.Status = commandStatus;

                    frame = _frame;
                } break;

                default:
                    break;
            }

            return frame;
        }

        public FrameBuilder setDestinationAddress16Bit(UInt16 destinationAddress)
        {
            destinationAddress16Bit = ByteOperations.littleEndianBytesFromWord(destinationAddress);
            return this;
        }
        public FrameBuilder setDestinationAddress16Bit(byte[] destinationAddress)
        {
            destinationAddress16Bit = destinationAddress;
            return this;
        }

        public FrameBuilder setDestinationAddress64Bit(UInt64 destinationAddress)
        {
            destinationAddress64Bit = ByteOperations.littleEndianBytesFromLong(destinationAddress);
            return this;
        }
        public FrameBuilder setDestinationAddress64Bit(byte[] destinationAddress)
        {
            destinationAddress64Bit = destinationAddress;
            return this;
        }

        public FrameBuilder setATCommandName(string commandName)
        {
            this.commandName = commandName;
            return this;
        }

        public FrameBuilder setATCommandData(byte[] commmandData)
        {
            this.commandData = commmandData;
            return this;
        }

        public FrameBuilder setCommandStatus(CommandStatus status)
        {
            commandStatus = status;
            return this;
        }

        // Helpers

        public FrameBuilder setBroadcastAddress()
        {
            setDestinationAddressDefaultValues();
            return this;
        }

        //! Allows for module parameter registers on a remote device to be queried or set.
        public static FrameBuilder RemoteATCommandRequest
        {
            get { return new FrameBuilder(FrameType.RemoteATCommand); }
        }

        public static FrameBuilder RemoteATCommandResponse
        {
            get { return new FrameBuilder(FrameType.RemoteCommandResponse); }
        }

        public static FrameBuilder ATCommandRequest
        {
            get { return new FrameBuilder(FrameType.ATCommand); }
        }

        public static FrameBuilder ATCommandResponse
        {
            get { return new FrameBuilder(FrameType.ATCommandResponse); }
        }

        // Private methods

        private void setDefaultValues()
        {
            switch (frameType)
            {
                case FrameType.RemoteATCommand:
                    setDestinationAddressDefaultValues();
                    break;

                default:
                    break;
            }
        }

        private void setDestinationAddressDefaultValues()
        {
            destinationAddress16Bit = new byte[2] { 0xFF, 0xFE };
            destinationAddress64Bit = new byte[8] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0xFF };
        }
    }
}
