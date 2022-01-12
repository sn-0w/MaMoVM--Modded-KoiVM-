#region

using dnlib.DotNet;
using MaMoVM.Confuser.Core.CFG;
using MaMoVM.Confuser.Core.RT;

#endregion

namespace MaMoVM.Confuser.Core.AST.IL
{
    public class ILBlock : BasicBlock<ILInstrList>
    {
        public ILBlock(int id, ILInstrList content)
            : base(id, content)
        {
        }

        public virtual IVMChunk CreateChunk(VMRuntime rt, MethodDef method)
        {
            return new BasicBlockChunk(rt, method, this);
        }
    }
}