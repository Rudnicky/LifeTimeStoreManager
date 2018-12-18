namespace DgtPoC
{
    public static class CustomEventHandler<T>
    {
        public delegate void FileHashTableRemoveEntryDel(string key, FileHashTableEntry<T> entry);
    }
}
