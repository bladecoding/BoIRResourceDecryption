using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Streams;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace boir
{
    class Program
    {
        static void Main(string[] args)
        {
            var dir = new DirectoryInfo(@"C:\Program Files (x86)\Steam\SteamApps\common\The Binding of Isaac Rebirth\resources\packed");
            foreach (var file in dir.GetFiles("*.a"))
            {
                using (var fs = File.OpenRead(file.FullName))
                {
                    var subDir = Path.GetFileNameWithoutExtension(fs.Name);
                    if (!Directory.Exists(subDir))
                        Directory.CreateDirectory(subDir);
                    new FileReader().Read(fs, subDir);
                }
            }

        }
    }
    public class FileReader
    {
        public void Read(Stream s, string dir)
        {
            if (Encoding.ASCII.GetString(s.ReadBytes(7)) != "ARCH000")
                throw new NotSupportedException("Not a boir file");
            var compressed = s.ReadBoolean();
            var recordStart = s.ReadInt32();
            var recordCount = s.ReadInt16();

            s.Position = recordStart;
            for (int i = 0; i < recordCount; i++)
            {
                if (compressed)
                {
                    var rec = new CompressedRecord();
                    rec.Read(s);
                    File.WriteAllText(Path.Combine(dir, i + ".xml"), rec.Data);
                }
                else
                {
                    var rec = new EncryptedRecord();
                    rec.Read(s);
                    File.WriteAllBytes(Path.Combine(dir, i + ".png"), rec.Data);
                }
            }
        }
    }
    public class CompressedRecord
    {
        public void Read(Stream s)
        {
            var chk = s.ReadInt64();
            var dataStart = s.ReadInt32();
            var dataLen = s.ReadInt32();
            var _un = s.ReadInt32();

            var o = s.Position;
            s.Position = dataStart;


            var sb = new StringBuilder();
            var decoder = new SharpLZW.LZWDecoder();
            while (sb.Length < dataLen)
            {
                sb.Append(decoder.DecodeFromCodes(s.ReadBytes(s.ReadInt32())));
            }

            Data = sb.ToString();

            s.Position = o;
        }


        public string Data { get; set; }
    }
    public class EncryptedRecord
    {
        public void Read(Stream s)
        {
            var chk = s.ReadInt32();
            var key = (uint)(s.ReadInt32() ^ 0xF9524287 | 1);
            var dataStart = s.ReadInt32();
            var dataLen = s.ReadInt32();
            var _un = s.ReadInt32();

            var o = s.Position;
            s.Position = dataStart;

            Data = Decrypt(s.ReadBytes(dataLen), ref key);

            s.Position = o;
        }

        unsafe static byte[] Decrypt(byte[] data, ref uint key)
        {
            var origSize = data.Length;
            if (data.Length % 255 != 0)
                Array.Resize(ref data, data.Length + (255 - (data.Length % 255)));

            fixed (byte* ptr = data)
            {
                for (int i = 0; i < data.Length / 4; i++)
                {
                    var bptr = ptr + i * 4;

                    *(uint*)(bptr) = *(uint*)(bptr) ^ key;
                    var s = key & 0xF;
                    if ((s -= 2) == 0)
                    {
                        // 1 2 3 4
                        // 4 3 2 1
                        SwapBytes(bptr, bptr + 3);
                        SwapBytes(bptr + 1, bptr + 2);
                    }
                    else if ((s -= 7) == 0)
                    {
                        // 1 2 3 4
                        // 2 1 4 3

                        SwapBytes(bptr, bptr + 1);
                        SwapBytes(bptr + 2, bptr + 3);
                    }
                    else if ((s -= 4) == 0)
                    {
                        // 1 2 3 4
                        // 3 4 1 2
                        SwapBytes(bptr, bptr + 2);
                        SwapBytes(bptr + 1, bptr + 3);
                    }

                    s = key;
                    s <<= 8;
                    s ^= key;
                    key = s;
                    key >>= 9;
                    s ^= key;
                    key = s;
                    key <<= 0x17;
                    key ^= s;

                }
            }

            if (data.Length != origSize)
                Array.Resize(ref data, origSize);
            return data;
        }
        unsafe static void SwapBytes(byte* b1, byte* b2)
        {
            var t = *b1;
            *b1 = *b2;
            *b2 = t;
        }
        public byte[] Data { get; set; }
    }
}
