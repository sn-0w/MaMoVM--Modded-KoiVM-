using System;
using System.Reflection;
using MaMoVM.Runtime.Dynamic;
using MaMoVM.Runtime.Execution;
using MaMoVM.Runtime.Execution.Internal;

namespace MaMoVM.Runtime.VCalls
{
    internal class Token : IVCall
    {
        public byte Code => Constants.VCALL_TOKEN;

        [VMProtect.BeginMutation]
        public void Run(VMContext ctx, out ExecutionState state)
        {
            var sp = ctx.Registers[Constants.REG_SP].U4;
            var typeSlot = ctx.Stack[sp];

            var reference = ctx.Instance.Data.LookupReference(typeSlot.U4);
            if(reference is Type)
                typeSlot.O = ValueTypeBox.Box(((Type) reference).TypeHandle, typeof(RuntimeTypeHandle));
            else if(reference is MethodBase)
                typeSlot.O = ValueTypeBox.Box(((MethodBase) reference).MethodHandle, typeof(RuntimeMethodHandle));
            else if(reference is FieldInfo)
                typeSlot.O = ValueTypeBox.Box(((FieldInfo) reference).FieldHandle, typeof(RuntimeFieldHandle));
            ctx.Stack[sp] = typeSlot;

            state = ExecutionState.Next;
        }
    }
}