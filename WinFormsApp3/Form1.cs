
using System;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

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

                textBox1.Text += ($"Bytes:            {HexDump(test)}\r\n");
                textBox1.Text += ($"Double Val:       {doubleVal}\r\n");
                textBox1.Text += ($"Long Val (correct): {longVal}\r\n");
                textBox1.Text += ("\r\n");
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            textBox1.Text = "";
            erros();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            textBox1.Text = "";
            // 正の値のサンプル: 0x12 34 56 78 9A BC DE FF
            byte[] positiveArray = new byte[] { 0x12, 0x34, 0x56, 0x78, 0x9A, 0xBC, 0xDE, 0xFF };
            ProcessByteArray(positiveArray, "正の値");

            // 負の値のサンプル: 0xFF FF FF FF FF FF FF FF（-1）
            byte[] negativeArray = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
            ProcessByteArray(negativeArray, "負の値（-1）");

            // もう一つの負の値のサンプル: 0x80 00 00 00 00 00 00 00（-2^63）
            byte[] negativeArray2 = new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
            ProcessByteArray(negativeArray2, "負の値（-2^63）");
        }
        private void ProcessByteArray(byte[] byteArray, string label)
        {
            // バイト列からdoubleを計算
            double byteDouble = 0;
            foreach (byte b in byteArray)
            {
                byteDouble = b + byteDouble * 256; // 指定された計算方法
            }

            // 64ビットlongとして解釈（符号付き）
            long longValue = (long)byteDouble;

            // 52ビットの最大値（2^53 - 1）と最小値（-2^53）を考慮
            long max52BitValue = (1L << 53) - 1; // 9,007,199,254,740,991
            long min52BitValue = -(1L << 53);    // -9,007,199,254,740,992

            // 52ビット範囲に収まるように制限
            if (longValue > max52BitValue)
            {
                longValue = max52BitValue;
            }
            else if (longValue < min52BitValue)
            {
                longValue = min52BitValue;
            }

            // TextBox1に結果を追加（改行で区切る）
            textBox1.Text += $"{label}: {((double)longValue).ToString("N0")}\r\n";
        }

        private void button3_Click(object sender, EventArgs e)
        {
            byte[] inputArray = new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

            // バイト単位で絶対値を取る処理
            byte[] resultArray = new byte[8];
            bool isNegative = (inputArray[0] & 0x80) != 0; // 最上位バイトのMSBで負数を判定

            if (isNegative)
            {
                // 2の補数の否定をバイト単位で計算（反転＋1）
                for (int i = 0; i < 8; i++)
                {
                    resultArray[i] = (byte)(~inputArray[i]); // バイトごとの反転
                }
                // +1をバイト単位で加算
                int carry = 1;
                for (int i = 7; i >= 0; i--)
                {
                    int sum = resultArray[i] + carry;
                    resultArray[i] = (byte)(sum & 0xFF);
                    carry = sum >> 8;
                }
            }
            else
            {
                // 正の値ならそのままコピー
                Array.Copy(inputArray, resultArray, 8);
            }

            // 結果をlongとして解釈（確認用）
            long resultValue = BitConverter.ToInt64(resultArray, 0);

            // TextBox1に結果を表示
            textBox1.Text = $"入力: {BitConverter.ToInt64(inputArray, 0):N0}\r\n" +
                            $"絶対値 (2^63): {resultValue:N0}\r\n" +
                            $"バイト列: {BitConverter.ToString(resultArray)}";
        }

        private void button4_Click(object sender, EventArgs e)
        {
            textBox1.Clear();

            // テストケース
            string[] testInputs = new[]
            {
                "80 00 00 00 00 00 00 00", // -2^63
                "12 34 56 78 9A BC DE FF", // 大きな正数
                "FF FF FF FF FF FF FF FF", // -1
                "7F FF FF FF FF FF FF FF"  // 2^63 - 1
            };

            foreach (string input in testInputs)
            {
                ProcessByteString(input);
                textBox1.AppendText(Environment.NewLine); // テストケース間に空行
            }
        }

        private void ProcessByteString(string input)
        {
            textBox1.AppendText($"入力: {input}\r\n");

            // 1. 16進数文字列をバイト配列に変換
            byte[] bytes = input.Split(' ')
                               .Select(hex => Convert.ToByte(hex, 16))
                               .ToArray();

            // 2. バイト列全体をlong値に変換（ビッグエンディアン）
            byte[] littleEndianBytes = bytes.Reverse().ToArray();
            long value = BitConverter.ToInt64(littleEndianBytes, 0);

            // 3. ビット反転 (NOT + 1)
            long notPlusOne = ~value + 1;

            // 4. 52ビット範囲に制限
            long max52Bit = (1L << 53) - 1; // 2^53 - 1 = 9,007,199,254,740,991
            long min52Bit = -(1L << 53);    // -2^53 = -9,007,199,254,740,992
            long restrictedNotPlusOne = notPlusOne;
            if (restrictedNotPlusOne > max52Bit) restrictedNotPlusOne = max52Bit;
            if (restrictedNotPlusOne < min52Bit) restrictedNotPlusOne = min52Bit;

            // 5. バイト列に変換して16進数文字列に
            byte[] resultBytes = BitConverter.GetBytes(restrictedNotPlusOne).Reverse().ToArray();
            string notPlusOneHex = BitConverter.ToString(resultBytes).Replace("-", " ");

            // 6. 全体に+1
            long plusOneValue = restrictedNotPlusOne + 1;
            if (plusOneValue > max52Bit) plusOneValue = max52Bit;
            if (plusOneValue < min52Bit) plusOneValue = min52Bit;
            byte[] plusOneBytes = BitConverter.GetBytes(plusOneValue).Reverse().ToArray();
            string plusOneHex = BitConverter.ToString(plusOneBytes).Replace("-", " ");

            // TextBox1に出力
            textBox1.AppendText($"NOT + 1 (52bit制限): {notPlusOneHex} ({restrictedNotPlusOne:N0})\r\n");
            textBox1.AppendText($"NOT + 1 + 1 (52bit制限): {plusOneHex} ({plusOneValue:N0})\r\n");
        }
    }
}

