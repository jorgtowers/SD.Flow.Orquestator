namespace SD.Flow.Orquestator.Core
{
    public class WorkflowEngine
    {
        private readonly Dictionary<string, IWorkflowAction> _actions;

        public WorkflowEngine(IEnumerable<IWorkflowAction> actions)
        {
            _actions = new Dictionary<string, IWorkflowAction>(StringComparer.OrdinalIgnoreCase);

            foreach (var action in actions)
            {
                if (_actions.ContainsKey(action.Name))
                {
                    WorkflowLogger.Log(
                        $"Advertencia: acción duplicada '{action.Name}'. Se conserva la primera registrada.");
                    continue;
                }

                _actions[action.Name] = action;
            }
        }

        public async Task RunWorkflowAsync(WorkflowDefinition workflow)
        {
            WorkflowLogger.Log($"=== Iniciando: {workflow.WorkflowName} ===");

            if (workflow.Steps == null || workflow.Steps.Count == 0)
            {
                WorkflowLogger.Log("El workflow no tiene pasos definidos.");
                WorkflowLogger.Log($"=== Finalizado: {workflow.WorkflowName} ===");
                return;
            }

            int? failedStepIndex = null;
            string? failedStepDescription = null;

            for (int i = 0; i < workflow.Steps.Count; i++)
            {
                int stepNumber = i + 1;
                var step = workflow.Steps[i];
                string stepLabel = string.IsNullOrWhiteSpace(step.Description)
                    ? step.ActionName
                    : step.Description;

                if (failedStepIndex.HasValue)
                {
                    WorkflowLogger.Log(
                        $"Paso {stepNumber} omitido ('{stepLabel}'): error previo en paso {failedStepIndex} ('{failedStepDescription}').");
                    continue;
                }

                if (!_actions.TryGetValue(step.ActionName, out var action))
                {
                    WorkflowLogger.Log(
                        $"Paso {stepNumber} omitido ('{stepLabel}'): acción '{step.ActionName}' no disponible. " +
                        "Verifique que exista la DLL correspondiente en la carpeta Actions.");
                    continue;
                }

                var missingKeys = WorkflowParameterValidator.GetMissingKeys(action, step.Parameters);
                if (missingKeys.Count > 0)
                {
                    WorkflowLogger.Log(
                        $"Paso {stepNumber} omitido ('{stepLabel}'): faltan parámetros obligatorios [{string.Join(", ", missingKeys)}].");
                    continue;
                }

                try
                {
                    WorkflowLogger.Log($"Ejecutando paso {stepNumber}: {stepLabel} [{step.ActionName}]");
                    await action.ExecuteAsync(step.Parameters ?? new Dictionary<string, string>());
                }
                catch (Exception ex)
                {
                    WorkflowLogger.LogError($"Falló el paso {stepNumber} '{stepLabel}'", workflow.WorkflowName, ex);
                    failedStepIndex = stepNumber;
                    failedStepDescription = stepLabel;
                }
            }

            if (failedStepIndex.HasValue)
            {
                WorkflowLogger.Log(
                    $"Workflow completado con errores: falló en el paso {failedStepIndex} ('{failedStepDescription}'). " +
                    "Los pasos posteriores fueron omitidos.");
            }

            WorkflowLogger.Log($"=== Finalizado: {workflow.WorkflowName} ===");
        }
    }
}
