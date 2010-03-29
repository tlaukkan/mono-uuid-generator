using System;
using System.Collections.Generic;
using System.Text;

namespace Jug.Test
{
    public class TestUtil
    {
        private const string HexCharacters = "0123456789ABCDEF";

        public static List<int> FindFirstDifference(byte[] bytes1, byte[] bytes2)
        {
            int maxLength = Math.Max(bytes1.Length, bytes2.Length);
            List<int> diffIndexes = new List<int>();
            for (int i = 0; i < maxLength; i++)
            {
                if (i >= bytes1.Length)
                {
                    diffIndexes.Add(i);
                    continue;
                }
                if (i >= bytes2.Length)
                {
                    diffIndexes.Add(i);
                    continue;
                }
                if (bytes1[i] != bytes2[i])
                {
                    diffIndexes.Add(i);
                }
            }
            return diffIndexes;
        }


        public static string RenderByteArray(byte[] bytes, List<int> colorIndexes, int rowLength)
        {
            StringBuilder stringBuilder = new StringBuilder();

            for (int i = 0; i < bytes.Length; i++)
            {
                byte b = bytes[i];

                if (i != 0 && i % rowLength == 0)
                {
                    stringBuilder.Append("<br/>");
                }

                if (i % rowLength != 0)
                {
                    stringBuilder.Append("-");
                }

                if (colorIndexes.Contains(i))
                {
                    stringBuilder.Append("<font color=\"#cc0000\">");
                }
                stringBuilder.Append(HexCharacters[b / 16]);
                stringBuilder.Append(HexCharacters[b % 16]);
                if (colorIndexes.Contains(i))
                {
                    stringBuilder.Append("</font>");
                }

            }

            return stringBuilder.ToString() + "</br> (Length: " + bytes.Length + ")";
        }
    }
}
