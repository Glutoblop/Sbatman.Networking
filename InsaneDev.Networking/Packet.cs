#region Usings

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

#endregion

namespace InsaneDev.Networking
{
    public class Packet : IDisposable
    {
        /// <summary>
        /// This is the initial size of the internal byte array size of the packet
        /// </summary>
        private const int INITAL_DATA_SIZE = 128;
        /// <summary>
        /// This 4 byte sequence is used to improve start of packet regognition, it isnt the sole desciptor of the packet start as this would possibly cause issues
        /// with packets with byte transferes within them that happened to contains this sequence.
        /// </summary>
        public static readonly byte[] PacketStart = { 0, 48, 21, 0 };
        /// <summary>
        /// The current position in the internal data array
        /// </summary>
        protected int _DataPos;
        /// <summary>
        /// Whether the packet is disposed
        /// </summary>
        protected bool _Disposed;
        /// <summary>
        /// The number of paramerters that are stored in the packet
        /// </summary>
        protected Int16 _ParamCount;
        /// <summary>
        /// A temp copy of the byte array generated by this packet, this is used as a cache for packets with multiple targets
        /// </summary>
        protected byte[] _ReturnByteArray;
        /// <summary>
        /// The type id of the packet
        /// </summary>
        public readonly Int16 Type;
        /// <summary>
        /// The internal data array of the packet
        /// </summary>
        protected byte[] _Data = new byte[INITAL_DATA_SIZE];
        /// <summary>
        /// A copy of all the bojects packed in this packet
        /// </summary>
        protected List<object> _PacketObjects;

        /// <summary>
        /// Creates a new packet with the specified type id
        /// </summary>
        /// <param name="type">The packets type ID</param>
        public Packet(Int16 type)
        {
            Type = type;
        }

        /// <summary>
        /// Disposes the packet, destroying all internals buffers and caches
        /// </summary>
        public void Dispose()
        {
            _ReturnByteArray = null;
            if (_Disposed) return;
            _Disposed = true;
            if (_PacketObjects != null)
            {
                _PacketObjects.Clear();
                _PacketObjects = null;
            }
            _Data = null;
        }

        /// <summary>
        /// Creates a deep copy of this packet
        /// </summary>
        /// <returns></returns>
        public Packet Copy()
        {
            if(_Disposed) throw new ObjectDisposedException(ToString());
            Packet p = new Packet(Type)
            {
                _Data = new byte[_Data.Length]
            };
            _Data.CopyTo(p._Data, 0);
            p._DataPos = _DataPos;
            if (_PacketObjects != null) p._PacketObjects = new List<object>(_PacketObjects);
            p._ParamCount = _ParamCount;
            p._ReturnByteArray = _ReturnByteArray;
            return p;
        }

        /// <summary>
        /// Adds a double to the packet
        /// </summary>
        /// <param name="d">double to add</param>
        public void AddDouble(Double d)
        {
            if (_Disposed) throw new ObjectDisposedException(ToString());
            _ReturnByteArray = null;
            while (_DataPos + 9 >= _Data.Length) ExpandDataArray();
            _Data[_DataPos++] = (byte)ParamTypes.DOUBLE;
            BitConverter.GetBytes(d).CopyTo(_Data, _DataPos);
            _DataPos += 8;
            _ParamCount++;
        }

        /// <summary>
        /// Adds a byte array to the packet
        /// </summary>
        /// <param name="byteArray">The bytearray to add</param>
        public void AddBytePacket(byte[] byteArray)
        {
            if (_Disposed) throw new ObjectDisposedException(ToString());
            _ReturnByteArray = null;
            int size = byteArray.Length;
            while (_DataPos + (size + 5) >= _Data.Length) ExpandDataArray();
            _Data[_DataPos++] = (byte)ParamTypes.BYTE_PACKET;
            BitConverter.GetBytes(byteArray.Length).CopyTo(_Data, _DataPos);
            _DataPos += 4;
            byteArray.CopyTo(_Data, _DataPos);
            _DataPos += size;
            _ParamCount++;
        }


        /// <summary>
        /// Adds a byte array to the packet Compressed
        /// </summary>
        /// <param name="byteArray">The bytearray to add</param>
        public void AddBytePacketCompressed(byte[] byteArray)
        {
            if (_Disposed) throw new ObjectDisposedException(ToString());
            byteArray = Compress(byteArray);
            _ReturnByteArray = null;
            int size = byteArray.Length;
            while (_DataPos + (size + 5) >= _Data.Length) ExpandDataArray();
            _Data[_DataPos++] = (byte)ParamTypes.COMPRESSED_BYTE_PACKET;
            BitConverter.GetBytes(byteArray.Length).CopyTo(_Data, _DataPos);
            _DataPos += 4;
            byteArray.CopyTo(_Data, _DataPos);
            _DataPos += size;
            _ParamCount++;
        }


        /// <summary>
        /// Adds a float to the packet
        /// </summary>
        /// <param name="f">The float to add</param>
        public void AddFloat(float f)
        {
            if (_Disposed) throw new ObjectDisposedException(ToString());
            _ReturnByteArray = null;
            while (_DataPos + 5 >= _Data.Length) ExpandDataArray();
            _Data[_DataPos++] = (byte)ParamTypes.FLOAT;
            BitConverter.GetBytes(f).CopyTo(_Data, _DataPos);
            _DataPos += 4;
            _ParamCount++;
        }

        /// <summary>
        /// Adds a boolean to the packet
        /// </summary>
        /// <param name="b">The bool to add</param>
        public void AddBool(bool b)
        {
            if (_Disposed) throw new ObjectDisposedException(ToString());
            _ReturnByteArray = null;
            while (_DataPos + 5 >= _Data.Length) ExpandDataArray();
            _Data[_DataPos++] = (byte)ParamTypes.BOOL;
            BitConverter.GetBytes(b).CopyTo(_Data, _DataPos);
            _DataPos += 1;
            _ParamCount++;
        }

        /// <summary>
        /// Adds a long to the packet
        /// </summary>
        /// <param name="l">The long to add</param>
        public void AddLong(long l)
        {
            if (_Disposed) throw new ObjectDisposedException(ToString());
            _ReturnByteArray = null;
            while (_DataPos + 9 >= _Data.Length) ExpandDataArray();
            _Data[_DataPos++] = (byte)ParamTypes.LONG;
            BitConverter.GetBytes(l).CopyTo(_Data, _DataPos);
            _DataPos += 8;
            _ParamCount++;
        }

        /// <summary>
        /// Adds an int32 to the packet
        /// </summary>
        /// <param name="i">The int 32 to add</param>
        public void AddInt(Int32 i)
        {
            if (_Disposed) throw new ObjectDisposedException(ToString());
            _ReturnByteArray = null;
            while (_DataPos + 5 >= _Data.Length) ExpandDataArray();
            _Data[_DataPos++] = (byte)ParamTypes.INT32;
            BitConverter.GetBytes(i).CopyTo(_Data, _DataPos);
            _DataPos += 4;
            _ParamCount++;
        }

        /// <summary>
        /// Adds an int64 to the packet
        /// </summary>
        /// <param name="i">The int64 to add</param>
        public void AddULong(UInt64 i)
        {
            if (_Disposed) throw new ObjectDisposedException(ToString());
            _ReturnByteArray = null;
            while (_DataPos + 9 >= _Data.Length) ExpandDataArray();
            _Data[_DataPos++] = (byte)ParamTypes.ULONG;
            BitConverter.GetBytes(i).CopyTo(_Data, _DataPos);
            _DataPos += 8;
            _ParamCount++;
        }

        /// <summary>
        /// Adds an Int16 to the packet
        /// </summary>
        /// <param name="i">The int16 to add</param>
        public void AddShort(Int16 i)
        {
            if (_Disposed) throw new ObjectDisposedException(ToString());
            _ReturnByteArray = null;
            while (_DataPos + 3 >= _Data.Length) ExpandDataArray();
            _Data[_DataPos++] = (byte)ParamTypes.SHORT;
            BitConverter.GetBytes(i).CopyTo(_Data, _DataPos);
            _DataPos += 2;
            _ParamCount++;
        }

        /// <summary>
        /// Adds a Uint32 to the packet
        /// </summary>
        /// <param name="u">The uint32 to add</param>
        public void AddUInt(UInt32 u)
        {
            if (_Disposed) throw new ObjectDisposedException(ToString());
            _ReturnByteArray = null;
            while (_DataPos + 5 >= _Data.Length) ExpandDataArray();
            _Data[_DataPos++] = (byte)ParamTypes.UINT32;
            BitConverter.GetBytes(u).CopyTo(_Data, _DataPos);
            _DataPos += 4;
            _ParamCount++;
        }

        /// <summary>
        /// Adds a UTF8 String to the packet
        /// </summary>
        /// <param name="s">The String to add</param>
        public void AddString(string s)
        {
            if (_Disposed) throw new ObjectDisposedException(ToString());
            byte[] byteArray = Encoding.UTF8.GetBytes(s);
            _ReturnByteArray = null;
            int size = byteArray.Length;
            while (_DataPos + (size + 5) >= _Data.Length) ExpandDataArray();
            _Data[_DataPos++] = (byte)ParamTypes.UTF8_STRING;
            BitConverter.GetBytes(byteArray.Length).CopyTo(_Data, _DataPos);
            _DataPos += 4;
            byteArray.CopyTo(_Data, _DataPos);
            _DataPos += size;
            _ParamCount++;
        }

        /// <summary>
        /// Converts the back to a bytearray
        /// </summary>
        /// <returns></returns>
        public byte[] ToByteArray()
        {
            if (_Disposed) throw new ObjectDisposedException(ToString());
            if (_ReturnByteArray != null) return _ReturnByteArray;
            _ReturnByteArray = new byte[12 + _DataPos];
            PacketStart.CopyTo(_ReturnByteArray, 0);
            BitConverter.GetBytes(_ParamCount).CopyTo(_ReturnByteArray, 4);
            BitConverter.GetBytes(12 + _DataPos).CopyTo(_ReturnByteArray, 6);
            BitConverter.GetBytes(Type).CopyTo(_ReturnByteArray, 10);
            Array.Copy(_Data, 0, _ReturnByteArray, 12, _DataPos);
            return _ReturnByteArray;
        }

        /// <summary>
        /// Converts a byte array to a packet
        /// </summary>
        /// <param name="data">the byte array to convery</param>
        /// <returns></returns>
        public static Packet FromByteArray(byte[] data)
        {
            Packet returnPacket = new Packet(BitConverter.ToInt16(data, 10))
            {
                _ParamCount = BitConverter.ToInt16(data, 4),
                _Data = new byte[BitConverter.ToInt32(data, 6) - 12]
            };
            returnPacket._DataPos = returnPacket._Data.Length;
            Array.Copy(data, 12, returnPacket._Data, 0, returnPacket._Data.Length);
            returnPacket.UpdateObjects();
            return returnPacket;
        }

        /// <summary>
        /// Returns the list of objects within this packet
        /// </summary>
        /// <returns></returns>
        public object[] GetObjects()
        {
            return _PacketObjects.ToArray();
        }

        /// <summary>
        /// Ensures the packet bojects array correctly represents the objects that should be within this packet
        /// </summary>
        protected void UpdateObjects()
        {
            if (_Disposed) throw new ObjectDisposedException(ToString());
            if (_PacketObjects != null)
            {
                _PacketObjects.Clear();
                _PacketObjects = null;
            }
            _PacketObjects = new List<Object>(_ParamCount);
            int bytepos = 0;
            try
            {
                for (int x = 0; x < _ParamCount; x++)
                {
                    switch ((ParamTypes)_Data[bytepos++])
                    {
                        case ParamTypes.DOUBLE:
                            _PacketObjects.Add(BitConverter.ToDouble(_Data, bytepos));
                            bytepos += 8;
                            break;
                        case ParamTypes.FLOAT:
                            _PacketObjects.Add(BitConverter.ToSingle(_Data, bytepos));
                            bytepos += 4;
                            break;
                        case ParamTypes.INT32:
                            _PacketObjects.Add(BitConverter.ToInt32(_Data, bytepos));
                            bytepos += 4;
                            break;
                        case ParamTypes.BOOL:
                            _PacketObjects.Add(BitConverter.ToBoolean(_Data, bytepos));
                            bytepos += 1;
                            break;
                        case ParamTypes.LONG:
                            _PacketObjects.Add(BitConverter.ToInt64(_Data, bytepos));
                            bytepos += 8;
                            break;
                        case ParamTypes.BYTE_PACKET:
                            byte[] data = new byte[BitConverter.ToInt32(_Data, bytepos)];
                            bytepos += 4;
                            Array.Copy(_Data, bytepos, data, 0, data.Length);
                            _PacketObjects.Add(data);
                            bytepos += data.Length;
                            break;
                        case ParamTypes.UINT32:
                            _PacketObjects.Add(BitConverter.ToUInt32(_Data, bytepos));
                            bytepos += 4;
                            break;
                        case ParamTypes.ULONG:
                            _PacketObjects.Add(BitConverter.ToUInt64(_Data, bytepos));
                            bytepos += 8;
                            break;
                        case ParamTypes.SHORT:
                            _PacketObjects.Add(BitConverter.ToInt16(_Data, bytepos));
                            bytepos += 2;
                            break;
                        case ParamTypes.COMPRESSED_BYTE_PACKET:
                            byte[] data2 = new byte[BitConverter.ToInt32(_Data, bytepos)];
                            bytepos += 4;
                            Array.Copy(_Data, bytepos, data2, 0, data2.Length);
                            _PacketObjects.Add(Uncompress(data2));
                            bytepos += data2.Length;
                            break;
                        case ParamTypes.UTF8_STRING:
                            byte[] data3 = new byte[BitConverter.ToInt32(_Data, bytepos)];
                            bytepos += 4;
                            Array.Copy(_Data, bytepos, data3, 0, data3.Length);
                            _PacketObjects.Add(Encoding.UTF8.GetString(data3));
                            bytepos += data3.Length;
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        /// <summary>
        /// Increases the size of the internal data array
        /// </summary>
        protected void ExpandDataArray()
        {
            _ReturnByteArray = null;
            byte[] newData = new byte[_Data.Length * 2];
            _Data.CopyTo(newData, 0);
            _Data = newData;
        }

        /// <summary>
        /// An enum containing supported types
        /// </summary>
        protected enum ParamTypes
        {
            DOUBLE,
            FLOAT,
            INT32,
            BOOL,
            BYTE_PACKET,
            LONG,
            UINT32,
            ULONG,
            SHORT,
            COMPRESSED_BYTE_PACKET,
            UTF8_STRING,
        };

        /// <summary>
        /// Compress the specified bytes.
        /// </summary>
        /// <param name='bytes'>
        /// Bytes.
        /// </param>
        public static byte[] Compress(byte[] bytes)
        {
            using (var ms = new MemoryStream())
            {
                using (var ds = new DeflateStream(ms, CompressionMode.Compress))
                {
                    ds.Write(bytes, 0, bytes.Length);
                }
                return ms.ToArray();
            }
        }

        /// <summary>
        /// Uncompress the specified bytes.
        /// </summary>
        /// <param name='bytes'>
        /// Bytes.
        /// </param>
        public static byte[] Uncompress(byte[] bytes)
        {
            using (var ds = new DeflateStream(new MemoryStream(bytes), CompressionMode.Decompress))
            {
                using (var ms = new MemoryStream())
                {
                    ds.CopyTo(ms);
                    return ms.ToArray();
                }
            }
        }
    }
}