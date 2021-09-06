namespace Core
{
    public interface IReferencable
    {
        int ReferenceCount { get; }

        int AddReference();

        int Release();
    }
}
