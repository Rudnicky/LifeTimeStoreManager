using System.Collections.Generic;

namespace DgtPoC
{
    public interface ILifeTimeList<T>
    {
        event CustomEventHandler<T>.FileHashTableRemoveEntryDel FileHashTableRemoveEntry;

        void Put(string key, T tag);

        bool Remove(string key);

        bool Exists(string key);

        T Get(string key);

        List<T> GetValueList();

        Dictionary<string, T> GetDictionary();

        void Clear();
    }
}
