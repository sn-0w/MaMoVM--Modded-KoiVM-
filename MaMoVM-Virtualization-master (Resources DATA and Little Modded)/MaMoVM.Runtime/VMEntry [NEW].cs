namespace MaMoVM.Runtime
{
    public class VMEntry
    {
        [VMProtect.BeginMutation]
        public static unsafe object Invoke(object[] args, void*[] typedRefs)
        {
           return VMInstance.InvokeInternal(args, typedRefs);
        }

        [VMProtect.BeginMutation]
        internal static object InvokeInternal(int moduleId, ulong codeAddr, uint key, uint sigId, object[] args)
        {
            return VMInstance.InvokeInternal(moduleId, codeAddr, key, sigId, args);
        }
    }
}