namespace MaMoVM.Runtime.VCalls
{
    internal interface IVCall
    {
        [VMProtect.BeginMutation]
        byte Code
        {
            get;
        }

        [VMProtect.BeginMutation]
        void Run(Execution.VMContext ctx, out Execution.ExecutionState state);
    }
}