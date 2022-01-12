#region

using MaMoVM.Confuser.Core.AST.IL;
using MaMoVM.Confuser.Core.AST.IR;
using MaMoVM.Confuser.Core.VMIR;

#endregion

namespace MaMoVM.Confuser.Core.VMIL.Translation
{
    public class TryHandler : ITranslationHandler
    {
        public IROpCode IRCode => IROpCode.TRY;

        public void Translate(IRInstruction instr, ILTranslator tr)
        {
            if(instr.Operand2 != null)
                tr.PushOperand(instr.Operand2);
            tr.PushOperand(instr.Operand1);
            tr.Instructions.Add(new ILInstruction(ILOpCode.TRY) {Annotation = instr.Annotation});
        }
    }

    public class LeaveHandler : ITranslationHandler
    {
        public IROpCode IRCode => IROpCode.LEAVE;

        public void Translate(IRInstruction instr, ILTranslator tr)
        {
            tr.PushOperand(instr.Operand1);
            tr.Instructions.Add(new ILInstruction(ILOpCode.LEAVE) {Annotation = instr.Annotation});
        }
    }
}