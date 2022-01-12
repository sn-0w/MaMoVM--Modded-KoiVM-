namespace MaMoVM.Confuser.Core.VMIR
{
    public interface ITransform
    {
        void Initialize(IRTransformer tr);
        void Transform(IRTransformer tr);
    }
}