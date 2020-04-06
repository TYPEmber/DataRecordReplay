using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace DRRCommon
{
    public static class StructBytes
    {
        /// <summary>
        /// 转换byte[]为结构体
        /// </summary>
        /// <param name="buf"></param>
        /// <param name="type"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        public static unsafe object BytesToStructure(byte[] buf, Type type, int offset = 0)
        {
            try
            {
                fixed (byte* p = buf)
                {
                    return Marshal.PtrToStructure(new IntPtr(p + offset), type);
                }
            }
            catch (Exception e)
            {

                throw e;
            }
        }

        public static unsafe Byte[] ConvertStructToBytes<T>(T structObj) where T : struct
        {
            int structSize = Marshal.SizeOf(structObj);
            byte[] bytes = new byte[structSize];
            fixed (byte* pbytes = &bytes[0])
            {
                Marshal.StructureToPtr(structObj, new IntPtr(pbytes), true);
            }
            return bytes;
        }

        public static unsafe Byte[] ConvertStructToBytes(object structObj)
        {
            int structSize = Marshal.SizeOf(structObj);
            byte[] bytes = new byte[structSize];
            fixed (byte* pbytes = &bytes[0])
            {
                Marshal.StructureToPtr(structObj, new IntPtr(pbytes), true);
            }
            return bytes;
        }
    }
}
