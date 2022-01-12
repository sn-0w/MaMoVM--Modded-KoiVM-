namespace MaMoVM.Runtime.Data
{
    internal struct VMExportInfo
    {
        [VMProtect.BeginMutation]
        public unsafe VMExportInfo(ref byte* ptr, System.Reflection.Module module)
        {
            CodeOffset = *(uint*) ptr;
            ptr += 4;
            if(CodeOffset != 0)
            {
                EntryKey = *(uint*) ptr;
                ptr += 4;
            }
            else
            {
                EntryKey = 0;
            }
            Signature = new VMFuncSig(ref ptr, module);
        }

        public readonly uint CodeOffset;
        public readonly uint EntryKey;
        public readonly VMFuncSig Signature;
    }
}