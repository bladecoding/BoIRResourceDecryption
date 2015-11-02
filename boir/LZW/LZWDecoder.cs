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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;


public class LZWDecoder
{
    public LZWDecoder()
    {
    }

    LookupTable lookup = new LookupTable();

    public byte[] Decode(byte[] data)
    {
        using (var ms = new MemoryStream(data))
            return Decode(ms);
    }


    public byte[] Decode(Stream stream)
    {

        using (var ms = new MemoryStream())
        {
            var br = new BitReader(stream);

            int i = 0;
            int prevValue = -1;
            byte lastChar = 0;

            prevValue = br.ReadBits(lookup.CodeLen);
            lastChar = lookup.OutputText(lastChar, prevValue, ms);

            while (br.BitsRead < br.BitsLength)
            {
                if (lookup.Count >= 0xFFF)
                {
                    if (br.BitsRead + lookup.CodeLen <= br.BitsLength)
                    {
                        lookup.Reset();
                        prevValue = br.ReadBits(lookup.CodeLen);
                        lastChar = lookup.OutputText(lastChar, prevValue, ms);
                    }
                    else
                    {
                        break;
                    }
                    continue;
                }

                int value;
                if (br.BitsRead + lookup.CodeLen <= br.BitsLength)
                {
                    lookup.EnsureCodeLen();
                    value = br.ReadBits(lookup.CodeLen);
                }
                else
                {
                    break;
                }

                if (value != lookup.Count)
                {
                    lastChar = lookup.OutputText(lastChar, value, ms);
                    if (prevValue != -1)
                        lookup.AppendChar(lastChar, prevValue);
                }
                else
                {
                    if (prevValue != -1)
                        lookup.AppendChar(lastChar, prevValue);
                    lastChar = lookup.OutputText(lastChar, value, ms);
                }

                prevValue = value;
            }
            return ms.ToArray();
        }
    }



}