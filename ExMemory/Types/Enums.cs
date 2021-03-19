namespace ExternalMemory.Types
{
    public enum OffsetType
    {
        ValueType = 0,
        ExClass = 1,
        IntPtr = 2
    }

    public enum ExKind
    {
        None,
        Instance,
        Pointer
    }
}