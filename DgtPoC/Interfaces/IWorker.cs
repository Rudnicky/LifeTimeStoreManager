namespace DgtPoC.Interfaces
{
    public interface IWorker
    {
        void Start();

        void CheckPerformance(int numberOfPushedItems);
    }
}
