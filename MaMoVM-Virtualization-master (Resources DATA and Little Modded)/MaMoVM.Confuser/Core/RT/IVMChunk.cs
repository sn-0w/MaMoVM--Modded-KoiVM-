namespace MaMoVM.Confuser.Core.RT
{
    public interface IVMChunk
    {
        uint Length
        {
            get;
        }

        void OnOffsetComputed(uint offset);
        byte[] GetData();
    }
}