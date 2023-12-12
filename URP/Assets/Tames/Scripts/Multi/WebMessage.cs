using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Multi
{
   
        public enum MessageHeader : byte
        {
            /// <summary>An unreliable user message.</summary>
            Unreliable,
            /// <summary>An internal unreliable ack message.</summary>
            Ack,
            /// <summary>An internal unreliable connect message.</summary>
            Connect,
            /// <summary>An internal unreliable connection rejection message.</summary>
            Reject,
            /// <summary>An internal unreliable heartbeat message.</summary>
            Heartbeat,
            /// <summary>An internal unreliable disconnect message.</summary>
            Disconnect,

            /// <summary>A notify message.</summary>
            Notify,

            /// <summary>A reliable user message.</summary>
            Reliable,
            /// <summary>An internal reliable welcome message.</summary>
            Welcome,
            /// <summary>An internal reliable client connected message.</summary>
            ClientConnected,
            /// <summary>An internal reliable client disconnected message.</summary>
            ClientDisconnected,
        }
        /// <summary>The send mode of a <see cref="WebMessage"/>.</summary>
      
        /// <summary>Provides functionality for converting data to bytes and vice versa.</summary>
        public class WebMessage
        {
            /// <summary>The maximum number of bits required for a message's header.</summary>
            public const int MaxHeaderSize = NotifyHeaderBits;
            /// <summary>The number of bits used by the <see cref="MessageHeader"/>.</summary>
            internal const int HeaderBits = 4;
            /// <summary>A bitmask that, when applied, only keeps the bits corresponding to the <see cref="MessageHeader"/> value.</summary>
            internal const byte HeaderBitmask = (1 << HeaderBits) - 1;
            /// <summary>The header size for unreliable messages. Does not count the 2 bytes used for the message ID.</summary>
            /// <remarks>4 bits - header.</remarks>
            internal const int UnreliableHeaderBits = HeaderBits;
            /// <summary>The header size for reliable messages. Does not count the 2 bytes used for the message ID.</summary>
            /// <remarks>4 bits - header, 16 bits - sequence ID.</remarks>
            internal const int ReliableHeaderBits = HeaderBits + 2 * BitsPerByte;
            /// <summary>The header size for notify messages.</summary>
            /// <remarks>4 bits - header, 24 bits - ack, 16 bits - sequence ID.</remarks>
            internal const int NotifyHeaderBits = HeaderBits + 5 * BitsPerByte;
            /// <summary>The minimum number of bytes contained in an unreliable message.</summary>
            internal const int MinUnreliableBytes = UnreliableHeaderBits / BitsPerByte + (UnreliableHeaderBits % BitsPerByte == 0 ? 0 : 1);
            /// <summary>The minimum number of bytes contained in a reliable message.</summary>
            internal const int MinReliableBytes = ReliableHeaderBits / BitsPerByte + (ReliableHeaderBits % BitsPerByte == 0 ? 0 : 1);
            /// <summary>The minimum number of bytes contained in a notify message.</summary>
            internal const int MinNotifyBytes = NotifyHeaderBits / BitsPerByte + (NotifyHeaderBits % BitsPerByte == 0 ? 0 : 1);
            /// <summary>The number of bits in a byte.</summary>
            private const int BitsPerByte = Converter.BitsPerByte;
            /// <summary>The number of bits in each data segment.</summary>
            private const int BitsPerSegment = Converter.BitsPerULong;

            /// <summary>The maximum number of bytes that a message can contain, including the <see cref="MaxHeaderSize"/>.</summary>
            public static int MaxSize { get; private set; }
            /// <summary>The maximum number of bytes of payload data that a message can contain. This value represents how many bytes can be added to a message <i>on top of</i> the <see cref="MaxHeaderSize"/>.</summary>

            /// <summary>An intermediary buffer to help convert <see cref="data"/> to a byte array when sending.</summary>
            internal static byte[] ByteBuffer;
            /// <summary>The maximum number of bits a message can contain.</summary>
            private static int maxBitCount;
            /// <summary>The maximum size of the <see cref="data"/> array.</summary>
            private static int maxArraySize;

            /// <summary>How many messages to add to the pool for each <see cref="Server"/> or <see cref="Client"/> instance that is started.</summary>
            /// <remarks>Changes will not affect <see cref="Server"/> and <see cref="Client"/> instances which are already running until they are restarted.</remarks>
            public static byte InstancesPerPeer { get; set; } = 4;
            /// <summary>A pool of reusable message instances.</summary>
            private static readonly List<WebMessage> pool = new List<WebMessage>(InstancesPerPeer * 2);

            static WebMessage()
            {
                MaxSize = MaxHeaderSize / BitsPerByte + (MaxHeaderSize % BitsPerByte == 0 ? 0 : 1) + 1225;
                maxBitCount = MaxSize * BitsPerByte;
                maxArraySize = MaxSize / sizeof(ulong) + (MaxSize % sizeof(ulong) == 0 ? 0 : 1);
                ByteBuffer = new byte[MaxSize];
            }

            /// <summary>The message's send mode.</summary>
            public RiptideNetworking. MessageSendMode SendMode { get; private set; }
            /// <summary>How many bits have been retrieved from the message.</summary>
            public int ReadBits => readBit;
            /// <summary>How many unretrieved bits remain in the message.</summary>
            public int UnreadBits => writeBit - readBit;
            /// <summary>How many bits have been added to the message.</summary>
            public int WrittenBits => writeBit;
            /// <summary>How many more bits can be added to the message.</summary>
            public int UnwrittenBits => maxBitCount - writeBit;
            /// <summary>How many of this message's bytes are in use. Rounds up to the next byte because only whole bytes can be sent.</summary>
            public int BytesInUse => writeBit / BitsPerByte + (writeBit % BitsPerByte == 0 ? 0 : 1);
            /// <summary>How many bytes have been retrieved from the message.</summary>
            [Obsolete("Use ReadBits instead.")] public int ReadLength => ReadBits / BitsPerByte + (ReadBits % BitsPerByte == 0 ? 0 : 1);
            /// <summary>How many more bytes can be retrieved from the message.</summary>
            [Obsolete("Use UnreadBits instead.")] public int UnreadLength => UnreadBits / BitsPerByte + (UnreadBits % BitsPerByte == 0 ? 0 : 1);
            /// <summary>How many bytes have been added to the message.</summary>
            [Obsolete("Use WrittenBits instead.")] public int WrittenLength => WrittenBits / BitsPerByte + (WrittenBits % BitsPerByte == 0 ? 0 : 1);
            /// <inheritdoc cref="data"/>
            private ulong[] Data => data;
            public byte[] ByteData { get { return GetByteData(); } }
            /// <summary>The message's data.</summary>
            private readonly ulong[] data;
            /// <summary>The next bit to be read.</summary>
            private int readBit;
            /// <summary>The next bit to be written.</summary>
            private int writeBit;

            /// <summary>Initializes a reusable <see cref="WebMessage"/> instance.</summary>
            private WebMessage() => data = new ulong[maxArraySize];

            /// <summary>Gets a completely empty message instance with no header.</summary>
            /// <returns>An empty message instance.</returns>

            /// <summary>Gets a message instance that can be used for sending.</summary>
            /// <param name="sendMode">The mode in which the message should be sent.</param>
            /// <param name="id">The message ID.</param>
            /// <returns>A message instance ready to be sent.</returns>
            public static WebMessage Create(RiptideNetworking.MessageSendMode sendMode, ushort id)
            {
                return new WebMessage().Init((MessageHeader)sendMode).AddVarULong(id);
            }
            private WebMessage Init(MessageHeader header)
            {
                data[0] = (byte)header;
                SetHeader(header);
                return this;
            }
            /// <summary>Sets the message's header bits to the given <paramref name="header"/> and determines the appropriate <see cref="MessageSendMode"/> and read/write positions.</summary>
            /// <param name="header">The header to use for this message.</param>
            private void SetHeader(MessageHeader header)
            {                
                 if (header >= MessageHeader.Reliable)
                {
                    readBit = ReliableHeaderBits;
                    writeBit = ReliableHeaderBits;
                    SendMode = RiptideNetworking.MessageSendMode.reliable;
                }
                else
                {
                    readBit = UnreliableHeaderBits;
                    writeBit = UnreliableHeaderBits;
                    SendMode = RiptideNetworking.MessageSendMode.unreliable;
                }
            }

            /// <summary>Adds up to 8 of the given bits to the message.</summary>
            /// <param name="bitfield">The bits to add.</param>
            /// <param name="amount">The number of bits to add.</param>
            /// <returns>The message that the bits were added to.</returns>

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public WebMessage AddVarLong(long value) => AddVarULong((ulong)Converter.ZigZagEncode(value));
            /// <summary>Adds a positive number to the message, using fewer bits for smaller values.</summary>
            /// <param name="value">The value to add.</param>
            /// <returns>The message that the value was added to.</returns>
            /// <remarks>The value is added in segments of 8 bits, 1 of which is used to indicate whether or not another segment follows. As a result, small values are
            /// added to the message using fewer bits, while large values will require a few more bits than they would if they were added via <see cref="AddByte(byte)"/>,
            /// <see cref="AddUShort(ushort)"/>, <see cref="AddUInt"/>, or <see cref="AddULong(ulong)"/> (or their signed counterparts).</remarks>
            public WebMessage AddVarULong(ulong value)
            {
                do
                {
                    byte byteValue = (byte)(value & 0b_0111_1111);
                    value >>= 7;
                    if (value != 0) // There's more to write
                        byteValue |= 0b_1000_0000;

                    AddByte(byteValue);
                }
                while (value != 0);

                return this;
            }

            /// <summary>Adds a <see cref="byte"/> to the message.</summary>
            /// <param name="value">The <see cref="byte"/> to add.</param>
            /// <returns>The message that the <see cref="byte"/> was added to.</returns>
            public WebMessage AddByte(byte value)
            {
                Converter.ByteToBits(value, data, writeBit);
                writeBit += BitsPerByte;
                return this;
            }

            /// <summary>Adds an <see cref="sbyte"/> to the message.</summary>
            /// <param name="value">The <see cref="sbyte"/> to add.</param>
            /// <returns>The message that the <see cref="sbyte"/> was added to.</returns>

            /// <summary>Retrieves a <see cref="byte"/> from the message.</summary>
            /// <returns>The <see cref="byte"/> that was retrieved.</returns>


            /// <summary>Retrieves an <see cref="sbyte"/> from the message.</summary>
            /// <returns>The <see cref="sbyte"/> that was retrieved.</returns>


            /// <summary>Adds a <see cref="byte"/> array to the message.</summary>
            /// <param name="array">The array to add.</param>
            /// <param name="includeLength">Whether or not to include the length of the array in the message.</param>
            /// <returns>The message that the array was added to.</returns>
            public WebMessage AddBytes(byte[] array, bool includeLength = true)
            {
                if (includeLength)
                    AddVarULong((uint)array.Length);


                if (writeBit % BitsPerByte == 0)
                {
                    Buffer.BlockCopy(array, 0, data, writeBit / BitsPerByte, array.Length);
                    writeBit += array.Length * BitsPerByte;
                }
                else
                {
                    for (int i = 0; i < array.Length; i++)
                    {
                        Converter.ByteToBits(array[i], data, writeBit);
                        writeBit += BitsPerByte;
                    }
                }

                return this;
            }
            public WebMessage AddString(string value)
            {
                AddBytes(Encoding.UTF8.GetBytes(value));
                return this;
            }

            public static void FromUInt(uint value, byte[] array, int startIndex)
            {
                array[startIndex] = (byte)value;
                array[startIndex + 1] = (byte)(value >> 8);
                array[startIndex + 2] = (byte)(value >> 16);
                array[startIndex + 3] = (byte)(value >> 24);
            }
            public byte[] GetByteData()
            {
                byte[] b = new byte[BytesInUse];
                for (int i = 0; i < BytesInUse; i++)
                {
                    int index = i / 8;
                    int sub = i % 8;
                    ulong u = data[index];
                    b[i] = (byte)((u >> ((7 - sub) * 8)) & 255);
                }
                FromUInt((uint)BytesInUse, b, 0);
                return b;
            }

        }
    }

