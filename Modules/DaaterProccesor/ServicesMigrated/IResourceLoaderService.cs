using System.Collections.Generic;

namespace GestLog.ServicesMigrated;

public interface IResourceLoaderService
{
    Dictionary<string, string> LoadPaises();
    Dictionary<long, string[]> LoadPartidas();
    Dictionary<string, string> LoadProveedores();
}
