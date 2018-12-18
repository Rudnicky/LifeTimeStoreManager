using System;
using System.Threading;

namespace DgtPoC
{
    class Program
    {
        static void Main(string[] args)
        {
            var lifeTimeStorage = new LifeTimeStorage<string>("TEST", 10, 1);
            lifeTimeStorage.FileHashTableRemoveEntry += LifeTimeStorage_FileHashTableRemoveEntry;

            lifeTimeStorage.Put("test_key", "test_tag");
            lifeTimeStorage.Put("test_key2", "test_tag2");
            lifeTimeStorage.Put("test_key2", "test_tag2");

            Thread.Sleep(10000);

            lifeTimeStorage.FileHashTableRemoveEntry -= LifeTimeStorage_FileHashTableRemoveEntry;
            lifeTimeStorage.RemoveJobs();

            Console.ReadKey();
        }

        private static void LifeTimeStorage_FileHashTableRemoveEntry(string key, FileHashTableEntry<string> entry)
        {
            System.Console.WriteLine("I've just removed: " + key);
        }
    }
}
