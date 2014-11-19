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

namespace System.IO
{
    public class BitReader
    {
        protected Stream m_Stream;
        protected ulong m_Buffer;
        protected uint m_BitsInBuffer;
        protected uint m_BitsRead;
        public BitReader(Stream s)
        {
            m_Stream = s;
            m_Buffer = 0;
            m_BitsInBuffer = 0;
        }

        public int ReadBits(int count)
        {
            return (int)ReadBits((uint)count);
        }

        public uint BitsRead
        {
            get { return m_BitsRead - m_BitsInBuffer; }
        }

        public long BitsLength
        {
            get { return m_Stream.Length * 8; }
        }

        public uint ReadBits(uint count)
        {
            if (count > 32)
                throw new NotSupportedException("Cannot read more than 32 bits at a time.");

            while (m_BitsInBuffer < count)
            {
                var b = m_Stream.ReadInt8();
                m_Buffer <<= 8;
                m_Buffer |= b;
                m_BitsRead += 8;
                m_BitsInBuffer += 8;
            }

            var ret = (m_Buffer >> (int)(m_BitsInBuffer - count)) & (uint)((1 << (int)count) - 1);
            m_BitsInBuffer -= count;
            m_Buffer &= (uint)((1 << (int)m_BitsInBuffer) - 1);
            return (uint)ret;
        }
    }
}
