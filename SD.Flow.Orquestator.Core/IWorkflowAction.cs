namespace SD.Flow.Orquestator.Core
{
    public interface IWorkflowAction
    {
        string Name { get; }
        Task ExecuteAsync(Dictionary<string, string> args);
    }
}
