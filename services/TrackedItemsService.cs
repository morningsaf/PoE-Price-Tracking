using System.Collections.Generic;
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

        public List<string> Load()
        {
            if(!File.Exists(_filePath)) return new List<string>();
            string json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
        }

        public void Save(List<string> names)
        {
            string? dir = Path.GetDirectoryName(_filePath);
            if(!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
            string json = JsonSerializer.Serialize(names);
            File.WriteAllText(_filePath, json);
        }


    }
}