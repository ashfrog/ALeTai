using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CRC_16
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            GenerateCrCHex();
        }

        private void GenerateCrCHex()
        {
            string str = textBox1.Text;
            // 使用正则表达式提取有效的十六进制字符
            string hexStr = Regex.Replace(str, @"[^0-9A-Fa-f]", "");
            if (string.IsNullOrWhiteSpace(hexStr))
            {
                MessageBox.Show("请输入16进制字符 0-9 A-F");
                return;
            }
            if (hexStr.Length % 2 != 0)
            {
                MessageBox.Show("输入的十六进制字符长度必须是偶数。");
                return;
            }
            byte[] data = CRC.HexStringToByteArray(hexStr);
            byte[] dataAndCrc = CRC.GetCRCDatas(data);
            string hexstr = CRC.ByteArrayToHexString(dataAndCrc);
            textBox2.Text = checkBox1.Checked ? AddSpacesToHexString(hexstr) : hexstr;
            Clipboard.SetText(textBox2.Text);
            label3.Text = "已复制到剪切板";
        }

        public static string AddSpacesToHexString(string hexString)
        {
            return string.Join(" ", Enumerable.Range(0, hexString.Length / 2)
                .Select(i => hexString.Substring(i * 2, 2)));
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            GenerateCrCHex();
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            label3.Text = "";
        }
    }
}