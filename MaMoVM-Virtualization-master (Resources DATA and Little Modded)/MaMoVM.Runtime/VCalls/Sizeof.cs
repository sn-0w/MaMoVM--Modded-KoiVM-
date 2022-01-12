using System;
using MaMoVM.Runtime.Dynamic;
using MaMoVM.Runtime.Execution;
using MaMoVM.Runtime.Execution.Internal;

namespace MaMoVM.Runtime.VCalls
{
    internal class Sizeof : IVCall
    {
        public byte Code => Constants.VCALL_SIZEOF;

        [VMProtect.BeginMutation]
        public void Run(VMContext ctx, out ExecutionState state)
        {
            var sp = ctx.Registers[Constants.REG_SP].U4;
            var bp = ctx.Registers[Constants.REG_BP].U4;
            var type = (Type) ctx.Instance.Data.LookupReference(ctx.Stack[sp].U4);
            ctx.Stack[sp] = new VMSlot
            {
                U4 = (uint) SizeOfHelper.SizeOf(type)
            };

            state = ExecutionState.Next;
        }
    }
}