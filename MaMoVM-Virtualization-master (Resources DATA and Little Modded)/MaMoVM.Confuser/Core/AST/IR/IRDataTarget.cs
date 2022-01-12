#region

using MaMoVM.Confuser.Core.RT;

#endregion

namespace MaMoVM.Confuser.Core.AST.IR
{
    public class IRDataTarget : IIROperand
    {
        public IRDataTarget(BinaryChunk target)
        {
            Target = target;
        }

        public BinaryChunk Target
        {
            get;
            set;
        }

        public string Name
        {
            get;
            set;
        }

        public ASTType Type => ASTType.Ptr;

        public override string ToString()
        {
            return Name;
        }
    }
}