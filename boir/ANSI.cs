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
using System.Collections;
using System.IO;

namespace SharpLZW
{
    public class ANSI
    {
        Dictionary<string, int> table = new Dictionary<string, int>();
        public Dictionary<string, int> Table
        {
            get
            {
                return table;
            }
        }

        public ANSI()
        {
            for (int i = 0; i < 256; i++)
            {
                table.Add(System.Text.Encoding.Default.GetString(new byte[1] { Convert.ToByte(i) }), i);
            }
        }

        public void WriteLine()
        {
            table.WriteLine();
        }

        public void WriteToFile()
        {
            File.WriteAllText("ANSI.txt", table.ToStringLines(), Encoding.Default);
        }
    }
}
