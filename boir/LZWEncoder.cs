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

namespace SharpLZW
{
    public class LZWEncoder
    {
        public Dictionary<string, int> dict = new Dictionary<string, int>();
        ANSI table = null;
             
        int codeLen = 8;
        public LZWEncoder()
        {
            table = new ANSI();
            dict = table.Table;
        }

        public string EncodeToCodes(string input)
        {
            StringBuilder sb = new StringBuilder();

            int i = 0;
            string w = "";
            while (i < input.Length)
            {
                w = input[i].ToString();

                i++;                

                while (dict.ContainsKey(w) && i < input.Length)
                {
                    w += input[i];
                    i++;
                }

                if (dict.ContainsKey(w) == false)                
                {
                    string matchKey = w.Substring(0, w.Length - 1);
                    sb.Append(dict[matchKey] +  ", ");

                    dict.Add(w, dict.Count);
                    i--;
                }
                else 
                {
                    sb.Append(dict[w] + ", ");
                }
            }

            return sb.ToString(); 
        }

        public string Encode(string input)
        {
            StringBuilder sb = new StringBuilder();

            int i = 0;
            string w = "";
            while (i < input.Length)
            {
                w = input[i].ToString();

                i++;

                while (dict.ContainsKey(w) && i < input.Length)
                {
                    w += input[i];
                    i++;
                }

                if (dict.ContainsKey(w) == false)                
                {
                    string matchKey = w.Substring(0, w.Length - 1);
                    sb.Append(Convert.ToString(dict[matchKey], 2).FillWithZero(codeLen));

                    if (Convert.ToString(dict.Count, 2).Length > codeLen)
                        codeLen++;

                    dict.Add(w, dict.Count);
                    i--;
                }
                else
                {                    
                    sb.Append(Convert.ToString(dict[w], 2).FillWithZero(codeLen));

                    if (Convert.ToString(dict.Count, 2).Length > codeLen)
                        codeLen++;   
                 
                }
            }

            return sb.ToString();
        }

        public byte[] EncodeToByteList(string input)
        {
            string encodedInput = Encode(input);
            return encodedInput.ToByteArray();
        }

    }
}
