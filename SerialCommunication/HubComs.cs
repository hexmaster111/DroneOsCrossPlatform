using System.IO.Ports;
using System.Runtime.InteropServices;

namespace SerialCommunication
{
    public struct DataFraim
    {
        public byte HeaderHigh;
        public byte HeaderLow;
        public byte CommandId;
        public byte Address;
        public byte Arg1High;
        public byte Arg1Low;
        public byte Arg2High;
        public byte Arg2Low;
        public byte CrcHigh;
        public byte CrcLow;
    }

    public enum ConnectionStatus
    {
        Connected,
        Disconnected,
    }

    public enum ConnectionError
    {
        PortUnavalable,
        PortDisconnected,
        PortInUse,
    }


    public class HubComs
    {
        public bool IsConnected { get; private set; }

        /// <summary>
        /// Thrown when a new datafraim is read in
        /// This data fraim is unchecked
        /// </summary>
        public Action<DataFraim> OnNewData;

        /// <summary>
        /// Thrown when there is a error connecting
        /// </summary>
        public Action<ConnectionError> OnNewConnectionError;

        /// <summary>
        /// Thrown on connect or disconnect
        /// </summary>
        public Action<ConnectionStatus> OnNewConnectionStatus;

        /// <summary>
        /// Thrown on sending a data fraim
        /// </summary>
        public Action<DataFraim> OnDataTransmit;

        //Event thrown when a Nack is sent
        public Action<DataFraim> OnSendNack;

        //Event thrown when a Ack is sent
        public Action<DataFraim> OnSendAck;

        //Event thrown when a HB is sent
        public Action<DataFraim> OnSendHb;

        //Event thrown when a message is added or removed from the queue
        //int is the number of messages in the queue
        public Action<int> OnMessageQueueCountChanged;

        private SerialPort _serialPort;

        private bool _comThreadsContinue = false;

        private Queue<DataFraim> _dataFraimQueue = new Queue<DataFraim>();

        public int MessageQueueCount
        {
            get { return _dataFraimQueue.Count; }
        }

        /// <summary>
        /// Gets connection status from the com thread 
        /// </summary>
        /// <returns>Connection status</returns>
        public ConnectionStatus GetConnectionStatus()
        {
            if (_comThreadsContinue)
                return ConnectionStatus.Connected;
            else
                return ConnectionStatus.Disconnected;
        }


        public HubComs()
        {
        }

        /// <summary>
        /// Stops read and write threads, and closes port.
        /// </summary>
        public void Close()
        {
            _comThreadsContinue = false;
            _serialPort.Close();
        }


        public void Open(string port, int baud)
        {
            Thread writeThread = new(_serialWriteThread);
            Thread hbThread = new(HubHeartBeatThread);
            Thread readThread = new(_serialReadThread);


            _serialPort = new SerialPort();

            _serialPort.PortName = port;
            _serialPort.BaudRate = baud;
            _serialPort.Parity = Parity.None;
            _serialPort.StopBits = StopBits.One;
            _serialPort.Handshake = Handshake.None; //is this flow control? yes. it is.

            _serialPort.ReadTimeout = 20;
            _serialPort.WriteTimeout = 20;


            try
            {
                _serialPort.Open();
            }
            catch (System.UnauthorizedAccessException e)
            {
                //port unavalable
                OnNewConnectionError?.Invoke(ConnectionError.PortInUse);
                IsConnected = false;
                return;
            }
            catch (System.IO.FileNotFoundException e)
            {
                //port not avalable
                OnNewConnectionError?.Invoke(ConnectionError.PortUnavalable);
                IsConnected = false;
                return;
            }

            _comThreadsContinue = true;
            writeThread.Start();
            hbThread.Start();
            readThread.Start();
            OnNewConnectionStatus?.Invoke(ConnectionStatus.Connected);
            IsConnected = true;
        }


        byte[] StructureToByteArray(object obj)
        {
            int len = Marshal.SizeOf(obj);

            byte[] arr = new byte[len];

            IntPtr ptr = Marshal.AllocHGlobal(len);

            Marshal.StructureToPtr(obj, ptr, true);

            Marshal.Copy(ptr, arr, 0, len);

            Marshal.FreeHGlobal(ptr);

            return arr;
        }

        void ByteArrayToStructure(byte[] bytearray, ref object obj)
        {
            int len = Marshal.SizeOf(obj);

            IntPtr i = Marshal.AllocHGlobal(len);

            Marshal.Copy(bytearray, 0, i, len);

            obj = Marshal.PtrToStructure(i, obj.GetType());

            Marshal.FreeHGlobal(i);
        }

        //from https://stackoverflow.com/questions/42038108/crc-16-calculation-c-sharp
        private ushort CalculateCRC(byte[] data, int len)
        {
            int crc = 0, i = 0;
            while (len-- != 0)
            {
                //Debug.WriteLine("{0}", data[i]);
                crc ^= data[i++] << 8;
                for (int k = 0; k < 8; k++)
                    crc = ((crc & 0x8000) != 0) ? (crc << 1) ^ 0x8005 : (crc << 1);
            }

            return (ushort)(crc & 0xffff);
        }


        public DataFraim CalculateCrc(DataFraim crclessDataFraim)
        {
            DataFraim o = crclessDataFraim;

            byte[] calcBytes = StructureToByteArray(o);
            ushort crc = CalculateCRC(calcBytes, calcBytes.Length - 2); //-2 for the two 0 crc bytes

            o.CrcHigh = (byte)(crc >> 8);
            o.CrcLow = (byte)(crc >> 0);

            return o;
        }

        public enum ControllerCommands
        {
            GetVal = 0x01,
            SetVal = 0x02,
            EnableHeartBeat = 0x03,
            HearBeat = 0x04,
            AddNotify = 0x05,
            ClearNotify = 0x06,
            GetConnectedDevices = 0x07,
            UiConnecting = 0x08,
            UiDisconnecting = 0x09,
            Nack = 0xB2,
            Ack = 0xB1,
        }


        public DataFraim BuildRawMessage(ControllerCommands commandId, byte targetAddress = 0, byte arg1H = 0,
            byte arg1L = 0,
            byte arg2H = 0, byte arg2L = 0)
        {
            DataFraim o = new()
            {
                HeaderHigh = 0x12,
                HeaderLow = 0x34,
                Address = targetAddress,
                CommandId = (byte)commandId,
                Arg1High = arg1H,
                Arg1Low = arg1L,
                Arg2High = arg2H,
                Arg2Low = arg2L
            };

            return CalculateCrc(o);
        }

        public void WriteRaw(DataFraim msg)
        {
            _serialPort.Write(StructureToByteArray(msg), 0, Marshal.SizeOf(msg));
            OnDataTransmit?.Invoke(msg);
        }

        public void QueueMessage(DataFraim msgDataFraim)
        {
            //potentlay do some basic message format checking
            _dataFraimQueue.Enqueue(msgDataFraim);
            OnMessageQueueCountChanged?.Invoke(_dataFraimQueue.Count);
        }

        public void DequeueMessage()
        {
            if (_dataFraimQueue.Count > 0)
            {
                _dataFraimQueue.Dequeue();
                OnMessageQueueCountChanged?.Invoke(_dataFraimQueue.Count);
            }
        }

        private bool _nextMessageOkToSend = true;


        /// <summary>
        /// Set to true to retry sending last message
        /// </summary>
        public bool RetryLastMessage
        {
            set
            {
                if (value)
                    _nextMessageOkToSend = true;
            }
        }

        /// <summary>
        /// Set to true to deque and send next message
        /// </summary>
        public bool NextMessageOkToSend
        {
            get => _nextMessageOkToSend;
            set
            {
                if (value)
                    DequeueMessage();
                _nextMessageOkToSend = value;
            }
        }

        private bool _sendAck = false;
        private bool _sendNack = false;


        public bool SendAck
        {
            get => _sendAck;
            set
            {
                if (value)
                    _sendAck = true;
            }
        }

        public bool SendNack
        {
            get => _sendNack;
            set
            {
                if (value)
                    _sendNack = true;
            }
        }

        private bool _sendHb = false;

        public bool SendHb
        {
            get => _sendHb;
            set
            {
                if (value)
                    _sendHb = true;
            }
        }

        private void HubHeartBeatThread()
        {
            while (_comThreadsContinue)
            {
                SendHb = true;
                Thread.Sleep(1000);
            }
        }

        private void _serialWriteThread()
        {
            bool lastMessageNotAllowedToSend = false;


            long msgId = 0;
            while (_comThreadsContinue)
            {
                {
                    //Handle sending messages right away
                    if (_sendAck)
                    {
                        DataFraim ack = BuildRawMessage(ControllerCommands.Ack);
                        _serialPort.Write(StructureToByteArray(ack), 0, Marshal.SizeOf(ack));
                        _sendAck = false;
                        OnSendAck?.Invoke(ack);
                        OnDataTransmit?.Invoke(ack);
                    }

                    if (_sendNack)
                    {
                        DataFraim nack = BuildRawMessage(ControllerCommands.Nack);
                        _serialPort.Write(StructureToByteArray(nack), 0, Marshal.SizeOf(nack));
                        _sendNack = false;
                        OnSendNack?.Invoke(nack);
                        OnDataTransmit?.Invoke(nack);
                    }

                    if (SendHb)
                    {
                        DataFraim hb = BuildRawMessage(ControllerCommands.HearBeat);
                        _serialPort.Write(StructureToByteArray(hb), 0, Marshal.SizeOf(hb));
                        _sendHb = false;
                        OnSendHb?.Invoke(hb);
                        OnDataTransmit?.Invoke(hb);
                    }
                }


                //write
                DataFraim toSend;

                if (_dataFraimQueue.TryPeek(out toSend))
                {
                    //if message is in queue
                    if (NextMessageOkToSend || lastMessageNotAllowedToSend) //if message is ok to send
                    {
                        byte[] toWrite = StructureToByteArray(toSend);
                        bool didSend = false;
                        try
                        {
                            _serialPort.Write(toWrite, 0, toWrite.Length);
                            didSend = true;
                        }
                        catch (TimeoutException e)
                        {
                            Console.WriteLine("Faild To write:" + e);
                            didSend = false;
                            _comThreadsContinue = false;
                            OnNewConnectionError?.Invoke(ConnectionError.PortDisconnected);
                        }

                        {
                            if (didSend)
                            {
                                OnDataTransmit?.Invoke(toSend);
                                NextMessageOkToSend = false;
                                lastMessageNotAllowedToSend = false;
                                Thread.Sleep(100);
                            }
                        }
                    }
                    else
                    {
                        lastMessageNotAllowedToSend = true;
                    }
                }

                Thread.Sleep(10);
            }

            OnNewConnectionStatus?.Invoke(ConnectionStatus.Disconnected);
            IsConnected = false;
        }


        private Queue<byte> __bytesToBeProcessed = new();

        private void _serialReadThread()
        {
            while (_comThreadsContinue)
            {
                //Enqueue all bytes in the buffer
                while (_serialPort.BytesToRead > 0)
                {
                    __bytesToBeProcessed.Enqueue((byte)_serialPort.ReadByte());
                }


                // If we have enough bytes available for a message to exist...
                while (__bytesToBeProcessed.Count >= 10)
                {
                    // ...Check if our first byte is maybe the start of a frame
                    // Note: We throw this away if it's not, because either way we need to advance our scan window.
                    byte currByte = __bytesToBeProcessed.Dequeue();
                    if (currByte != 0x12)
                    {
                        // Nope! Just wait until the next potential time we have a message
                        continue;
                    }

                    // Check the second - don't throw it away, in case it's actually the start of a header.
                    currByte = __bytesToBeProcessed.Peek();
                    if (currByte != 0x34)
                    {
                        // Nope! Try again next time, see if this is the first byte on next iteration
                        continue;
                    }

                    // We have a valid header! Chuck out the rest of the header and start pulling in the message.
                    __bytesToBeProcessed.Dequeue();
                    //currByte.Dequeue();

                    //Array<byte> frame = new Array(8);
                    byte[] frame = new byte[8];
                    for (var i = 0; i < 8; i++)
                    {
                        frame[i] = __bytesToBeProcessed.Dequeue();
                    }

                    DataFraim outputDataFraim = new()
                    {
                        HeaderHigh = 0x12, //Held onto to preform the crc check in the next step
                        HeaderLow = 0x34, //Held onto to preform the crc check in the next step
                        CommandId = frame[0],
                        Address = frame[1],
                        Arg1High = frame[2],
                        Arg1Low = frame[3],
                        Arg2High = frame[4],
                        Arg2Low = frame[5],
                        CrcHigh = frame[6],
                        CrcLow = frame[7],
                    };

                    // TODO: Implement CRC check before you pass the message on to the OnNewData action.

                    // TODO: Impliment this somehow...
                    // AddonResponceHanlder.ResponceData responceData = new AddonResponceHanlder.ResponceData()
                    // {
                    //     Address = frame[1],
                    //     ArgumentOneHigh = frame[2],
                    //     ArgumentOneLow = frame[3],
                    //     ArgumentTwoHigh = frame[4],
                    //     ArguementTwoLow = frame[5],
                    // };

                    byte[] recevedCrc = new[]
                    {
                        frame[6],
                        frame[7],
                    };

                    //Check that the fraim is valid
                    DataFraim calcedMsg = CalculateCrc(outputDataFraim);


                    if ((recevedCrc[0] != calcedMsg.CrcHigh) ||
                        recevedCrc[1] != calcedMsg.CrcLow)
                    {
                        //Crc is not valid
                        SendNack = true;
                        return;
                    }
                    else
                    {
                        SendAck = true;
                    }


                    //Packege up and thorw our event
                    OnNewData?.Invoke(outputDataFraim);
                    NextMessageOkToSend = true;
                }
            }
        }
    }
}