#region

using System;
using System.Linq;
using MaMoVM.Confuser.Core.VMIL;

#endregion

namespace MaMoVM.Confuser.Core.VM
{
    public class OpCodeDescriptor
    {
        private readonly byte[] opCodeOrder = Enumerable.Range(0, 256).Select(x => (byte) x).ToArray();

        public OpCodeDescriptor(Random random)
        {
            random.Shuffle(opCodeOrder);
        }

        public byte this[ILOpCode opCode] => opCodeOrder[(int) opCode];
    }
}