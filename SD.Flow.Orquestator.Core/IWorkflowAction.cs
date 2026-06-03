namespace SD.Flow.Orquestator.Core
{
    public interface IWorkflowAction
    {
        string Name { get; }

        /// <summary>Claves obligatorias en Parameters del paso JSON.</summary>
        IReadOnlyCollection<string> RequiredParameterKeys => Array.Empty<string>();

        Task ExecuteAsync(Dictionary<string, string> args);
    }
}
