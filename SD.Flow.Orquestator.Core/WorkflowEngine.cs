namespace SD.Flow.Orquestator.Core
{
    public class WorkflowEngine
    {
        private readonly Dictionary<string, IWorkflowAction> _actions;

        public WorkflowEngine(IEnumerable<IWorkflowAction> actions)
        {
            // Guardamos todas las estrategias disponibles
            _actions = actions.ToDictionary(a => a.Name);
        }

        public async Task RunWorkflowAsync(WorkflowDefinition workflow)
        {
            WorkflowLogger.Log($"=== Iniciando: {workflow.WorkflowName} ===");

            foreach (var step in workflow.Steps)
            {
                if (_actions.TryGetValue(step.ActionName, out var action))
                {
                    try
                    {
                        WorkflowLogger.Log($"Ejecutando paso: {step.Description}");
                        await action.ExecuteAsync(step.Parameters);
                    }
                    catch (Exception ex)
                    {
                        WorkflowLogger.LogError($"Falló el paso '{step.Description}'", workflow.WorkflowName, ex);

                        // Aquí podrías decidir si detener todo el flujo o seguir
                        WorkflowLogger.Log("Abortando workflow debido a un error crítico.");
                        return;
                    }
                }
            }

            WorkflowLogger.Log($"=== Finalizado: {workflow.WorkflowName} ===");
        }
    }
}
