using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;

namespace Utilities
{
    public class Config
    {
        private readonly Dictionary<string, object?> m_configMap = new();
        private readonly string m_filePath;
        private readonly string m_category;
        private static SemaphoreSlim m_fileLock = new(1, 1);
        private const string m_DefaultConfigDir = "../json";

        public Config(string configFilename = null, string category = "default")
        {
            m_category = category;
            if (string.IsNullOrEmpty(configFilename))
            {
                m_filePath = Path.Combine(m_DefaultConfigDir, $"config.json");

            }
            else if (Path.IsPathRooted(configFilename))
            {
                m_filePath = configFilename;
            }
            else
            {
                m_filePath = Path.Combine(m_DefaultConfigDir, configFilename);
            }
            
            var directory = Path.GetDirectoryName(m_filePath);
            if(!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }
            ReadJsonFile();
        }

        public bool ReadConfig<T>(string key, out T? value)
        {
            value = default;
          
            if(!m_configMap.TryGetValue(key, out var obj))
            {
                return false;
            }
            try
            {
                if(obj is JsonElement jsonElement)
                {
                    value = JsonSerializer.Deserialize<T>(jsonElement.GetRawText());
                    return true;
                }
                else
                {
                    var converted = Convert.ChangeType(obj, typeof(T));
                    if(converted is null)
                    {
                        return false;
                    }
                    value = (T)converted;
                }
                return true;
        }
            catch
            {
                return false;
            }
        }
        public bool WriteConfig<T>(string key, T value)
        {
            m_configMap[key] = value;
            return true;
        }
        public bool ReadConfigOrUseDefault<T>(string key, out T value, T defaultValue)
        {
            if(!ReadConfig<T>(key, out value!))
            {
                value = defaultValue;
                WriteConfig<T>(key, value);
                SaveConfig();
            }
            return true;
            
        }

        public T GetConfigOrDefault<T>(string key, T defaultValue)
        {
            ReadConfigOrUseDefault<T>(key, out T value, defaultValue);
            return value;
        }

        public bool SaveConfig()
        {
            return WriteJsonFile();
        }

        public bool LoadConfig()
        {
            return ReadJsonFile();
        }

        private bool ReadJsonFile()
        {
            m_fileLock.Wait();
            try
            {
                if(!File.Exists(m_filePath))
                {
                    return false;
                }
                string jsonContent = File.ReadAllText(m_filePath);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    WriteIndented = true
                };

                var configFileData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonContent, options);

                if(configFileData == null || !configFileData.ContainsKey(m_category))
                {
                    return false;
                }
                var categoryData = configFileData[m_category];

                if(categoryData.ValueKind != JsonValueKind.Object)
                {
                    return false;
                }
                m_configMap.Clear();
                foreach(var property in categoryData.EnumerateObject())
                {
                    string key = property.Name;
                    var value = property.Value;
                    m_configMap[key] = value.ValueKind switch
                    {
                        JsonValueKind.String => value.GetString(),
                        JsonValueKind.Number => value.GetDouble(),
                        JsonValueKind.True => true,
                        JsonValueKind.False => false,
                        JsonValueKind.Object => value,
                        JsonValueKind.Array => value,
                        _ => value,
                    };
                }
                return true;
            }
            catch(Exception e)
            {
                Console.Error.WriteLine($"Error reading config file: {e.Message}");
                return false;
            }
            finally
            {
                m_fileLock.Release();
            }
        }

        private bool WriteJsonFile()
        {
            m_fileLock.Wait();
            try
            {
                var configFileData = new Dictionary<string, Dictionary<string, object?>>();
                if (File.Exists(m_filePath))
                {
                    try
                    {
                        string existingContent = File.ReadAllText(m_filePath);
                        var tempData = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, object?>>>(existingContent);
                        if (tempData != null)
                        {
                            configFileData = tempData;
                        }
                    }
                    catch
                    {
                        
                    }
                }
                configFileData[m_category] = new Dictionary<string, object?>(m_configMap);
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };
                string jsonContent = JsonSerializer.Serialize(configFileData, options);
                File.WriteAllText(m_filePath, jsonContent);
                return true;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"Error writing config file: {e.Message}");
                return false;
            }
            finally
            {
                m_fileLock.Release();
            }
        }
            

    }
}