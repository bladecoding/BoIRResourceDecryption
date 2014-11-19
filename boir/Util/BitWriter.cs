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
using System.IO.Streams;
using System.Linq;
using System.Text;

namespace System.IO
{
    public class BitWriter : IDisposable
    {
        uint m_BitsInBuffer;
        Stream m_Stream;
        ulong m_Buffer;

        public BitWriter(Stream s)
        {
            m_Stream = s;
            m_Buffer = 0;
            m_BitsInBuffer = 0;
        }

        public void WriteBits(uint val, uint bits)
        {
            if (bits > 32)
                throw new NotSupportedException("Cannot write more than 32 bits at a time");

            m_Buffer <<= (int)bits;
            m_Buffer |= val;
            m_BitsInBuffer += bits;

            Flush(false);
        }

        //Flushes the buffer to the stream.
        //allbits is used to indicate if we should write 7 or less remaining bits as a full byte.
        //allbits is only ever really used at the end.
        public void Flush(bool allBits)
        {
            while (m_BitsInBuffer > 7)
            {
                var b = (byte)((m_Buffer >> (int)(m_BitsInBuffer - 8)) & 0xFF);
                m_BitsInBuffer = Math.Max(m_BitsInBuffer - 8, 0);
                m_Buffer &= (ulong)((1 << (int)m_BitsInBuffer) - 1);
                m_Stream.WriteInt8(b);
            }

            if (allBits && m_BitsInBuffer > 0)
            {
                var b = (byte)((m_Buffer) << (int)(8 - m_BitsInBuffer)); //Pad the last bits with 0.
                m_BitsInBuffer = 0;
                m_Buffer = 0;
                m_Stream.WriteInt8(b);
            }
        }

        public void Dispose()
        {
            Flush(true);
        }
    }
}
