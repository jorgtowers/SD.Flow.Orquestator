using SD.Flow.Orquestator.Core;
using System.Reflection;

public class SDActions
{
    public List<IWorkflowAction> LoadActions()
    {
        var actionsByName = new Dictionary<string, IWorkflowAction>(StringComparer.OrdinalIgnoreCase);
        string actionsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Actions");

        if (!Directory.Exists(actionsPath))
        {
            WorkflowLogger.Log($"Carpeta Actions no encontrada en: {actionsPath}. No se cargarán módulos.");
            return new List<IWorkflowAction>();
        }

        var actionDlls = Directory.GetFiles(actionsPath, "*.Action.*.dll", SearchOption.TopDirectoryOnly);

        if (actionDlls.Length == 0)
        {
            WorkflowLogger.Log($"No se encontraron DLL de acciones (*.Action.*.dll) en: {actionsPath}");
        }

        foreach (var dllPath in actionDlls)
        {
            try
            {
                Assembly assembly = Assembly.LoadFrom(dllPath);
                var actionTypes = assembly.GetTypes().Where(t =>
                    typeof(IWorkflowAction).IsAssignableFrom(t) &&
                    t.IsClass &&
                    !t.IsAbstract);

                foreach (var type in actionTypes)
                {
                    if (Activator.CreateInstance(type) is not IWorkflowAction instance)
                        continue;

                    if (actionsByName.ContainsKey(instance.Name))
                    {
                        WorkflowLogger.Log(
                            $"Advertencia: '{instance.Name}' en {Path.GetFileName(dllPath)} está duplicada; se ignora.");
                        continue;
                    }

                    actionsByName[instance.Name] = instance;
                    WorkflowLogger.Log($"Acción registrada: {instance.Name} ({Path.GetFileName(dllPath)})");
                }
            }
            catch (Exception ex)
            {
                WorkflowLogger.Log($"Error cargando acción desde {dllPath}: {ex.Message}");
            }
        }

        return actionsByName.Values.ToList();
    }
}
