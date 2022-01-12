using System;
using MaMoVM.Runtime.Dynamic;
using MaMoVM.Runtime.Execution;

namespace MaMoVM.Runtime.VCalls
{
    internal class Ckoverflow : IVCall
    {
        public byte Code => Constants.VCALL_CKOVERFLOW;

        [VMProtect.BeginMutation]
        public void Run(VMContext ctx, out ExecutionState state)
        {
            var sp = ctx.Registers[Constants.REG_SP].U4;
            var fSlot = ctx.Stack[sp--];

            if(fSlot.U4 != 0)
                throw new OverflowException();

            ctx.Stack.SetTopPosition(sp);
            ctx.Registers[Constants.REG_SP].U4 = sp;
            state = ExecutionState.Next;
        }
    }
}