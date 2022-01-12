using System;

namespace MaMoVM.Confuser.Core.RT
{
    public class BinaryChunk : IVMChunk
    {
        public EventHandler<OffsetComputeEventArgs> OffsetComputed;

        public BinaryChunk(byte[] data)
        {
            Data = data;
        }

        public byte[] Data
        {
            get;
        }

        public uint Offset
        {
            get;
            private set;
        }

        uint IVMChunk.Length => (uint) Data.Length;

        void IVMChunk.OnOffsetComputed(uint offset)
        {
            if(OffsetComputed != null)
                OffsetComputed(this, new OffsetComputeEventArgs(offset));
            Offset = offset;
        }

        byte[] IVMChunk.GetData()
        {
            return Data;
        }
    }

    public class OffsetComputeEventArgs : EventArgs
    {
        internal OffsetComputeEventArgs(uint offset)
        {
            Offset = offset;
        }

        public uint Offset
        {
            get;
        }
    }
}