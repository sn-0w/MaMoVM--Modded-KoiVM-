using System;
using System.Diagnostics;
using MaMoVM.Runtime.Dynamic;
using MaMoVM.Runtime.Execution;

namespace MaMoVM.Runtime.VCalls
{
    internal class Box : IVCall
    {
        public byte Code => Constants.VCALL_BOX;

        [VMProtect.BeginMutation]
        public void Run(VMContext ctx, out ExecutionState state)
        {
            var sp = ctx.Registers[Constants.REG_SP].U4;
            var typeSlot = ctx.Stack[sp--];
            var valSlot = ctx.Stack[sp];

            var valType = (Type) ctx.Instance.Data.LookupReference(typeSlot.U4);
            if(Type.GetTypeCode(valType) == TypeCode.String && valSlot.O == null)
            {
                valSlot.O = ctx.Instance.Data.LookupString(valSlot.U4);
            }
            else
            {
                Debug.Assert(valType.IsValueType);
                valSlot.O = valSlot.ToObject(valType);
            }
            ctx.Stack[sp] = valSlot;

            ctx.Stack.SetTopPosition(sp);
            ctx.Registers[Constants.REG_SP].U4 = sp;
            state = ExecutionState.Next;
        }
    }
}