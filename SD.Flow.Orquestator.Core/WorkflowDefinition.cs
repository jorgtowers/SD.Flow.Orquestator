namespace SD.Flow.Orquestator.Core
{
    public class WorkflowDefinition
    {
        public string WorkflowName { get; set; }
        public List<WorkflowStep> Steps { get; set; }
    }
}
