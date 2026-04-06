using EQFR.Common;

namespace EQFR.IO.Config;

public interface IConfigLoader
{
    Result<ConfigBundle> LoadFromDirectory(string configDirectory);
}

