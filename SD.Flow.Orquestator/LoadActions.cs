using SD.Flow.Orquestator.Core;
using System.Reflection;

// ... dentro de tu clase de orquestación ...
public class SDActions
{
    public List<IWorkflowAction> LoadActions()
    {
        var actions = new List<IWorkflowAction>();

        // 1. Obtener la ruta donde se ejecuta la aplicación
        string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Actions");

        // 2. Buscar archivos DLL que cumplan con el patrón ".Action."
        var actionDlls = Directory.GetFiles(path, "*.Action.*.dll", SearchOption.AllDirectories);

        foreach (var dllPath in actionDlls)
        {
            try
            {
                // 3. Cargar el ensamblado (Assembly)
                Assembly assembly = Assembly.LoadFrom(dllPath);

                // 4. Buscar tipos que implementen IWorkflowAction, sean clases y no sean abstractas
                var actionTypes = assembly.GetTypes().Where(t =>
                    typeof(IWorkflowAction).IsAssignableFrom(t) &&
                    t.IsClass &&
                    !t.IsAbstract);

                foreach (var type in actionTypes)
                {
                    // 5. Instanciar la acción y agregarla a la lista
                    var instance = Activator.CreateInstance(type) as IWorkflowAction;
                    if (instance != null)
                    {
                        actions.Add(instance);
                    }
                }
            }
            catch (Exception ex)
            {
                // Es vital capturar errores aquí por si una DLL está corrupta o bloqueada
                Console.WriteLine($"Error cargando acción desde {dllPath}: {ex.Message}");
            }
        }

        return actions;
    }
}
