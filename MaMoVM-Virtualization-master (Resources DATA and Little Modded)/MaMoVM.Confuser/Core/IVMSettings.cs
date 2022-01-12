namespace MaMoVM.Confuser.Core
{
    public interface IVMSettings
    {
        int Seed
        {
            get;
        }

        bool IsDebug
        {
            get;
        }

        bool ExportDbgInfo
        {
            get;
        }

        bool DoStackWalk
        {
            get;
        }

        bool IsVirtualized(dnlib.DotNet.MethodDef method);
        bool IsExported(dnlib.DotNet.MethodDef method);
    }
}