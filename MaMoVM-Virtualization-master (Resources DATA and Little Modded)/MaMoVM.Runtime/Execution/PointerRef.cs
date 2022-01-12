using System;
using MaMoVM.Runtime.Execution.Internal;

namespace MaMoVM.Runtime.Execution
{
    internal unsafe class PointerRef : IReference
    {
        // Only for typed reference use

        private readonly void* ptr;

        [VMProtect.BeginUltra]
        public PointerRef(void* ptr)
        {
            this.ptr = ptr;
        }

        [VMProtect.BeginUltra]
        public VMSlot GetValue(VMContext ctx, PointerType type)
        {
            throw new NotSupportedException();
        }

        [VMProtect.BeginUltra]
        public void SetValue(VMContext ctx, VMSlot slot, PointerType type)
        {
            throw new NotSupportedException();
        }

        [VMProtect.BeginUltra]
        public IReference Add(uint value)
        {
            throw new NotSupportedException();
        }

        [VMProtect.BeginUltra]
        public IReference Add(ulong value)
        {
            throw new NotSupportedException();
        }

        [VMProtect.BeginUltra]
        public void ToTypedReference(VMContext ctx, TypedRefPtr typedRef, Type type)
        {
            TypedReferenceHelpers.MakeTypedRef(ptr, typedRef, type);
        }
    }
}