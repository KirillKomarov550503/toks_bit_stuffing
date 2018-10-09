using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lab1_1
{
    class BitStuffing
    {
        private static void PrintBytes(byte[] bytes)
        {
            Console.Write("Bytes: ");
            foreach (byte bt in bytes)
            {
                Console.Write(bt + " ");
            }
            Console.WriteLine("\n_________________________________________");
        }
        public static byte[] ConvertBinaryStrToByteArray(string binaryStr)
        {

            int remainder = binaryStr.Length % 8;
            if (remainder > 0)
            {
                for (int i = 0; i < 8 - remainder; i++)
                {
                    binaryStr += "0";
                }
            }
            int numberOfBytes = binaryStr.Length / 8;
            Console.WriteLine("Binary str after cal: " + binaryStr);
            byte[] bytes = new byte[numberOfBytes];
            for (int i = 0; i < numberOfBytes; i++)
            {
                bytes[i] = Convert.ToByte(binaryStr.Substring(8 * i, 8), 2);
            }
            return bytes;
        }
        public static string ConvertBytesToBinaryString(byte[] bytes)
        {
            string bits = "";
            foreach (byte bt in bytes)
            {
                bits += Convert.ToString(bt, 2).PadLeft(8, '0');
            }
            return bits;
        }

        public static byte[] CodeData(byte[] bytes)
        {
            string bits = ConvertBytesToBinaryString(bytes);
            string bitSeq = "0101101";
            string bitStuff = "01011011";
            string modifiedString = bits.Replace(bitSeq, bitStuff);
            return ConvertBinaryStrToByteArray(modifiedString);
        }

        public static byte[] DecodeData(byte[] bytes)
        {
            string bits = ConvertBytesToBinaryString(bytes);
            string modifiedBits = bits.Replace("01011011", "0101101");
            string resultStr;
            int diff = bits.Length - modifiedBits.Length;
            if (diff > 0)
            {
                resultStr = modifiedBits.Substring(0, 8 * (modifiedBits.Length / 8));
            }
            else
            {
                resultStr = modifiedBits;
            }
            return ConvertBinaryStrToByteArray(resultStr);
        }
    }
}
