#region

using dnlib.DotNet.Emit;
using MaMoVM.Confuser.Core.AST.ILAST;
using MaMoVM.Confuser.Core.AST.IR;

#endregion

namespace MaMoVM.Confuser.Core.VMIR
{
    public interface ITranslationHandler
    {
        Code ILCode
        {
            get;
        }

        IIROperand Translate(ILASTExpression expr, IRTranslator tr);
    }
}