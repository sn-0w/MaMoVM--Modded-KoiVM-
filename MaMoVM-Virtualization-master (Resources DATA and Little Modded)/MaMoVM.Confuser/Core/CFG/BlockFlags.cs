#region

using System;

#endregion

namespace MaMoVM.Confuser.Core.CFG
{
    [Flags]
    public enum BlockFlags
    {
        Normal = 0,
        ExitEHLeave = 1,
        ExitEHReturn = 2
    }
}