using MaMoVM.Runtime.Execution;

namespace MaMoVM.Runtime.OpCodes
{
    internal interface IOpCode
    {
        byte Code
        {
            get;
        }

        void Run(VMContext ctx, out ExecutionState state);
    }
}