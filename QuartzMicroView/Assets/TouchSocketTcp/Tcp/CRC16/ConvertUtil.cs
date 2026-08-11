using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public class ConvertUtil
{
    /// <summary>
    /// 16进制字符串转byte[] (2个字符对应 一个byte)
    /// </summary>
    /// <param name="hexString"></param>
    /// <returns></returns>
    public static byte[] HexStrTobyte(string hexString)
    {
        hexString = hexString.Replace(" ", "");
        if ((hexString.Length % 2) != 0)
            hexString += " ";
        byte[] returnBytes = new byte[hexString.Length / 2];
        for (int i = 0; i < returnBytes.Length; i++)
            returnBytes[i] = Convert.ToByte(hexString.Substring(i * 2, 2).Trim(), 16);
        return returnBytes;
    }
}

