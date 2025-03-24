
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

        //https://grok.com/chat/0fc048f3-dbc4-4189-a285-fc137fcfca9a �O���b�N����̉񓚂��Q�l�ɂ��܂���
        //
        // �r�b�O�G���f�B�A������64�r�b�g������double�Ńp�[�X�i�Ӑ}�I�ɐ��x�����Č��j
        static double ParseBigEndianDouble(byte[] bytes)
        {
            if (bytes.Length != 8) throw new ArgumentException("Input must be 8 bytes");
            double result = 0;
            for (int i = 0; i < 8; i++)
            {
                result = result * 256 + bytes[i]; // �o�C�g���ƂɃV�t�g�idouble�Ōv�Z�j
            }
            // �����t�������ɕϊ��iLua�Ɠ��l�̃��W�b�N�j
            if (result >= Math.Pow(2, 63))
            {
                result -= Math.Pow(2, 64);
            }
            return result;
        }

        // ���m�Ȕ�r�p�Flong�Ńp�[�X
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

        // �o�C�g���16�i���ŕ\��
        static string HexDump(byte[] bytes)
        {
            return BitConverter.ToString(bytes).Replace("-", " ");
        }

        public void erros()
        {
            // �e�X�g�f�[�^
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
            // ���̒l�̃T���v��: 0x12 34 56 78 9A BC DE FF
            byte[] positiveArray = new byte[] { 0x12, 0x34, 0x56, 0x78, 0x9A, 0xBC, 0xDE, 0xFF };
            ProcessByteArray(positiveArray, "���̒l");

            // ���̒l�̃T���v��: 0xFF FF FF FF FF FF FF FF�i-1�j
            byte[] negativeArray = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
            ProcessByteArray(negativeArray, "���̒l�i-1�j");

            // ������̕��̒l�̃T���v��: 0x80 00 00 00 00 00 00 00�i-2^63�j
            byte[] negativeArray2 = new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
            ProcessByteArray(negativeArray2, "���̒l�i-2^63�j");
        }
        private void ProcessByteArray(byte[] byteArray, string label)
        {
            // �o�C�g�񂩂�double���v�Z
            double byteDouble = 0;
            foreach (byte b in byteArray)
            {
                byteDouble = b + byteDouble * 256; // �w�肳�ꂽ�v�Z���@
            }

            // 64�r�b�glong�Ƃ��ĉ��߁i�����t���j
            long longValue = (long)byteDouble;

            // 52�r�b�g�̍ő�l�i2^53 - 1�j�ƍŏ��l�i-2^53�j���l��
            long max52BitValue = (1L << 53) - 1; // 9,007,199,254,740,991
            long min52BitValue = -(1L << 53);    // -9,007,199,254,740,992

            // 52�r�b�g�͈͂Ɏ��܂�悤�ɐ���
            if (longValue > max52BitValue)
            {
                longValue = max52BitValue;
            }
            else if (longValue < min52BitValue)
            {
                longValue = min52BitValue;
            }

            // TextBox1�Ɍ��ʂ�ǉ��i���s�ŋ�؂�j
            textBox1.Text += $"{label}: {((double)longValue).ToString("N0")}\r\n";
        }

        private void button3_Click(object sender, EventArgs e)
        {
            byte[] inputArray = new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

            // �o�C�g�P�ʂŐ�Βl����鏈��
            byte[] resultArray = new byte[8];
            bool isNegative = (inputArray[0] & 0x80) != 0; // �ŏ�ʃo�C�g��MSB�ŕ����𔻒�

            if (isNegative)
            {
                // 2�̕␔�̔ے���o�C�g�P�ʂŌv�Z�i���]�{1�j
                for (int i = 0; i < 8; i++)
                {
                    resultArray[i] = (byte)(~inputArray[i]); // �o�C�g���Ƃ̔��]
                }
                // +1���o�C�g�P�ʂŉ��Z
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
                // ���̒l�Ȃ炻�̂܂܃R�s�[
                Array.Copy(inputArray, resultArray, 8);
            }

            // ���ʂ�long�Ƃ��ĉ��߁i�m�F�p�j
            long resultValue = BitConverter.ToInt64(resultArray, 0);

            // TextBox1�Ɍ��ʂ�\��
            textBox1.Text = $"����: {BitConverter.ToInt64(inputArray, 0):N0}\r\n" +
                            $"��Βl (2^63): {resultValue:N0}\r\n" +
                            $"�o�C�g��: {BitConverter.ToString(resultArray)}";
        }

        private void button4_Click(object sender, EventArgs e)
        {
            textBox1.Clear();

            // �e�X�g�P�[�X
            string[] testInputs = new[]
            {
                "80 00 00 00 00 00 00 00", // -2^63
                "12 34 56 78 9A BC DE FF", // �傫�Ȑ���
                "FF FF FF FF FF FF FF FF", // -1
                "7F FF FF FF FF FF FF FF"  // 2^63 - 1
            };

            foreach (string input in testInputs)
            {
                ProcessByteString(input);
                textBox1.AppendText(Environment.NewLine); // �e�X�g�P�[�X�Ԃɋ�s
            }
        }

        private void ProcessByteString(string input)
        {
            textBox1.AppendText($"����: {input}\r\n");

            // 1. 16�i����������o�C�g�z��ɕϊ�
            byte[] bytes = input.Split(' ')
                               .Select(hex => Convert.ToByte(hex, 16))
                               .ToArray();

            // 2. �o�C�g��S�̂�long�l�ɕϊ��i�r�b�O�G���f�B�A���j
            byte[] littleEndianBytes = bytes.Reverse().ToArray();
            long value = BitConverter.ToInt64(littleEndianBytes, 0);

            // 3. �r�b�g���] (NOT + 1)
            long notPlusOne = ~value + 1;

            // 4. 52�r�b�g�͈͂ɐ���
            long max52Bit = (1L << 53) - 1; // 2^53 - 1 = 9,007,199,254,740,991
            long min52Bit = -(1L << 53);    // -2^53 = -9,007,199,254,740,992
            long restrictedNotPlusOne = notPlusOne;
            if (restrictedNotPlusOne > max52Bit) restrictedNotPlusOne = max52Bit;
            if (restrictedNotPlusOne < min52Bit) restrictedNotPlusOne = min52Bit;

            // 5. �o�C�g��ɕϊ�����16�i���������
            byte[] resultBytes = BitConverter.GetBytes(restrictedNotPlusOne).Reverse().ToArray();
            string notPlusOneHex = BitConverter.ToString(resultBytes).Replace("-", " ");

            // 6. �S�̂�+1
            long plusOneValue = restrictedNotPlusOne + 1;
            if (plusOneValue > max52Bit) plusOneValue = max52Bit;
            if (plusOneValue < min52Bit) plusOneValue = min52Bit;
            byte[] plusOneBytes = BitConverter.GetBytes(plusOneValue).Reverse().ToArray();
            string plusOneHex = BitConverter.ToString(plusOneBytes).Replace("-", " ");

            // TextBox1�ɏo��
            textBox1.AppendText($"NOT + 1 (52bit����): {notPlusOneHex} ({restrictedNotPlusOne:N0})\r\n");
            textBox1.AppendText($"NOT + 1 + 1 (52bit����): {plusOneHex} ({plusOneValue:N0})\r\n");
        }
    }
}

