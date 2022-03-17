using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace RptWatcher;

/// <summary>
/// Holder of the entry point of the program.
/// </summary>
public static class Program
{
    private static string? currentFile;
    private static int printed;

    /// <summary>
    /// Entry point of the program.
    /// </summary>
    /// <param name="args">The command line arguments.</param>
    public static void Main(string[] args)
    {
        string dir = args?.Length == 1 ? args[0] : GetDefaultDirectory();
        string? mostRecent = GetMostRecentFile(dir);

        if (mostRecent is not null)
        {
            SwitchCurrentFile(mostRecent);
        }

        using var watcher = new FileSystemWatcher(dir);
        watcher.Changed += OnChanged;
        watcher.Created += OnCreated;
        watcher.Filter = "*.rpt";
        watcher.IncludeSubdirectories = true;
        watcher.EnableRaisingEvents = true;

        while (true) { }
    }

    private static string GetDefaultDirectory()
    {
        string baseDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(baseDir, "Arma 3");
    }

    private static string? GetMostRecentFile(string dir)
    {
        string? result = null;
        DateTime writeTime = DateTime.MinValue;

        foreach (string file in Directory.GetFiles(dir, "*.rpt", SearchOption.AllDirectories))
        {
            DateTime curWriteTime = File.GetLastWriteTimeUtc(file);
            if (curWriteTime > writeTime)
            {
                result = file;
                writeTime = curWriteTime;
            }
        }

        return result;
    }

    private static void OnCreated(object sender, FileSystemEventArgs e)
    {
        if (Path.GetExtension(e.FullPath) != ".rpt" || e.ChangeType != WatcherChangeTypes.Created)
        {
            return;
        }

        SwitchCurrentFile(e.FullPath);
    }

    private static void OnChanged(object sender, FileSystemEventArgs e)
    {
        string prevFullPath = Path.GetFullPath(currentFile);
        string newFullPath = Path.GetFullPath(e.FullPath);

        if (prevFullPath != newFullPath || e.ChangeType != WatcherChangeTypes.Changed)
        {
            return;
        }

        PrintContent();
    }

    private static void SwitchCurrentFile(string file)
    {
        currentFile = file;
        Print($" === {Path.GetFileNameWithoutExtension(file)} ===\n", ConsoleColor.Magenta);
        printed = 0;
        PrintContent();
    }

    private static void PrintContent()
    {
        string content = ReadFile(currentFile);
        string stripped = content.Substring(printed);
        printed += stripped.Length;
        string[] lines = Regex.Split(stripped, @"(?=\n\d{2}:\d{2}:\d{2})");

        foreach (string line in lines)
        {
            string upper = line.ToUpperInvariant();
            ConsoleColor? color = null;

            if (upper.Contains("ERROR") || upper.Contains("FILE") || upper.Contains("CONTEXT"))
            {
                color = ConsoleColor.Red;
            }
            else if (upper.Contains("WARNING") || upper.Contains("FAIL") || upper.Contains("NOT FOUND") || upper.Contains("MISSING"))
            {
                color = ConsoleColor.Yellow;
            }

            Print(line, color);
        }
    }

    private static string ReadFile(string path)
    {
        while (true)
        {
            try
            {
                using FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using StreamReader sr = new StreamReader(fs, Encoding.Default);
                return sr.ReadToEnd();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                Thread.Sleep(10);
            }
        }
    }

    private static void Print(string text, ConsoleColor? color)
    {
        if (color is null)
        {
            Console.Write(text);
        }
        else
        {
            ConsoleColor prev = Console.ForegroundColor;
            Console.ForegroundColor = color.Value;
            Console.Write(text);
            Console.ForegroundColor = prev;
        }
    }
}