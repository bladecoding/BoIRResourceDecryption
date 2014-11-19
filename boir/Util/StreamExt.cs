/*   
BoIRResourceEditor
Copyright (C) 2014 Bladecoding

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.IO.Streams
{
    /// <summary>
    /// BinaryReader/BinaryWriter as extension methods
    /// </summary>
    //TODO: Document
    public static class StreamExt
    {
        [ThreadStatic]
        static byte[] _buffer;
        static byte[] buffer
        {
            get { return _buffer ?? (_buffer = new byte[16]); }
        }

        public static void FillBuffer(this Stream stream, int numBytes)
        {
            if ((numBytes < 0x0) || (numBytes > buffer.Length))
            {
                throw new ArgumentOutOfRangeException("numBytes");
            }
            if (numBytes < 1)
                return;

            int read;
            if (numBytes == 0x1)
            {
                read = stream.ReadByte();
                if (read == -1)
                {
                    throw new EndOfStreamException("End of stream");
                }
                buffer[0x0] = (byte)read;
            }
            else
            {
                int offset = 0x0;
                do
                {
                    read = stream.Read(buffer, offset, numBytes - offset);
                    if (read == 0x0)
                    {
                        throw new EndOfStreamException("End of stream");
                    }
                    offset += read;
                }
                while (offset < numBytes);
            }

        }
        public static void WriteBoolean(this Stream s, bool value)
        {
            s.WriteInt8(value ? ((byte)0x1) : ((byte)0x0));
        }
        public static void WriteInt8(this Stream s, byte num)
        {
            s.WriteByte(num);
        }
        public static void WriteInt16(this Stream s, Int16 value)
        {
            buffer[0x0] = (byte)value;
            buffer[0x1] = (byte)(value >> 0x8);
            s.Write(buffer, 0x0, 0x2);
        }
        public static void WriteInt32(this Stream s, Int32 value)
        {
            buffer[0x0] = (byte)value;
            buffer[0x1] = (byte)(value >> 0x8);
            buffer[0x2] = (byte)(value >> 0x10);
            buffer[0x3] = (byte)(value >> 0x18);
            s.Write(buffer, 0x0, 0x4);
        }
        public static void WriteInt64(this Stream s, Int64 value)
        {
            buffer[0x0] = (byte)value;
            buffer[0x1] = (byte)(value >> 0x8);
            buffer[0x2] = (byte)(value >> 0x10);
            buffer[0x3] = (byte)(value >> 0x18);
            buffer[0x4] = (byte)(value >> 0x20);
            buffer[0x5] = (byte)(value >> 0x28);
            buffer[0x6] = (byte)(value >> 0x30);
            buffer[0x7] = (byte)(value >> 0x38);
            s.Write(buffer, 0x0, 0x8);
        }
        public static unsafe void WriteDouble(this Stream s, double num)
        {
            Int64 n1 = *((Int64*)&num);
            s.WriteInt64(n1);
        }
        public static unsafe void WriteSingle(this Stream s, float num)
        {
            var n1 = *((Int32*)&num);
            s.WriteInt32(n1);
        }
        public static void WriteBytesWithLength(this Stream s, byte[] bytes)
        {
            s.WriteInt32(bytes.Length);
            s.WriteBytes(bytes);
        }
        public static void WriteBytes(this Stream s, byte[] bytes, Int32 len)
        {
            s.Write(bytes, 0, len);
        }
        public static void WriteBytes(this Stream s, byte[] bytes)
        {
            s.Write(bytes, 0, bytes.Length);
        }
        public static void WriteString(this Stream s, string str)
        {
            if (str == null)
                str = string.Empty;

            s.WriteEncodedInt((Int32)str.Length);
            if (str.Length > 0)
                s.WriteBytes(Encoding.UTF8.GetBytes(str));
        }
        public static void WriteEncodedInt(this Stream s, int value)
        {
            uint num = (uint)value;
            while (num >= 0x80)
            {
                s.WriteInt8((byte)(num | 0x80));
                num = num >> 0x7;
            }
            s.WriteInt8((byte)num);
        }

        public static byte ReadInt8(this Stream s)
        {
            int read = s.ReadByte();
            if (read == -1)
            {
                throw new EndOfStreamException("End of stream");
            }
            return (byte)read;
        }
        public static bool ReadBoolean(this Stream s)
        {
            return s.ReadInt8() != 0;
        }

        public static Int16 ReadInt16(this Stream s)
        {
            s.FillBuffer(0x2);
            return (Int16)(buffer[0x0] | (buffer[0x1] << 0x8));
        }
        public static UInt16 ReadUInt16(this Stream s)
        {
            return (UInt16)s.ReadInt16();
        }

        public static Int32 ReadInt32(this Stream s)
        {
            s.FillBuffer(0x4);
            return (((buffer[0x0] | (buffer[0x1] << 0x8)) | (buffer[0x2] << 0x10)) | (buffer[0x3] << 0x18));

        }
        public static UInt32 ReadUInt32(this Stream s)
        {
            return (UInt32)s.ReadInt32();
        }

        public static Int64 ReadInt64(this Stream s)
        {
            s.FillBuffer(0x8);
            UInt64 num = (UInt32)(((buffer[0x0] | (buffer[0x1] << 0x8)) | (buffer[0x2] << 0x10)) | (buffer[0x3] << 0x18));
            UInt64 num2 = (UInt32)(((buffer[0x4] | (buffer[0x5] << 0x8)) | (buffer[0x6] << 0x10)) | (buffer[0x7] << 0x18));
            return (Int64)((num2 << 0x20) | num);

        }
        public static UInt64 ReadUInt64(this Stream s)
        {
            return (UInt64)s.ReadInt64();
        }

        public static unsafe double ReadDouble(this Stream s)
        {
            var ret = (UInt64)s.ReadUInt64();
            return *((double*)&ret);
        }

        public static unsafe float ReadSingle(this Stream s)
        {
            var ret = s.ReadUInt32();
            return *((float*)&ret);
        }

        public static byte[] ReadBytesWithLength(this Stream s)
        {
            Int32 len = s.ReadInt32();
            return s.ReadBytes(len);
        }
        public static byte[] ReadBytes(this Stream s, Int32 count)
        {
            if (count < 0x0)
            {
                throw new ArgumentOutOfRangeException("count");
            }

            byte[] buffer = new byte[count];
            int offset = 0x0;
            do
            {
                int num2 = s.Read(buffer, offset, count);
                if (num2 == 0x0)
                {
                    break;
                }
                offset += num2;
                count -= num2;
            }
            while (count > 0x0);
            if (offset != buffer.Length)
            {
                byte[] dst = new byte[offset];
                Buffer.BlockCopy(buffer, 0x0, dst, 0x0, offset);
                buffer = dst;
            }
            return buffer;

        }
        public static string ReadString(this Stream s)
        {
            int len = s.ReadEncodedInt();
            if (len > 0)
                return Encoding.UTF8.GetString(s.ReadBytes(len));
            return string.Empty;
        }

        public static string ReadLine(this Stream s)
        {
            var ret = new StringBuilder();
            char c;
            while ((c = (char)s.ReadInt8()) != '\n')
            {
                if (c != '\r')
                    ret.Append(c);
            }
            return ret.ToString();
        }

        public static int ReadEncodedInt(this Stream s)
        {
            byte num3;
            int num = 0x0;
            int num2 = 0x0;
            do
            {
                if (num2 == 0x23)
                {
                    throw new FormatException("Format_Bad7BitInt32");
                }
                num3 = s.ReadInt8();
                num |= (num3 & 0x7f) << num2;
                num2 += 0x7;
            }
            while ((num3 & 0x80) != 0x0);
            return num;
        }


        public static void InternalCopyTo(this Stream source, Stream destination, int bufferSize)
        {
            int num;
            var buffer = new byte[bufferSize];
            while ((num = source.Read(buffer, 0, buffer.Length)) != 0)
            {
                destination.Write(buffer, 0, num);
            }
        }
        public static void CopyTo(this Stream source, Stream destination)
        {
            source.InternalCopyTo(destination, 0x1000);
        }

        public static byte[] CopyToArray(this Stream source)
        {
            if (source is MemoryStream)
                return ((MemoryStream)source).ToArray();
            using (var ms = new MemoryStream())
            {
                source.CopyTo(ms);
                return ms.ToArray();
            }
        }
    }
    public static class MemoryStreamExt
    {
        public static void Reset(this MemoryStream ms)
        {
            ms.Position = 0;
        }
    }
}



namespace System.IO.Streams.Generic
{
    public static class StreamGenericExt
    {
        static Dictionary<Type, Action<Stream, object>> WriteFuncs = new Dictionary<Type, Action<Stream, object>>()
        {
            {typeof(bool), (s, o) => s.WriteBoolean((bool)o)},
            {typeof(byte), (s, o) => s.WriteInt8((byte)o)},
            {typeof(Int16), (bw, data) => bw.WriteInt16((Int16)data) },
            {typeof(Int32), (bw, data) => bw.WriteInt32((Int32)data) },
            {typeof(Int64), (bw, data) => bw.WriteInt64((Int64)data) },
            {typeof(Single), (bw, data) => bw.WriteSingle((Single)data) },
            {typeof(Double), (bw, data) => bw.WriteDouble((Double)data) },
            {typeof(byte[]), (s, o) => s.WriteBytesWithLength((byte[])o)},
            {typeof(string), (s, o) => s.WriteString((string)o)},
        };
        public static void Write<T>(this Stream stream, T obj)
        {
            if (WriteFuncs.ContainsKey(typeof(T)))
            {
                WriteFuncs[typeof(T)](stream, obj);
                return;
            }

            throw new NotImplementedException();
        }
        static Dictionary<Type, Func<Stream, object>> ReadFuncs = new Dictionary<Type, Func<Stream, object>>()
        {
            {typeof(bool), s => s.ReadBoolean()},
            {typeof(byte), s => s.ReadInt8()},
            {typeof(Int16), br => br.ReadInt16() },
            {typeof(Int32), br => br.ReadInt32() },
            {typeof(Int64), br => br.ReadInt64() },
            {typeof(Single), br => br.ReadSingle() },
            {typeof(Double), br => br.ReadDouble() },
            {typeof(byte[]), s => s.ReadBytesWithLength()},
            {typeof(string), s => s.ReadString()},
        };
        public static T Read<T>(this Stream stream)
        {
            if (ReadFuncs.ContainsKey(typeof(T)))
                return (T)ReadFuncs[typeof(T)](stream);

            throw new NotImplementedException();
        }
    }
}