﻿using System;
using System.IO;
using System.Text;
using OpenTK;

namespace WindViewer.Editor
{
    /// <summary>
    /// FileSystem Helpers. These help with converting from Little Endian to Big Endian. 
    /// </summary>
    class FSHelpers
    {
        #region Reading
        public static byte Read8(byte[] data, int offset)
        {
            if (data.Length <= offset) {
                return 0;
            }

            return (Buffer.GetByte(data, offset));
        }

        public static short Read16(byte[] data, int offset)
        {
            if (data.Length <= offset)
            {
                return 0;
            }
            return (short)((Buffer.GetByte(data, offset) << 8) | Buffer.GetByte(data, offset + 1));
        }

        public static ushort Read16Swap(byte[] data, uint offset)
        {
            if (data.Length <= offset)
            {
                return 0;
            }
            return (ushort)((Buffer.GetByte(data, (int)offset + 1) << 8) | Buffer.GetByte(data, (int)offset));
        }

        public static int Read32(byte[] data, int offset)
        {
            if (data.Length <= offset)
            {
                return 0;
            }
            return ((Buffer.GetByte(data, offset) << 24) | (Buffer.GetByte(data, offset + 1) << 16) | (Buffer.GetByte(data, offset + 2) << 8) | Buffer.GetByte(data, offset + 3));
        }

        public static uint Read32Swap(byte[] data, uint offset)
        {
            if (data.Length <= offset)
            {
                return 0;
            }
            return (uint)((Buffer.GetByte(data, (int)offset + 3) << 24) | (Buffer.GetByte(data, (int)offset + 2) << 16) | (Buffer.GetByte(data, (int)offset + 1) << 8) | Buffer.GetByte(data, (int)offset));
        }

        public static string ReadString(byte[] data, ref int offset)
        {
            if (offset >= data.Length) return null;
            while (data[offset] == 0) offset++;
            int length = Array.IndexOf(data, (byte)0, offset) - offset;
            string returnString = ReadString(data, offset, length);
            offset += length;
            return returnString;
        }

        public static string ReadString(byte[] data, int offset)
        {
            if (offset >= data.Length) return null;
            while (data[offset] == 0) offset++;
            int length = Array.IndexOf(data, (byte)0, offset) - offset;
            return ReadString(data, offset, length);
        }

        public static string ReadString(byte[] data, int offset, int length)
        {
            if (offset >= data.Length) return null;
            while (data[offset] == 0) offset++;
            byte[] tempBuffer = new Byte[length + 1];
            Buffer.BlockCopy(data, offset, tempBuffer, 0, length);
            return Encoding.GetEncoding(1251).GetString(tempBuffer, 0, Array.IndexOf(tempBuffer, (byte)0));
        }

        public static byte[] ReadN(byte[] data, int offset, int count)
        {
            byte[] ret = new byte[count];
            Array.Copy(data, offset, ret, 0, count);
            return ret;
        }

        public static float ReadFloat(byte[] data, int offset)
        {
            return ConvertIEEE754Float((uint) Read32(data, offset));
        }

        public static Vector3 ReadVector3(byte[] data, int offset)
        {
            Vector3 result = new Vector3();
            result.X = ReadFloat(data, offset);
            result.Y = ReadFloat(data, offset + 4);
            result.Z = ReadFloat(data, offset + 8);

            return result;
        }

        public static HalfRotation ReadHalfRot(byte[] data, uint offset)
        {
            HalfRotation rot = new HalfRotation();
            rot.X = Read16(data, (int) offset + 0x0);
            rot.Y = Read16(data, (int)offset + 0x2);
            rot.Z = Read16(data, (int)offset + 0x4);

            return rot;
        }

        public static float ConvertIEEE754Float(UInt32 Raw)
        {
            byte[] data = new byte[4];
            for (int i = 0; i < 4; i++)
                data[i] = (byte)((Raw >> (i * 8)) & 0xFF);
            return BitConverter.ToSingle(data, 0);
        }
        #endregion

        public static void Swap(ref byte b1, ref byte b2)
        {
            byte tmp = b1; b1 = b2; b2 = tmp;
        }

        #region Writing
        public static void Write8(BinaryWriter bWriter, byte value)
        {
            bWriter.Write(value);
        }

        public static void Write16(BinaryWriter bWriter, ushort value)
        {
            ushort swappedValue = (ushort) ((value & 0XFFU) << 8 | (value & 0xFF00U) >> 8);
            bWriter.Write(swappedValue);
        }

        public static void Write32(BinaryWriter bWriter, int value)
        {
            int swappedValue = (int) ((value & 0x000000FFU) << 24 | (value & 0x0000FF00U) << 8 |
                                      (value & 0x00FF0000U) >> 8 | (value & 0xFF000000U) >> 24);
            bWriter.Write(swappedValue);
        }

        public static void WriteString(BinaryWriter bWriter, string str, int length)
        {
            byte[] stringAsBytes = new byte[length];

            for (int i = 0; i < length; i++)
            {
                if (i < str.Length)
                {
                    stringAsBytes[i] = (byte) str[i];
                }
                else
                {
                    stringAsBytes[i] = 0;
                }
            }

            bWriter.Write(stringAsBytes);
        }

        public static void WriteArray(BinaryWriter binaryWriter, byte[] value)
        {
            binaryWriter.Write(value);
        }

        public static void WriteFloat(BinaryWriter binaryWriter, float value)
        {
            byte[] reversed = BitConverter.GetBytes(value);
            Array.Reverse(reversed);

            binaryWriter.Write(reversed);
        }

        public static void WriteVector(BinaryWriter binaryWriter, Vector3 value)
        {
            WriteFloat(binaryWriter, value.X);
            WriteFloat(binaryWriter, value.Y);
            WriteFloat(binaryWriter, value.Z);
        }
        #endregion

        /// <summary>
        /// Used to easily convert "0xFFFFFF" into 3 bytes, each with the value of FF.
        /// </summary>
        /// <param name="value">Value of bytes in Hexadecimal format, ie: 0xFF or 0xFF00FF</param>
        /// <param name="length">Number of bytes in length, ie: 1 or 3.</param>
        /// <returns>The first "length" worth of bytes when converted to an int. </returns>
        public static byte[] ToBytes(uint value, int length)
        {
            byte[] fullLength = BitConverter.GetBytes(value);

            byte[] clippedBytes = new byte[length];
            for (int i = 0; i < length; i++)
                clippedBytes[i] = fullLength[i];

            //If we're running on a Little Endian machine (most of them...) we need to reverse the Array order
            //So that we don't turn 0x800000 into 0 0 128, but instead 128 0 0. 
            if (BitConverter.IsLittleEndian)
                Array.Reverse(clippedBytes);

            return clippedBytes;
        }
    }
}
