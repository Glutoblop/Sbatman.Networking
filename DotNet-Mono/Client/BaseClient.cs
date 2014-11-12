#region Usings

using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;

#endregion

namespace Sbatman.Networking.Client
{
    /// <summary>
    ///     A Base class of a client connection. This can be used to connect to the specified server. All message handeling is
    ///     performed ASynchronously
    /// </summary>
    public class BaseClient
    {
        /// <summary>
        ///     Queue containing un processed packets
        /// </summary>
        protected readonly Queue<Packet> _PacketsToProcess = new Queue<Packet>();

        /// <summary>
        ///     List of packets that have yet to be sent
        /// </summary>
        protected readonly List<Packet> _PacketsToSend = new List<Packet>();

        /// <summary>
        ///     The buffer size allocated to this client
        /// </summary>
        protected int _BufferSize;

        /// <summary>
        ///     Buffer of butes used to collect incomming packets and putt hem together
        /// </summary>
        protected byte[] _ByteBuffer;

        /// <summary>
        ///     Current point in the byte buffer to use for new data
        /// </summary>
        protected int _ByteBufferCount;

        /// <summary>
        ///     The TCP socket the client is connected on
        /// </summary>
        protected TcpClient _ClientSocket = new TcpClient();

        /// <summary>
        ///     Bool identifing if the client is currently connected or not
        /// </summary>
        protected bool _Connected;

        /// <summary>
        ///     Set true in the event of an error
        /// </summary>
        protected bool _Error;

        /// <summary>
        ///     Last internal error message
        /// </summary>
        protected string _ErrorMessage;

        /// <summary>
        ///     The interval in MS packets are checked for
        /// </summary>
        protected int _PacketCheckInterval = 2;

        /// <summary>
        ///     The thread used for handeling packets
        /// </summary>
        protected Thread _PacketHandel;

        /// <summary>
        ///     Initialise a connection to the speicified adress and port
        /// </summary>
        /// <param name="serverAddress"> Adress of server to attempt to connect to </param>
        /// <param name="port">The port over which to connect</param>
        /// <param name="bufferSize">The size in bytes of the internal store for recieved but unprocessed packets</param>
        public bool Connect(String serverAddress, int port, int bufferSize = 50000)
        {
            _BufferSize = 50000;
            _ErrorMessage = "";
            _Error = false;
            if (_ByteBuffer == null)
            {
                _ByteBuffer = new byte[_BufferSize];
            }
            else
            {
                for (int i = 0; i < _BufferSize; i++) _ByteBuffer[i] = 0;
            }
            try
            {
                if (_ClientSocket == null)
                {
                    _ClientSocket = new TcpClient(serverAddress, port);
                    _ClientSocket.NoDelay = true;
                }
                else
                {
                    _ClientSocket.Connect(serverAddress, port);
                }
                if (_ClientSocket.Connected)
                {
                    _Connected = true;
                    if (_PacketHandel != null)
                    {
                        _PacketHandel.Abort();
                        _PacketHandel = null;
                    }
                    _PacketHandel = new Thread(Update);
                    _PacketHandel.Start();
                    return true;
                }
            }
            catch
            {
                Console.WriteLine("NerfCorev2:Networking - Failure to connect to " + serverAddress + " on port " + port);
            }

            return false;
        }

        /// <summary>
        ///     Changes the number of milliseconds between packet checks (this shouldnt be higer then 4ms for timely responces, or
        ///     lower then 1ms to prevent high cpu usage
        /// </summary>
        /// <param name="timeBetweenChecksInMs"> Number of ms between checks </param>
        public void SetPacketCheckInterval(int timeBetweenChecksInMs)
        {
            if (timeBetweenChecksInMs <= 0) timeBetweenChecksInMs = 1;
            _PacketCheckInterval = timeBetweenChecksInMs;
        }

        /// <summary>
        /// Returns a bool representing whether no delay has been foced to true. This disables an underlying packet bunching logic
        /// decreasing avaliable bandwidth in exchange for quicker response times.
        /// </summary>
        /// <returns></returns>
        public bool GetForceNoDelay()
        {
            return _ClientSocket.NoDelay;
        }

        /// <summary>
        /// sets whether no delay has been foced to true. This disables an underlying packet bunching logic
        /// decreasing avaliable bandwidth in exchange for quicker response times. Default = true
        /// </summary>
        /// <param name="setting">True for speed over bandwidth, false for bandwidth over speed</param>
        public void SetForceNoDelay(bool setting)
        {
            _ClientSocket.NoDelay = setting;
        }

        /// <summary>
        ///     Retruns a list of all the packs that have come in and need to be processed. This clears the list.
        /// </summary>
        /// <returns> </returns>
        public Packet[] GetPacketsToProcess()
        {
            Packet[] packets;
            lock (_PacketsToProcess)
            {
                int count = _PacketsToProcess.Count;
                packets = new Packet[count];
                for (int x = 0; x < count; x++)
                {
                    packets[x] = _PacketsToProcess.Dequeue();
                }
            }
            return packets;
        }

        /// <summary>
        ///     Injects packets into the process list as though they had been recieved over the network, great for debugging
        ///     and for local server/client combo's
        /// </summary>
        /// <param name="p">The packet to inject</param>
        public void InjectToPacketsToProcess(Packet p)
        {
            lock (_PacketsToProcess)
            {
                _PacketsToProcess.Enqueue(p);
            }
        }

        /// <summary>
        ///     Retruns an int containing the number of waiting prackets
        /// </summary>
        /// <returns> </returns>
        public int GetPacketsToProcessCount()
        {
            return _PacketsToProcess.Count;
        }

        /// <summary>
        ///     Retruns an int containing the number of packets that have not yet been sent
        /// </summary>
        /// <returns> </returns>
        public int GetPacketsToSendCount()
        {
            return _PacketsToSend.Count;
        }

        /// <summary>
        ///     Sends a packet to the connected server, and diposes the packet once sent.
        /// </summary>
        /// <param name="packet"> Packet to send </param>
        public virtual void SendPacket(Packet packet)
        {
            lock (_PacketsToSend)
            {
                _PacketsToSend.Add(packet);
            }
        }

        /// <summary>
        ///     Send multiple packets to the connected server
        /// </summary>
        /// <param name="packets"> List of packets to send</param>
        public virtual void SendPacket(IEnumerable<Packet> packets)
        {
            lock (_PacketsToSend)
            {
                _PacketsToSend.AddRange(packets);
            }
        }

        /// <summary>
        ///     Returns true of your connected to the server
        /// </summary>
        /// <returns> </returns>
        public virtual bool IsConnected()
        {
            return _ClientSocket != null && _ClientSocket.Connected;
        }

        /// <summary>
        ///     Disconnect from the server
        /// </summary>
        public void Disconnect()
        {
            _Connected = false;
        }

        private void Update()
        {
            try
            {
                while (_Connected)
                {
                    List<Packet> templist = new List<Packet>();
                    lock (_PacketsToSend)
                    {
                        templist.AddRange(_PacketsToSend);
                        _PacketsToSend.Clear();
                    }

                    if (templist.Count > 0)
                    {
                        lock (_ClientSocket)
                        {
                            NetworkStream netStream = new NetworkStream(_ClientSocket.Client);
                            foreach (Packet packet in templist)
                            {
                                byte[] data = packet.ToByteArray();
                                netStream.Write(data, 0, data.Length);
                                packet.Dispose();
                            }
                            netStream.Close();
                        }
                    }
                    templist.Clear();
                    lock (_ClientSocket)
                    {
                        if (_ClientSocket.Available > 0)
                        {
                            byte[] datapulled = new byte[_ClientSocket.Available];
                            _ClientSocket.GetStream().Read(datapulled, 0, datapulled.Length);
                            Array.Copy(datapulled, 0, _ByteBuffer, _ByteBufferCount, datapulled.Length);
                            _ByteBufferCount += datapulled.Length;
                        }
                    }
                    bool finding = _ByteBufferCount > 11;
                    while (finding)
                    {
                        bool packerstartpresent = true;
                        for (int x = 0; x < 4; x++)
                        {
                            if (_ByteBuffer[x] == Packet.PacketStart[x]) continue;
                            packerstartpresent = false;
                            break;
                        }
                        if (packerstartpresent)
                        {
                            int size = BitConverter.ToInt32(_ByteBuffer, 6);
                            if (_ByteBufferCount >= size)
                            {
                                byte[] packet = new byte[size];
                                Array.Copy(_ByteBuffer, 0, packet, 0, size);
                                Array.Copy(_ByteBuffer, size, _ByteBuffer, 0, _ByteBufferCount - size);
                                _ByteBufferCount -= size;
                                Packet p = Packet.FromByteArray(packet);
                                if (p != null) _PacketsToProcess.Enqueue(p);
                            }
                            else
                            {
                                finding = false;
                            }
                        }
                        else
                        {
                            int offset = -1;
                            for (int x = 0; x < _ByteBufferCount; x++)
                            {
                                if (_ByteBuffer[x] == Packet.PacketStart[x]) offset = x;
                            }
                            if (offset != -1)
                            {
                                Array.Copy(_ByteBuffer, offset, _ByteBuffer, 0, _ByteBufferCount - offset);
                                _ByteBufferCount -= offset;
                            }
                            else
                            {
                                _ByteBufferCount = 0;
                            }
                        }
                        if (_ByteBufferCount < 12) finding = false;
                    }
                    lock (_PacketsToProcess)
                    {
                        foreach (Packet p in templist) _PacketsToProcess.Enqueue(p);
                    }
                    templist.Clear();
                    Thread.Sleep(_PacketCheckInterval);
                }
            }
            catch (Exception e)
            {
                _Error = true;
                _ErrorMessage = e.Message;
            }

            if (_ClientSocket != null)
            {
                if (_ClientSocket.Connected) _ClientSocket.Close();
                _ClientSocket = null;
            }
            _Connected = false;
        }

        /// <summary>
        ///     Returns true if an internal error has occured. This can be retrived with GetError.
        /// </summary>
        /// <returns></returns>
        public bool HasErrored()
        {
            return _Error;
        }

        /// <summary>
        ///     Returns the message string of the last error and resets the has error to false
        /// </summary>
        /// <returns></returns>
        public string GetError()
        {
            _Error = false;
            return _ErrorMessage;
        }

        /// <summary>
        ///     Retruns the internal TCP socket used by this client. Lock the TCPClient when in use to maintain thread safe
        ///     operations
        /// </summary>
        /// <returns></returns>
        public TcpClient GetInternalSocket()
        {
            return _ClientSocket;
        }

        /// <summary>
        ///     Gets the size of the internal buffer array that stores incoming but unhandled packets.
        /// </summary>
        /// <returns>
        ///     The internal buffer size in bytes.
        /// </returns>
        public int GetInternalBufferSize()
        {
            return _BufferSize;
        }
    }
}