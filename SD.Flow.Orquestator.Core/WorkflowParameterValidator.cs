namespace SD.Flow.Orquestator.Core
{
    public static class WorkflowParameterValidator
    {
        public static IReadOnlyList<string> GetMissingKeys(
            IWorkflowAction action,
            Dictionary<string, string>? parameters)
        {
            var required = action.RequiredParameterKeys;
            if (required.Count == 0)
                return Array.Empty<string>();

            if (parameters == null || parameters.Count == 0)
                return required.ToList();

            return required
                .Where(key => !parameters.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value))
                .ToList();
        }
    }
}
