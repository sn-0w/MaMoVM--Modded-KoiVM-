using System;
using System.Reflection;
using System.Reflection.Emit;

namespace MaMoVM.Runtime.Execution.Internal
{
    internal static class Unverifier
    {
        public static readonly Module Module;

        [VMProtect.BeginUltra]
        static Unverifier()
        {
            string A = VMProtect.SDK.DecryptString("System.Data.Common.dll");
            string B = VMProtect.SDK.DecryptString("System.Security.Cryptography.ProtectedData.dll");
            var assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName(A), AssemblyBuilderAccess.Run);
            Module = assemblyBuilder.DefineDynamicModule(B).DefineType(Convert.ToUInt32(32).ToString()).CreateType().Module;
        }
    }
}