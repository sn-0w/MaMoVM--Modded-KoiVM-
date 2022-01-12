using dnlib.DotNet;
using MaMoVM.Confuser.Core.AST;
using MaMoVM.Confuser.Core.AST.IL;
using MaMoVM.Confuser.Core.RT;

namespace MaMoVM.Confuser.Core.Protections.SMC
{
    internal class SMCBlock : ILBlock
    {
        internal static readonly InstrAnnotation CounterInit = new InstrAnnotation("SMC_COUNTER");
        internal static readonly InstrAnnotation EncryptionKey = new InstrAnnotation("SMC_KEY");
        internal static readonly InstrAnnotation AddressPart1 = new InstrAnnotation("SMC_PART1");
        internal static readonly InstrAnnotation AddressPart2 = new InstrAnnotation("SMC_PART2");

        public SMCBlock(int id, ILInstrList content)
            : base(id, content)
        {
        }

        public byte Key
        {
            get;
            set;
        }

        public ILImmediate CounterOperand
        {
            get;
            set;
        }

        public override IVMChunk CreateChunk(VMRuntime rt, MethodDef method)
        {
            return new SMCBlockChunk(rt, method, this);
        }
    }

    internal class SMCBlockChunk : BasicBlockChunk, IVMChunk
    {
        public SMCBlockChunk(VMRuntime rt, MethodDef method, SMCBlock block)
            : base(rt, method, block)
        {
            block.CounterOperand.Value = Length + 1;
        }

        uint IVMChunk.Length => base.Length + 1;

        void IVMChunk.OnOffsetComputed(uint offset)
        {
            base.OnOffsetComputed(offset + 1);
        }

        byte[] IVMChunk.GetData()
        {
            var data = GetData();
            var newData = new byte[data.Length + 1];
            var key = ((SMCBlock) Block).Key;

            for(var i = 0; i < data.Length; i++)
                newData[i + 1] = (byte) (data[i] ^ key);
            newData[0] = key;
            return newData;
        }
    }

    internal class SMCBlockRef : ILRelReference
    {
        public SMCBlockRef(IHasOffset target, IHasOffset relBase, uint key)
            : base(target, relBase)
        {
            Key = key;
        }

        public uint Key
        {
            get;
            set;
        }

        public override uint Resolve(VMRuntime runtime)
        {
            return base.Resolve(runtime) ^ Key;
        }
    }
}