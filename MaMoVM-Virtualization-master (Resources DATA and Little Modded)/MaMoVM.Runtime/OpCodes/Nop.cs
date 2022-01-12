using MaMoVM.Runtime.Dynamic;
using MaMoVM.Runtime.Execution;

namespace MaMoVM.Runtime.OpCodes
{
    internal class Nop : IOpCode
    {
        public byte Code => Constants.OP_NOP;

        public void Run(VMContext ctx, out ExecutionState state)
        {
            state = ExecutionState.Next;
        }
    }
}