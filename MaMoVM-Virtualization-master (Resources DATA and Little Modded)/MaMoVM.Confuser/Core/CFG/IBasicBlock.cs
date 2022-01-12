#region

using System.Collections.Generic;

#endregion

namespace MaMoVM.Confuser.Core.CFG
{
    public interface IBasicBlock
    {
        int Id
        {
            get;
        }

        object Content
        {
            get;
        }

        BlockFlags Flags
        {
            get;
            set;
        }

        IEnumerable<IBasicBlock> Sources
        {
            get;
        }

        IEnumerable<IBasicBlock> Targets
        {
            get;
        }
    }
}