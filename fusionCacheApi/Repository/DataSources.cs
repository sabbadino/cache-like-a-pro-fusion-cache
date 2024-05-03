using System.Text.Json;

namespace fusionCacheApi.Repository
{
    public interface IDataSources
    {
        Task<string> GetCurrentTime(string location, bool throwEx);

        Task<List<PortDetails>?> GetPorts(bool throwEx);
    }
    public class DataSources : IDataSources
    {
        public async Task<string> GetCurrentTime(string location, bool throwEx)
        {
            if (throwEx)
            {
                throw new Exception("forced exception");
            }
            return await Task.FromResult(DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss-fff"));
        }

        public Task<List<PortDetails>?> GetPorts(bool throwEx)
        {
            return Task.FromResult(JsonSerializer.Deserialize<List<PortDetails>>(File.ReadAllText($".{Path.DirectorySeparatorChar}Repository{Path.DirectorySeparatorChar}ports.json")));
        }
    }
}
