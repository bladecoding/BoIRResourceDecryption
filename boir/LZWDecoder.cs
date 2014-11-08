/*

	This is a simple implementation of the well-known LZW algorithm. 
    Copyright (C) 2011  Stamen Petrov <stamen.petrov@gmail.com>

    This program is free software; you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation; either version 2 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program; if not, write to the Free Software
    Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA

*/


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpLZW
{
    public class LZWDecoder
    {
        public Dictionary<string, int> dict = new Dictionary<string, int>();
        int codeLen = 8;
        ANSI table;
        public LZWDecoder()
        {
            table = new ANSI();
            dict = table.Table;         
        }

        public string DecodeFromCodes(byte[] bytes)
        {
            string output = bytes.GetBinaryString();            

            return Decode(output);
        }

        public string Decode2(string output)
        {
            StringBuilder sb = new StringBuilder();

            int i = 0;
            string w = "";
            int prevValue = -1;

            while (i < output.Length)
            {
                if (i + codeLen <= output.Length)
                {
                    w = output.Substring(i, codeLen);
                }
                else
                {
                    break;
                }

                i += codeLen;

                int value = Convert.ToInt32(w, 2);

                string key = dict.FindKey(value);
                string prevKey = dict.FindKey(prevValue);

                if (prevKey == null)
                {
                    prevKey = "";
                }

                if (key == null)
                {
                    //handles the situation cScSc
                    key = prevKey;

                    if (key.Length < 1)
                    {
                        prevValue = value;
                        continue;
                    }

                    sb.Append(prevKey + key.Substring(0, 1));
                }
                else
                {
                    sb.Append(key);
                }

                string finalKey = prevKey + key.Substring(0, 1);

                if (dict.ContainsKey(finalKey) == false)
                {
                    dict[finalKey] = dict.Count;
                }

                if (Convert.ToString(dict.Count, 2).Length > codeLen)
                    codeLen++;

                prevValue = value;
            }

            return sb.ToString();
        }
        LookupTable lookup = new LookupTable();

        public string Decode(string output)
        {
            StringBuilder sb = new StringBuilder();

            string w = "";
            int i = 0;
            int prevValue = -1;
            char lastChar = (char)0;
            while (i < output.Length)
            {
                if (i + lookup.CodeLen <= output.Length)
                {
                    w = output.Substring(i, lookup.CodeLen);
                }
                else
                {
                    break;
                }

                i += lookup.CodeLen;

                int value = Convert.ToInt32(w, 2);
                if (prevValue == -1)
                {
                    lookup.OutputText(lastChar, value, sb);
                }
                else if (value != lookup.Count)
                {
                    lastChar = lookup.OutputText(lastChar, value, sb);
                    lookup.AppendChar(lastChar, value, prevValue);
                }
                else
                {
                    lookup.AppendChar(lastChar, value, prevValue);
                    lastChar = lookup.OutputText(lastChar, value, sb);
                }


                lookup.EnsureCodeLen();
                prevValue = value;

                if (lookup.Count >= 0xFFF)
                {
                    lookup.Reset();
                    prevValue = -1;
                }
            }

            return sb.ToString();
        }

        class LookupTable
        {
            List<LookupRecord> Records = Enumerable.Range(0, 10001).Select(i => new LookupRecord { Character = i < 256 ? (char)i : (char)0 }).ToList();
            int RecordsIndex = 0x100;
            public int CodeLen = 8;
            public char OutputText(char lastChar, int index, StringBuilder sb)
            {
                if (index == -1)
                    return lastChar;

                var app = new StringBuilder();
                while (index != -1)
                {
                    var rec = Records[index];
                    app.Append(lastChar = rec.Character);
                    index = rec.PreviousCharacter;
                    if (app.Length > 0x1000)
                        throw new OverflowException();
                }
                sb.Append(app.ToString().Reverse().ToArray());
                return lastChar;
            }

            public void AppendChar(char lastChar, int val, int prevVal)
            {
                var next = Records[prevVal].PreviousIndex;
                Records[prevVal].PreviousIndex = RecordsIndex;
                Records[RecordsIndex].PreviousCharacter = prevVal;
                Records[RecordsIndex].Character = lastChar;
                Records[RecordsIndex].NextIndex = next;
                RecordsIndex++;
            }

            public void EnsureCodeLen()
            {
                if (Count >= (1 << CodeLen))
                    CodeLen++;
            }

            public void Reset()
            {
                for (int i = 0; i < Records.Count; i++)
                    Records[i].PreviousIndex = -1;
                RecordsIndex = 0x100;
                CodeLen = 8;
            }

            public int Count { get { return RecordsIndex; } }

            public string ToByteString()
            {
                return string.Join("", Records.Select(r => r.ToString())).Replace("|", "");
            }
        }

        class LookupRecord
        {
            public int PreviousCharacter = -1;
            public int PreviousIndex = -1;
            public int NextIndex = -1;
            public char Character;
            public override string ToString()
            {
                return string.Format("{0:X8}|{1:X8}|{2:X8}|{3:X8}",
                    System.Net.IPAddress.NetworkToHostOrder(PreviousCharacter), 
                    System.Net.IPAddress.NetworkToHostOrder(PreviousIndex),
                    System.Net.IPAddress.NetworkToHostOrder(NextIndex),
                    System.Net.IPAddress.NetworkToHostOrder((int)Character));
            }
        }

    }
}