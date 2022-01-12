#region

using MaMoVM.Confuser.Core.AST.IR;
using MaMoVM.Confuser.Core.VMIR;

#endregion

namespace MaMoVM.Confuser.Core.VMIL
{
    public interface ITranslationHandler
    {
        IROpCode IRCode
        {
            get;
        }

        void Translate(IRInstruction instr, ILTranslator tr);
    }
}