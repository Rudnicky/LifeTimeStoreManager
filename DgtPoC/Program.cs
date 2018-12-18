using log4net;
using log4net.Config;
using System.Reflection;

namespace DgtPoC
{
    class Program
    {
        private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        static void Main(string[] args)
        {
            SetupLogger();
            SetupWorker();
        }

        #region Private Static Methods
        private static void SetupLogger()
        {
            XmlConfigurator.Configure();
        }

        private static void SetupWorker()
        {
            var worker = new Worker(_log);
            worker.Start();
        }
        #endregion
    }
}
