using System;
using System.IO;
using System.Text;
using System.Text.Json;

namespace nLogViewer.Infrastructure.Helpers.FileProvider;

internal class JsonFileProvider<T> : IFileProvider<T> where T : class, new()
{
    /// <summary>
    /// Десериализация содержимого указанного файла
    /// </summary>
    /// <param name="filePath">Путь к файлу</param>
    /// <returns></returns>
    public T Load(string filePath)
    {
        if(!File.Exists(filePath))
        {
            throw new ArgumentException("Файл не существует", nameof(filePath));
        }
        FileInfo file = new FileInfo(filePath);
        using var sr = new StreamReader(file.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
        var data = sr.ReadToEnd();
        return JsonSerializer.Deserialize<T>(data) ?? new T();
    }

    /// <summary>
    /// Сериализация в файл
    /// </summary>
    /// <param name="filePath">Путь к файлу</param>
    public void Save(T obj, string filePath)
    {
        if (string.IsNullOrEmpty(filePath) || string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("Наименование файла не может быть null", nameof(filePath));
        }
        
        try
        {
            string json = JsonSerializer.Serialize(obj);
            File.WriteAllText(filePath, json, Encoding.UTF8);
        }
        catch (Exception e)
        {
            throw new Exception($"При попытке записи в файл {filePath} было вызвано исключение");
        }
    }
}