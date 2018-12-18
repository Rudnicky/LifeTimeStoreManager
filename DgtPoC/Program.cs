using log4net;
using log4net.Config;
using System;
using System.Reflection;
using System.Threading;

namespace DgtPoC
{
    class Program
    {
        private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        static void Main(string[] args)
        {
            SetupLogger();

            var lifeTimeStorage = new LifeTimeStorage<string>(_log, "TEST", 10, 1);
            lifeTimeStorage.FileHashTableRemoveEntry += LifeTimeStorage_FileHashTableRemoveEntry;

            lifeTimeStorage.Put("test_key", "test_tag");
            lifeTimeStorage.Put("test_key2", "test_tag2");
            lifeTimeStorage.Put("test_key2", "test_tag2");

            Thread.Sleep(10000);

            lifeTimeStorage.FileHashTableRemoveEntry -= LifeTimeStorage_FileHashTableRemoveEntry;
            lifeTimeStorage.RemoveJobs();

            Console.ReadKey();
        }

        private static void SetupLogger()
        {
            XmlConfigurator.Configure();
        }

        private static void LifeTimeStorage_FileHashTableRemoveEntry(string key, FileHashTableEntry<string> entry)
        {
            System.Console.WriteLine("I've just removed: " + key);
        }
    }
}
