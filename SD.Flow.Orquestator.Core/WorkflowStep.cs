namespace SD.Flow.Orquestator.Core
{
    public class WorkflowStep
    {
        public string Description { get; set; }
        public string ActionName { get; set; }
        public Dictionary<string, string> Parameters { get; set; }
    }
}
