using System;
using System.Text;
using System.IO;

namespace boir
{
    public class TextUtil
    {
        public static bool IsText(byte[] raw)
        {
            if (raw.Length == 0) return false;

            for (var i = 0; i < raw.Length; i++)
                if (isControlChar(raw[i]))
                {
                    return false;
                }
            return true;
        }

        public static bool isControlChar(int ch)
        {
            return (ch > Chars.NUL && ch < Chars.BS)
                || (ch > Chars.CR && ch < Chars.SUB);
        }

        public static class Chars
        {
            public static char NUL = (char)0; // Null char
            public static char BS = (char)8; // Back Space
            public static char CR = (char)13; // Carriage Return
            public static char SUB = (char)26; // Substitute
        }
    }
}

