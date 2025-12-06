using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.Json;

namespace Carbot;

public class PromptStore
{
    private readonly List<string> _truths;
    private readonly List<string> _dares;
    private readonly Random _random = new();
    private readonly object _lock = new();
    private readonly string _dataFilePath;

    private class PromptData
    {
        public List<string> Truths { get; set; } = new();
        public List<string> Dares { get; set; } = new();
    }

    public PromptStore()
    {
        var baseDir = AppContext.BaseDirectory;
        var dataDir = Path.Combine(baseDir, "data");
        Directory.CreateDirectory(dataDir);
        _dataFilePath = Path.Combine(dataDir, "prompts.json");

        _truths = new List<string>
        {
            "What is your biggest irrational fear?",
            "Have you ever lied about something serious?"
        };

        _dares = new List<string>
        {
            "Eat healthier today",
            "Drink some water"
        };
        LoadFromFile();
    }

    private void LoadFromFile()
    {
        if (!File.Exists(_dataFilePath)) return;

        try
        {
            var json = File.ReadAllText(_dataFilePath);
            var data = JsonSerializer.Deserialize<PromptData>(json);
            if (data is null) return;

            _truths.Clear();
            _truths.AddRange(data.Truths);

            _dares.Clear();
            _dares.AddRange(data.Dares);
        }
        catch
        {
            // ignore for now
        }
    }

    private void SaveToFile()
    {
        var data = new PromptData
        {
            Truths = _truths.ToList(),
            Dares = _dares.ToList()
        };

        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        File.WriteAllText(_dataFilePath, json);
    }

    public IReadOnlyList<string> GetTruths()
    {
        lock (_lock)
            return _truths.ToList();
    }

    public IReadOnlyList<string> GetDares()
    {
        lock (_lock)
            return _dares.ToList();
    }

    public string GetRandomTruth()
    {
        lock (_lock)
            return _truths[_random.Next(_truths.Count)];
    }

    public string GetRandomDare()
    {
        lock (_lock)
            return _dares[_random.Next(_dares.Count)];
    }

    public void AddTruth(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        lock (_lock)
        {
            _truths.Add(text);
            SaveToFile();
        } 
    }

    public void AddDare(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        lock (_lock)
        { 
            _dares.Add(text);
            SaveToFile();
        }
    }

    public bool RemoveTruth(int index)
    {
        lock (_lock)
        {
            if (index < 0 || index >= _truths.Count)
                return false;
            _truths.RemoveAt(index);
            SaveToFile();
            return true;
        }
    }

    public bool RemoveDare(int index)
    {
        lock (_lock)
        {
            if (index < 0 || index >= _dares.Count)
                return false;
            _dares.RemoveAt(index);
            SaveToFile();
            return true;
        }
    }
}