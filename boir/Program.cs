﻿/*   
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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Streams;
using System.Linq;
using System.Text;
using System.Xml;
using System.Threading.Tasks;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using Newtonsoft.Json;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Parameters;

namespace boir
{
    public class Program
    {
        static void Main(string[] args)
        {


            //windows accepts both forward and back slashes, so i will use the ones that work everywhere
            string steamDir;
            string homeDir = Environment.GetFolderPath(Environment.SpecialFolder.Personal);

            if (Environment.OSVersion.Platform == PlatformID.Unix)
                steamDir = Path.Combine(homeDir, ".local/share/Steam");
            else if (Environment.OSVersion.Platform == PlatformID.MacOSX)
                steamDir = Path.Combine(homeDir, "Library/Application Support/Steam");
            else
                steamDir = (string)Registry.GetValue(@"HKEY_CURRENT_USER\Software\Valve\Steam", "SteamPath", "C:/Program Files (x86)/Steam");

            var dir = new DirectoryInfo(Path.Combine(steamDir, "SteamApps/common/The Binding of Isaac Rebirth/resources/packed"));

            var hashToFilename = GetFileNames(Path.Combine(dir.FullName, "config.a"));

            var found = 0;
            var total = 0;

            foreach (var file in dir.GetFiles("*.a"))
            {
                using (var fs = File.OpenRead(file.FullName))
                {
                    //var subDir = Path.GetFileNameWithoutExtension(fs.Name);
                    //if (!Directory.Exists(subDir))
                    //    Directory.CreateDirectory(subDir);
                    foreach (var p in new FileReader().Read(fs))
                    {
                        var fileName = (hashToFilename.ContainsKey(p.Hash)) ? hashToFilename[p.Hash] : "nothing";
                        Console.WriteLine("Found for hash {0} file name {1}", p.Hash, fileName);

                        total++;

                        string filePath;
                        if (fileName != "nothing")
                        {
                            found++;
                            filePath = fileName;
                        }
                        else
                        {
                            filePath = Path.Combine(file.Name, (total - found) + "." + p.RecordType.ToString().ToLower());
                        }
                        Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                        File.WriteAllBytes(filePath, p.Data);
                    }
                }
            }
            Console.WriteLine("Found {0} of {1}", found, total);
        }

        static Dictionary<uint, string> GetFileNames(string configFilePath)
        {
            var assembly = Assembly.GetExecutingAssembly();
            string result;

            using (Stream stream = assembly.GetManifestResourceStream("BoIRResourceEditor.xml_paths_config.json"))
            using (StreamReader reader = new StreamReader(stream))
            {
                result = reader.ReadToEnd();
            }

            var spec = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, List<string>>>>(result);

            var ret = new Dictionary<uint, string>();

            using (var fs = File.OpenRead(configFilePath))
                foreach (var rec in new FileReader().Read(fs))
                {
                    var xml = System.Text.Encoding.UTF8.GetString(rec.Data);

                    foreach (var doc in ReadAllDocuments(new StringReader(xml)))
                    {
                        if (!spec.ContainsKey(doc.FirstChild.Name))
                            continue;

                        var rootNodeName = doc.FirstChild.Name;

                        foreach (var subEntry in spec[doc.FirstChild.Name])
                        {
                            var rootAttrName = subEntry.Key;
                            string rootPath;
                            if (rootAttrName == "")
                            {
                                rootPath = "";
                            }
                            else if (rootAttrName.Contains("."))
                            {
                                continue;
                                //TODO: handle fxRays
                            }
                            else
                            {
                                rootPath = doc.SelectSingleNode(rootNodeName + "/@" + rootAttrName).Value;
                            }

                            foreach (var xpath in subEntry.Value)
                            {
                                //TODO: special cases
                                foreach (XmlNode attr in doc.SelectNodes("/" + rootAttrName + "/" + xpath))
                                {
                                    var path = (rootPath == "") ? attr.Value : (rootPath + "/" + attr.Value);
                                    var hash = Hash1(path);

                                    if (ret.ContainsKey(hash))
                                        Console.WriteLine("{0}: existing {1}, new {2}", hash, ret[hash], path);
                                    else
                                        ret.Add(hash, path);
                                }
                            }
                        }
                    }
                }

            return ret;
        }

        static IEnumerable<XmlDocument> ReadAllDocuments(TextReader stream)
        {
            XmlReaderSettings settings = new XmlReaderSettings();
            settings.ConformanceLevel = ConformanceLevel.Fragment;

            var reader = XmlReader.Create(stream, settings);
            while (!reader.EOF)
            {
                reader.MoveToContent();
                XmlDocument doc = new XmlDocument();
                try
                {
                    while (reader.NodeType != XmlNodeType.Element)
                    {
                        if (!reader.Read())
                            yield break;
                    }
                    doc.Load(reader.ReadSubtree());
                }
                catch (XmlException)
                {
                    Console.WriteLine("Failed to load XML: {0}...", reader.ReadContentAsString());
                    yield break;
                }
                yield return doc;
            }
        }

        static uint Hash1(string str)
        {
            uint ret = 0x1505;
            for (int i = 0; i < str.Length; i++)
            {
                byte c = (byte)str[i];
                if ((byte)(c - 0x41) <= 0x19)
                    c += 0x20;
                if (c == 0x5C)
                    c = 0x2F;
                ret = (uint)(((ret << 5) + ret) + c);
            }
            return ret;
        }


    }

    public class FileReader
    {
        public IEnumerable<Record> Read(Stream s)
        {
            if (Encoding.ASCII.GetString(s.ReadBytes(7)) != "ARCH000")
                throw new NotSupportedException("Not a boir file");
            var compressed = s.ReadByte();
            var recordStart = s.ReadInt32();
            var recordCount = s.ReadInt16();

            s.Position = recordStart;
            for (int i = 0; i < recordCount; i++)
            {
                Record rec = null;
                switch ((FileType)compressed)
                {
                    case FileType.Compressed:
                        rec = (Record)new CompressedRecord();
                        break;
                    case FileType.Encrypted:
                        rec = new EncryptedRecord();
                        break;
                    case FileType.Encrypted2:
                        rec = new MiniZCompressedRecord();
                        break;
                }
                rec.Read(s);

                yield return rec;
            }
        }
    }

    public enum FileType
    {
        Encrypted = 0,
        Compressed = 1,
        Encrypted2 = 2,
    }

    public enum RecordType
    {
        PNG,
        BMF,
        OGG,
        OGV,
        WAV,
        BIN, //some unknown binary type
        XML,
        TXT,
    }

    public abstract class Record
    {
        public RecordType RecordType { get; set; }
        public byte[] Data { get; set; }
        public uint Hash { get; set; }
        public abstract byte[] Decompress(Stream s, int dataLen, uint key);

        public void Read(Stream s)
        {
            Hash = s.ReadUInt32();
            var key = (uint)(s.ReadInt32());
            var dataStart = s.ReadInt32();
            var dataLen = s.ReadInt32();
            var _un = s.ReadInt32();

            var o = s.Position;
            s.Position = dataStart;

            Data = Decompress(s, dataLen, key);
            RecordType = DetermineRecordType();

            s.Position = o;
        }

        protected uint XorKey(uint key)
        {
            return key ^ 0xF9524287 | 1;
        }

        private RecordType DetermineRecordType()
        {
            if (TextUtil.IsText(Data))
            {
                if (Data[0] == '<')
                {
                    return RecordType.XML;
                }
                else
                {
                    return RecordType.TXT;
                }
            }

            if (Encoding.ASCII.GetString(Data.Take(4).ToArray()) == "OggS")
            {
                if (Encoding.ASCII.GetString(Data.Skip(0x1D).Take(6).ToArray()) == "theora" ||
                    Encoding.ASCII.GetString(Data.Skip(0x57).Take(6).ToArray()) == "theora")
                {
                    return RecordType.OGV;
                }
                else
                {
                    return RecordType.OGG;
                }
            }

            if (Encoding.ASCII.GetString(Data.Take(4).ToArray()) == "RIFF" &&
                Encoding.ASCII.GetString(Data.Skip(8).Take(4).ToArray()) == "WAVE")
            {
                return RecordType.WAV;
            }

            if (Data.Take(8).SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }))
            {
                return RecordType.PNG;
            }

            if (Encoding.ASCII.GetString(Data.Take(3).ToArray()) == "BMF")
            {
                return RecordType.BMF;
            }

            return RecordType.BIN;
        }
    }

    public class CompressedRecord : Record
    {
        public override byte[] Decompress(Stream s, int dataLen, uint key)
        {
            var sb = new List<byte>();
            var decoder = new LZWDecoder();

            while (sb.Count < dataLen)
            {
                sb.AddRange(decoder.Decode(s.ReadBytes(s.ReadInt32())));
            }

            return sb.ToArray();
        }
    }

    public class EncryptedRecord : Record
    {
        public unsafe override byte[] Decompress(Stream s, int dataLen, uint key)
        {
            key = XorKey(key);
            var data = s.ReadBytes(dataLen);

            var origSize = data.Length;
            if (data.Length % 255 != 0)
                Array.Resize(ref data, data.Length + (255 - (data.Length % 255)));

            fixed (byte* ptr = data)
            {
                for (int i = 0; i < data.Length / 4; i++)
                {
                    var bptr = ptr + i * 4;

                    *(uint*)(bptr) = *(uint*)(bptr) ^ key;
                    var s_ = key & 0xF;
                    if ((s_ -= 2) == 0)
                    {
                        // 1 2 3 4
                        // 4 3 2 1
                        SwapBytes(bptr, bptr + 3);
                        SwapBytes(bptr + 1, bptr + 2);
                    }
                    else if ((s_ -= 7) == 0)
                    {
                        // 1 2 3 4
                        // 2 1 4 3

                        SwapBytes(bptr, bptr + 1);
                        SwapBytes(bptr + 2, bptr + 3);
                    }
                    else if ((s_ -= 4) == 0)
                    {
                        // 1 2 3 4
                        // 3 4 1 2
                        SwapBytes(bptr, bptr + 2);
                        SwapBytes(bptr + 1, bptr + 3);
                    }

                    s_ = key;
                    s_ <<= 8;
                    s_ ^= key;
                    key = s_;
                    key >>= 9;
                    s_ ^= key;
                    key = s_;
                    key <<= 0x17;
                    key ^= s_;

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
    }

    public class MiniZCompressedRecord : Record
    {
        [DllImport("MiniZLib.dll", EntryPoint = "tinfl_decompress_mem_to_mem", CallingConvention = CallingConvention.Cdecl)]
        unsafe extern static int tinfl_decompress_mem_to_mem(
            byte* pOut_buf,
            int* out_buf_len,
            byte* pSrc_buf,
                int src_buf_len,
                int flags
            );
        public static unsafe byte[] DecompressMem(byte[] src)
        {
            var dst = new byte[0x400];
            var len = dst.Length;

            fixed (byte* srcPtr = src)
            fixed (byte* dstPtr = dst)
            {
                //Todo error checking.
                //miniz seems to return -1 even on successful decompress.
                //1 check is probably to see if the returned length is 0.
                var err = tinfl_decompress_mem_to_mem(dstPtr, &len, srcPtr, src.Length, 2);
            }
            Array.Resize(ref dst, len);
            return dst;

        }

        //double multiply on 32bit systems
        /*ulong Multiply(uint a, uint b, uint c, uint d)
        {
            if (b == 0 || d == 0)
                return (uint)(a * c);
            return ((ulong)a * c) + ((ulong)((b * c) + (a * d)) << 32);

        }*/

        //Still kind of a mess. First time manually decompiling int64 arithmetic.
        byte[] Generate128BitKey(uint seed)
        {
            var key = new byte[0x400];

            ulong a = seed;
            ulong b = (((seed << 15) ^ seed) << 8) ^ (seed >> 9) ^ seed;

            ulong newSeed = a << 32 | b;


            for (int i = 0; i < key.Length; i += 4)
            {

                var num = newSeed >> 27 ^ (newSeed >> 45);
                var high = (uint)(num >> 32);
                var low = (uint)(num);
                var left = (uint)low << (-(int)high & 0x1F);
                var right = (uint)low >> (byte)(high);

                //replace this with bc pack helper.
                BitConverter.GetBytes(left | right).CopyTo(key, i);

                newSeed = (newSeed * 0x5851F42D4C957F2D) + 0x7F;
            }
            return key;
        }

        public unsafe override byte[] Decompress(Stream s, int dataLen, uint key)
        {
            var isc = new IsaacCipher();
            isc.Init(false, new KeyParameter(Generate128BitKey(key)));

            var sb = new List<byte>();
            var isEnc = false;
            while (sb.Count < dataLen)
            {
                var olen = s.ReadInt32();
                var len = olen & 0x7fffffff;
                var bytes = s.ReadBytes(len);
                var isCompressed = (olen >> 31) != 0;

                if (!isEnc && !isCompressed)
                {
                    isEnc = len == 0x400;
                }

                if (isEnc)
                {
                    var ret = new byte[bytes.Length];
                    isc.ProcessBytes(bytes, 0, bytes.Length, ret, 0);
                    sb.AddRange(ret);
                }
                else
                {
                    sb.AddRange(DecompressMem(bytes));
                }
            }

            return sb.ToArray();
        }
    }

}
