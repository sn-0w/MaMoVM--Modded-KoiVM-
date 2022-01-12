#region

using MaMoVM.Confuser.Core.AST.IL;
using MaMoVM.Confuser.Core.AST.IR;
using MaMoVM.Confuser.Core.VMIR;

#endregion

namespace MaMoVM.Confuser.Core.VMIL.Translation
{
    public class CallHandler : ITranslationHandler
    {
        public IROpCode IRCode => IROpCode.CALL;

        public void Translate(IRInstruction instr, ILTranslator tr)
        {
            tr.PushOperand(instr.Operand1);
            tr.Instructions.Add(new ILInstruction(ILOpCode.CALL) {Annotation = instr.Annotation});
        }
    }

    public class RetHandler : ITranslationHandler
    {
        public IROpCode IRCode => IROpCode.RET;

        public void Translate(IRInstruction instr, ILTranslator tr)
        {
            tr.Instructions.Add(new ILInstruction(ILOpCode.RET));
        }
    }
}