using MaMoVM.Runtime.Dynamic;
using MaMoVM.Runtime.Execution;

namespace MaMoVM.Runtime.VCalls
{
    internal class Rangechk : IVCall
    {
        public byte Code => Constants.VCALL_RANGECHK;

        [VMProtect.BeginMutation]
        public void Run(VMContext ctx, out ExecutionState state)
        {
            var sp = ctx.Registers[Constants.REG_SP].U4;
            var valueSlot = ctx.Stack[sp--];
            var maxSlot = ctx.Stack[sp--];
            var minSlot = ctx.Stack[sp];

            valueSlot.U8 = (long) valueSlot.U8 > (long) maxSlot.U8 || (long) valueSlot.U8 < (long) minSlot.U8 ? 1u : 0;

            ctx.Stack[sp] = valueSlot;

            ctx.Stack.SetTopPosition(sp);
            ctx.Registers[Constants.REG_SP].U4 = sp;
            state = ExecutionState.Next;
        }
    }
}