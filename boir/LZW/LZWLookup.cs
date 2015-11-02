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
using System.IO;
using System.IO.Streams;
using System.Linq;
using System.Text;
class LookupTable
{
    public List<LookupRecord> Records = Enumerable.Range(0, 0x1001).Select(i => new LookupRecord { Character = i < 256 ? (byte)i : (byte)0 }).ToList();
    int RecordsIndex = 0x100;
    public int CodeLen = 8;
    public byte OutputText(byte lastChar, int index, Stream s)
    {
        if (index == -1)
            return lastChar;

        var bytes = new Stack<byte>();
        while (index != -1)
        {
            var rec = Records[index];
            bytes.Push(lastChar = rec.Character);
            index = rec.PreviousCharacter;
            if (bytes.Count > 0xFFF)
                throw new OverflowException();
        }
        while (bytes.Count > 0)
            s.WriteInt8(bytes.Pop());
        return lastChar;
    }

    public void AppendChar(byte lastChar, int prevVal)
    {
        Records[RecordsIndex].PreviousCharacter = prevVal;
        Records[RecordsIndex].Character = lastChar;
        RecordsIndex++;
    }

    public int FindEntry(byte[] input, int start, int length)
    {
        if (start == -1)
            start = 0;
        for (int i = start; i < Count; i++)
        {
            bool matches = true;
            var entry = Records[i];
            for (int j = length - 1; j >= 0; j--, entry = entry.PreviousCharacter != -1 ? Records[entry.PreviousCharacter] : null)
            {
                if (entry == null || entry.Character != input[j])
                {
                    matches = false;
                    break;
                }
            }
            if (matches && entry == null)
                return i;
        }
        return -1;
    }

    public void EnsureCodeLen()
    {
        if (Count >= (1 << CodeLen))
            CodeLen++;
    }
    public void EnsureCodeLen2()
    {
        if (Count > (1 << CodeLen))
            CodeLen++;
    }

    public void Reset()
    {
        RecordsIndex = 0x100;
        CodeLen = 8; 
        Records = Enumerable.Range(0, 0x1001).Select(i => new LookupRecord { Character = i < 256 ? (byte)i : (byte)0 }).ToList();
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
    public byte Character;
    public override string ToString()
    {
        return string.Format("{0:X8}|{1:X8}",
            System.Net.IPAddress.NetworkToHostOrder(PreviousCharacter),
            System.Net.IPAddress.NetworkToHostOrder((int)Character));
    }
}