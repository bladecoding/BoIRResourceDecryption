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

public class LZWEncoder
{
    LookupTable lookup = new LookupTable();

    public LZWEncoder()
    {
    }

    public byte[] Encode(byte[] input)
    {
        var buffer = new byte[0xffff];
        var bufferIdx = 0;
        using (var ms = new MemoryStream())
        {
            using (var bw = new BitWriter(ms))
            {

                int i = 0;
                string w = "";
                while (i < input.Length)
                {

                    bufferIdx = 0;
                    buffer[bufferIdx++] = input[i++];

                    int cur = -1, prev = -1;
                    while ((cur = lookup.FindEntry(buffer, 0, bufferIdx)) != -1 && i < input.Length)
                    {
                        buffer[bufferIdx++] = input[i++];
                        prev = cur;
                    }



                    if (cur == -1)
                    {
                        //Debug.WriteLine("E: " + prev);
                        bw.WriteBits((uint)prev, (uint)lookup.CodeLen);
                        lookup.AppendChar(buffer[bufferIdx - 1], prev);
                        i--;
                    }
                    else
                    {
                        //Debug.WriteLine("E: " + cur);
                        bw.WriteBits((uint)cur, (uint)lookup.CodeLen); 
                    }

                    lookup.EnsureCodeLen2(); 
                    if (lookup.Count > 0xFFF)
                    {
                        lookup.Reset();
                    }
                }
            }
            return ms.ToArray();
        }

    }

}
