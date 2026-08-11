using System;
using System.Text;

public class CRC
{
    /// <summary>
    /// 判断数据中crc是否正确
    /// </summary>
    /// <param name="datas">传入的数据后两位是crc</param>
    /// <returns></returns>
    public static bool IsCrcOK(byte[] datas)
    {
        if (datas == null || datas.Length < 2)
        {
            return false;
        }
        int length = datas.Length - 2;
        byte[] bytes = new byte[length];
        Array.Copy(datas, 0, bytes, 0, length);
        byte[] getCrc = GetModbusCrc16(bytes);
        if (getCrc[0] == datas[length] && getCrc[1] == datas[length + 1])
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    /// <summary>
    /// 传入数据添加两位crc
    /// </summary>
    /// <param name="datas"></param>
    /// <returns></returns>
    public static byte[] GetCRCDatas(byte[] datas)
    {
        int length = datas.Length;
        byte[] crc16 = GetModbusCrc16(datas);
        byte[] crcDatas = new byte[length + 2];
        Array.Copy(datas, crcDatas, length);
        Array.Copy(crc16, 0, crcDatas, length, 2);
        return crcDatas;
    }

    /// <summary>
    /// modbuscrc16
    /// </summary>
    /// <param name="bytes"></param>
    /// <returns></returns>
    public static byte[] GetModbusCrc16(byte[] bytes)
    {
        byte crcRegister_H = 0xFF, crcRegister_L = 0xFF;// 预置一个值为 0xFFFF 的 16 位寄存器
        byte polynomialCode_H = 0xA0, polynomialCode_L = 0x01;// 多项式码 0xA001
        for (int i = 0; i < bytes.Length; i++)
        {
            crcRegister_L = (byte)(crcRegister_L ^ bytes[i]);
            for (int j = 0; j < 8; j++)
            {
                byte tempCRC_H = crcRegister_H;
                byte tempCRC_L = crcRegister_L;
                crcRegister_H = (byte)(crcRegister_H >> 1);
                crcRegister_L = (byte)(crcRegister_L >> 1);
                // 高位右移前最后 1 位应该是低位右移后的第 1 位：如果高位最后一位为 1 则低位右移后前面补 1
                if ((tempCRC_H & 0x01) == 0x01)
                {
                    crcRegister_L = (byte)(crcRegister_L | 0x80);
                }
                if ((tempCRC_L & 0x01) == 0x01)
                {
                    crcRegister_H = (byte)(crcRegister_H ^ polynomialCode_H);
                    crcRegister_L = (byte)(crcRegister_L ^ polynomialCode_L);
                }
            }
        }
        return new byte[] { crcRegister_L, crcRegister_H };
    }

    /// <summary>
    /// 传入16进制字符串，添加两位CRC16校验，并返回16进制字符串
    /// </summary>
    /// <param name="hexString">16进制字符串</param>
    /// <returns>带有CRC16校验的16进制字符串</returns>
    public static string GetCRCHexString(string hexString)
    {
        if (string.IsNullOrEmpty(hexString))
        {
            return string.Empty;
        }

        // 移除所有空格
        hexString = hexString.Replace(" ", "");

        // 检查是否为有效的16进制字符串
        if (!IsValidHexString(hexString))
        {
            throw new ArgumentException("输入的不是有效的16进制字符串");
        }

        // 将16进制字符串转换为字节数组
        byte[] bytes = HexStringToByteArray(hexString);

        // 计算CRC并获取带有CRC的字节数组
        byte[] crcBytes = GetCRCDatas(bytes);

        // 将带有CRC的字节数组转换回16进制字符串
        string result = ByteArrayToHexString(crcBytes);

        return result;
    }

    /// <summary>
    /// 检查输入是否为有效的16进制字符串
    /// </summary>
    /// <param name="hexString">16进制字符串</param>
    /// <returns>是否有效</returns>
    private static bool IsValidHexString(string hexString)
    {
        // 16进制字符串长度应为偶数
        if (hexString.Length % 2 != 0)
        {
            return false;
        }

        // 检查是否只包含有效的16进制字符
        foreach (char c in hexString)
        {
            if (!((c >= '0' && c <= '9') || (c >= 'A' && c <= 'F') || (c >= 'a' && c <= 'f')))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 将16进制字符串转换为字节数组
    /// </summary>
    /// <param name="hexString">16进制字符串</param>
    /// <returns>字节数组</returns>
    private static byte[] HexStringToByteArray(string hexString)
    {
        int length = hexString.Length;
        byte[] bytes = new byte[length / 2];
        for (int i = 0; i < length; i += 2)
        {
            bytes[i / 2] = Convert.ToByte(hexString.Substring(i, 2), 16);
        }
        return bytes;
    }

    /// <summary>
    /// 将字节数组转换为16进制字符串
    /// </summary>
    /// <param name="bytes">字节数组</param>
    /// <returns>16进制字符串</returns>
    private static string ByteArrayToHexString(byte[] bytes)
    {
        StringBuilder sb = new StringBuilder();
        foreach (byte b in bytes)
        {
            sb.Append(b.ToString("X2"));
        }
        return sb.ToString();
    }

    /// <summary>
    /// 验证16进制字符串的CRC16校验是否正确
    /// </summary>
    /// <param name="hexString">带有CRC校验的16进制字符串</param>
    /// <returns>校验是否正确</returns>
    public static bool IsHexStringCrcOK(string hexString)
    {
        if (string.IsNullOrEmpty(hexString) || hexString.Length < 4)
        {
            return false;
        }

        // 移除所有空格
        hexString = hexString.Replace(" ", "");

        // 检查是否为有效的16进制字符串
        if (!IsValidHexString(hexString))
        {
            throw new ArgumentException("输入的不是有效的16进制字符串");
        }

        // 将16进制字符串转换为字节数组
        byte[] bytes = HexStringToByteArray(hexString);

        // 验证CRC
        return IsCrcOK(bytes);
    }
}