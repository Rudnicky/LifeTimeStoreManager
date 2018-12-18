using System;

namespace DgtPoC
{
    public class FileHashTableEntry<T>
    {
        public int Id { get; set; }

        public string Key { get; set; }

        public T Tag { get; set; }

        public DateTime DeleteDate { get; set; }
    }
}
