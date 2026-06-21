using System.IO;
using System.Text.Json;

namespace PoE_Price_Tracking
{
    public class TrackedItemsService
    {
        private readonly string _filePath;
        public TrackedItemsService(string filePath)
        {
            _filePath = filePath;
        }

        public UserData Load()
        {
            if(!File.Exists(_filePath)) return new UserData();
            string json = File.ReadAllText(_filePath);
            var data = JsonSerializer.Deserialize<UserData>(json);
            return data ?? new UserData();
        }

        public void Save(UserData data)
        {
            string? dir = Path.GetDirectoryName(_filePath);
            if(!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
            string json = JsonSerializer.Serialize(data);
            File.WriteAllText(_filePath, json);
        }
    }
}