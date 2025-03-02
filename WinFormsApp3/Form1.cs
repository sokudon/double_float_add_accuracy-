
using System;
using System.Windows.Forms;

namespace WinFormsApp3
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        //https://grok.com/chat/0fc048f3-dbc4-4189-a285-fc137fcfca9a グロックたんの回答を参考にしました
        //
        // ビッグエンディアンから64ビット整数をdoubleでパース（意図的に精度問題を再現）
        static double ParseBigEndianDouble(byte[] bytes)
        {
            if (bytes.Length != 8) throw new ArgumentException("Input must be 8 bytes");
            double result = 0;
            for (int i = 0; i < 8; i++)
            {
                result = result * 256 + bytes[i]; // バイトごとにシフト（doubleで計算）
            }
            // 符号付き整数に変換（Luaと同様のロジック）
            if (result >= Math.Pow(2, 63))
            {
                result -= Math.Pow(2, 64);
            }
            return result;
        }

        // 正確な比較用：longでパース
        static long ParseBigEndianLong(byte[] bytes)
        {
            if (bytes.Length != 8) throw new ArgumentException("Input must be 8 bytes");
            long result = 0;
            for (int i = 0; i < 8; i++)
            {
                result = (result << 8) | bytes[i];
            }
            return result;
        }

        // バイト列を16進数で表示
        static string HexDump(byte[] bytes)
        {
            return BitConverter.ToString(bytes).Replace("-", " ");
        }

        public void erros()
        {
            // テストデータ
            byte[][] tests = new byte[][]
            {
            new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }, // 1
            new byte[] { 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, // 2^63-1
            new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, // -2^63
            new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF },  // -1
            new byte[]{ 0xFF,0xFF, 0xFF, 0xFF, 0x9E, 0xA6, 0x48, 0xA0 }
            };

            foreach (var test in tests)
            {
                double doubleVal = ParseBigEndianDouble(test);
                long longVal = ParseBigEndianLong(test);

               textBox1.Text+=($"Bytes:            {HexDump(test)}\r\n");
                textBox1.Text += ($"Double Val:       {doubleVal}\r\n");
                textBox1.Text += ($"Long Val (correct): {longVal}\r\n");
                textBox1.Text += ("\r\n");
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            erros();
        }
    }
}
