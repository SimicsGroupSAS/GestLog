using System.Collections.Generic;

namespace GestLog.Modules.DaaterProccesor.Services;

public interface IResourceLoaderService
{
    Dictionary<string, string> LoadPaises();
    Dictionary<long, string[]> LoadPartidas();
    Dictionary<string, string> LoadProveedores();
}
