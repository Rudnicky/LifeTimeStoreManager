using DgtPoC.Interfaces;
using DgtPoC.Models;
using log4net;
using System.Collections.Generic;
using System.Threading;

namespace DgtPoC
{
    public sealed class Worker : IWorker
    {
        private readonly ILog _logger;
        private LifeTimeStorage<string> _lifeTimeStorage;

        public Worker(ILog logger)
        {
            this._logger = logger;
        }

        public void Start()
        {
            _lifeTimeStorage = new LifeTimeStorage<string>(_logger, "TEST", 10, 0);
            _lifeTimeStorage.FileHashTableRemoveEntry += LifeTimeStorage_FileHashTableRemoveEntry;

            _lifeTimeStorage.Put("test_key", "test_tag");
            _lifeTimeStorage.Put("test_key2", "test_tag2");
            _lifeTimeStorage.Put("test_key2", "test_tag2");

            CheckPerformance(100);
            CheckPerformance(1000);
            CheckPerformance(10000);
            CheckPerformance(100000);

            _lifeTimeStorage.FileHashTableRemoveEntry -= LifeTimeStorage_FileHashTableRemoveEntry;
            _lifeTimeStorage.RemoveJobs();
        }

        public void CheckPerformance(int numberOfPushedItems)
        {
            // creates instance of stopwatch to measure time
            var stopWatch = new System.Diagnostics.Stopwatch();

            // creates list of dummy data models
            var dummies = new List<DummyModel>();
            for (int i=0; i<numberOfPushedItems; i++)
            {
                dummies.Add(new DummyModel() { Key = "test_key" + i.ToString(), Value = "test_value" + i.ToString() });
            }

            // start mesauring time
            stopWatch.Start();
            
            // invoke insert on our database
            foreach (var dummy in dummies)
            {
                _lifeTimeStorage.Put(dummy.Key, dummy.Value);
            }

            // get elapsed time
            stopWatch.Stop();

            System.Console.WriteLine("CheckedPerformance for: " + numberOfPushedItems.ToString() +
                ", took exaclty: " + stopWatch.Elapsed.ToString());
        }

        private static void LifeTimeStorage_FileHashTableRemoveEntry(string key, FileHashTableEntry<string> entry)
        {
            System.Console.WriteLine("I've just removed: " + key);
        }
    }
}
