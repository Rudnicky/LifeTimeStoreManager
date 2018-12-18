using DgtPoC.Interfaces;
using log4net;
using System.Threading;

namespace DgtPoC
{
    public sealed class Worker : IWorker
    {
        private readonly ILog _logger;

        public Worker(ILog logger)
        {
            this._logger = logger;
        }

        public void Start()
        {
            var lifeTimeStorage = new LifeTimeStorage<string>(_logger, "TEST", 10, 1);
            lifeTimeStorage.FileHashTableRemoveEntry += LifeTimeStorage_FileHashTableRemoveEntry;

            lifeTimeStorage.Put("test_key", "test_tag");
            lifeTimeStorage.Put("test_key2", "test_tag2");
            lifeTimeStorage.Put("test_key2", "test_tag2");

            Thread.Sleep(10000);

            lifeTimeStorage.FileHashTableRemoveEntry -= LifeTimeStorage_FileHashTableRemoveEntry;
            lifeTimeStorage.RemoveJobs();
        }

        private static void LifeTimeStorage_FileHashTableRemoveEntry(string key, FileHashTableEntry<string> entry)
        {
            System.Console.WriteLine("I've just removed: " + key);
        }
    }
}
