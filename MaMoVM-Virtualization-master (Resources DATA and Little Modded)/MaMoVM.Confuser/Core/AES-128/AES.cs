using System;
using System.Collections.Generic;

namespace MaMoVM.Confuser.Core.AES128
{
    internal class AES
    {
        delegate int positionArray(int round, int column, int row);
        delegate int positionArray4(int column, int row);

        private static int _countRound = 11;

        private static byte[] _rcon = { 0x00, 0x01, 0x02, 0x04, 0x08, 0x10, 0x20, 0x40, 0x80, 0x1b, 0x36 };

        private byte[] _key;

        private static byte GMul(byte a, byte b)
        {
            byte result = 0;
            byte hi_bit_set;

            for (var counter = 0; counter < 8; counter++)
            {
                if ((b & 0x01) != 0)
                    result ^= a;

                hi_bit_set = (byte)(a & 0x80);

                a <<= 1;

                if (hi_bit_set != 0)
                    a ^= 0x1b;

                b >>= 1;
            }
            return result;
        }

        public static byte SubBytes(byte input)
        {
            return AESBOX.SBOX[((input & 0xF0) >> 4) * 16 + (input & 0x0F)];
        }
        public static byte InvSubBytes(byte input)
        {
            return AESBOX.InvSBOX[((input & 0xF0) >> 4) * 16 + (input & 0x0F)];
        }

        public static byte[] ShiftRows(byte[] input)
        {
            byte[] changeArray = new byte[4 * 4];
            for (var row = 0; row < 4; row++)
            {
                Array.Copy(input, 4 * row + row, changeArray, 4 * row, 4 - row);
            }
            for (var row = 1; row < 4; row++)
            {
                Array.Copy(input, 4 * row, changeArray, 4 * row + 4 - row, row);
            }

            return changeArray;
        }
        public static byte[] InvShiftRows(byte[] input)
        {
            byte[] changeArray = new byte[4 * 4];
            for (var row = 0; row < 4; row++)
            {
                Array.Copy(input, 4 * row, changeArray, 4 * row + row, 4 - row);
            }
            for (var row = 1; row < 4; row++)
            {
                Array.Copy(input, 4 * row + 4 - row, changeArray, 4 * row, row);
            }
            return changeArray;
        }

        public static byte[] MixColumns(byte[] input)
        {
            byte[] changeArray = new byte[4 * 4];

            positionArray4 index = (c, r) => (c + 4 * r);


            for (var column = 0; column < 4; column++)
            {
                changeArray[index(column, 0)] = (byte)(
                    GMul(input[index(column, 0)], 0x02) ^
                    GMul(input[index(column, 1)], 0x03) ^
                    input[index(column, 2)] ^
                    input[index(column, 3)]
                    );

                changeArray[index(column, 1)] = (byte)(
                    GMul(input[index(column, 1)], 0x02) ^
                    GMul(input[index(column, 2)], 0x03) ^
                    input[index(column, 0)] ^
                    input[index(column, 3)]
                    );

                changeArray[index(column, 2)] = (byte)(
                    GMul(input[index(column, 2)], 0x02) ^
                    GMul(input[index(column, 3)], 0x03) ^
                    input[index(column, 0)] ^
                    input[index(column, 1)]
                    );

                changeArray[index(column, 3)] = (byte)(
                    GMul(input[index(column, 0)], 0x03) ^
                    GMul(input[index(column, 3)], 0x02) ^
                    input[index(column, 1)] ^
                    input[index(column, 2)]
                    );

            }

            return changeArray;

        }

        public static byte[] InvMixColumns(byte[] input)
        {
            byte[] changeArray = new byte[4 * 4];

            positionArray4 index = (c, r) => (c + 4 * r);


            for (var column = 0; column < 4; column++)
            {
                changeArray[index(column, 0)] = (byte)(
                    GMul(input[index(column, 0)], 0x0E) ^
                    GMul(input[index(column, 1)], 0x0B) ^
                    GMul(input[index(column, 2)], 0x0D) ^
                    GMul(input[index(column, 3)], 0x09)
                    );

                changeArray[index(column, 1)] = (byte)(
                    GMul(input[index(column, 0)], 0x09) ^
                    GMul(input[index(column, 1)], 0x0E) ^
                    GMul(input[index(column, 2)], 0x0B) ^
                    GMul(input[index(column, 3)], 0x0D)
                    );

                changeArray[index(column, 2)] = (byte)(
                    GMul(input[index(column, 0)], 0x0D) ^
                    GMul(input[index(column, 1)], 0x09) ^
                    GMul(input[index(column, 2)], 0x0E) ^
                    GMul(input[index(column, 3)], 0x0B)
                    );

                changeArray[index(column, 3)] = (byte)(
                    GMul(input[index(column, 0)], 0x0B) ^
                    GMul(input[index(column, 1)], 0x0D) ^
                    GMul(input[index(column, 2)], 0x09) ^
                    GMul(input[index(column, 3)], 0x0E)
                    );


            }
            return changeArray;
        }

        public static byte[] AddRoundKey(byte[] input, byte[] key)
        {
            for (var i = 0; i < input.Length; i++)
            {
                input[i] ^= key[i];
            }
            return input;
        }

        public AES(byte[] key)
        {
            this._key = key;
        }

        public byte[] KeyExpansion()
        {
            byte[] roundKey = new byte[_countRound * 16];
            Array.Copy(_key, roundKey, 16);

            positionArray index = (rd, c, r) => (16 * rd + c + 4 * r);

            for (var round = 1; round < _countRound; round++)
            {
                // Copy W(i-1) in WI 
                for (var row = 1; row < 4; row++)
                {
                    roundKey[index(round, 0, row - 1)] = SubBytes(roundKey[index(round - 1, 3, row)]);
                }

                // Rotation word
                roundKey[index(round, 0, 3)] = SubBytes(roundKey[index(round - 1, 3, 0)]);

                for (var row = 0; row < 4; row++)
                {
                    roundKey[index(round, 0, row)] ^= roundKey[index(round - 1, 0, row)];
                }

                roundKey[index(round, 0, 0)] ^= _rcon[round];


                for (var column = 1; column < 4; column++)
                {
                    for (var row = 0; row < 4; row++)
                    {
                        roundKey[index(round, column, row)] = (byte)(roundKey[index(round - 1, column, row)] ^ roundKey[index(round, column - 1, row)]);
                    }

                }

            }

            return roundKey;
        }

        public byte[] Encrypt(byte[] input)
        {
            var key = KeyExpansion();

            if (input.Length % 16 != 0)
            {
                var temp = new byte[input.Length + (16 - input.Length % 16)];
                Array.Copy(input, temp, input.Length);
                input = temp;
            }

            var output = new List<byte>();

            var listKey = new List<byte[]>();

            for (var i = 0; i < key.Length; i += 16)
            {
                var arr = new byte[16];
                Array.Copy(key, i, arr, 0, 16);
                listKey.Add(arr);
            }


            for (var state = 0; state < input.Length; state += 16)
            {

                var arrState = new byte[16];
                Array.Copy(input, state, arrState, 0, 16);

                arrState = AddRoundKey(arrState, listKey[0]);

                for (var round = 1; round < _countRound - 1; round++)
                {
                    //Array.ForEach(arrState, element => element = SubBytes(element));
                    for (var i = 0; i < arrState.Length; i++)
                        arrState[i] = SubBytes(arrState[i]);

                    arrState = ShiftRows(arrState);
                    arrState = MixColumns(arrState);
                    arrState = AddRoundKey(arrState, listKey[round]);
                }

                //Array.ForEach(arrState, element => element = SubBytes(element));
                for (var i = 0; i < arrState.Length; i++)
                    arrState[i] = SubBytes(arrState[i]);

                arrState = ShiftRows(arrState);
                arrState = AddRoundKey(arrState, listKey[_countRound - 1]);


                Array.ForEach(arrState, element => output.Add(element));

            }
            return output.ToArray();
        }
    }
}
