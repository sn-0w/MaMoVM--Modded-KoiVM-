using MaMoVM.Runtime.Dynamic;
using MaMoVM.Runtime.Execution;

namespace MaMoVM.Runtime.VCalls
{
    internal class Exit : IVCall
    {
        public byte Code => Constants.VCALL_EXIT;

        [VMProtect.BeginMutation]
        public void Run(VMContext ctx, out ExecutionState state)
        {
            state = ExecutionState.Exit;
        }
    }
}