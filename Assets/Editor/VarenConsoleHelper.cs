// PURPOSE: Collects Unity Console errors with scene, source location, and stack-trace details for faster debugging.
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class VarenConsoleErrorCollector
{
    [Serializable]
    public class ErrorEntry
    {
        public string message;
        public string stackTrace;
        public string scene;
        public string time;
        public LogType type;
    }

    private static readonly List<ErrorEntry> entries = new List<ErrorEntry>();

    public static IReadOnlyList<ErrorEntry> Entries => entries;

    static VarenConsoleErrorCollector()
    {
        Application.logMessageReceived -= Record;
        Application.logMessageReceived += Record;
    }

    public static void Clear()
    {
        entries.Clear();
    }

    private static void Record(string condition, string stackTrace, LogType type)
    {
        if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert)
            return;

        entries.Add(new ErrorEntry
        {
            message = condition,
            stackTrace = stackTrace,
            scene = SceneManager.GetActiveScene().name,
            time = DateTime.Now.ToString("HH:mm:ss"),
            type = type
        });

        if (entries.Count > 100)
            entries.RemoveAt(0);
    }
}

public class VarenConsoleHelper : EditorWindow
{
    private const string MenuPath = "VAREN/Diagnostics/Console Helper";
    private static readonly Regex SourceLocation = new Regex(@"\(at (Assets[\\/].+?):(\d+)\)", RegexOptions.Compiled);
    private static readonly Regex CompilerLocation = new Regex(@"(Assets[\\/].+?\.cs)\((\d+),(\d+)\):", RegexOptions.Compiled);
    private Vector2 windowScroll;
    private Vector2 listScroll;
    private Vector2 detailScroll;
    private Vector2 diffScroll;
    private int selectedIndex = -1;
    private string selectedDiff;

    [MenuItem(MenuPath)]
    public static void Open()
    {
        GetWindow<VarenConsoleHelper>("Console Helper");
    }

    private void OnEnable()
    {
        minSize = new Vector2(520f, 360f);
    }

    private void OnGUI()
    {
        IReadOnlyList<VarenConsoleErrorCollector.ErrorEntry> entries = VarenConsoleErrorCollector.Entries;
        windowScroll = EditorGUILayout.BeginScrollView(windowScroll);
        EditorGUILayout.HelpBox(
            "This tool records new Unity errors while this Editor session is open. Select an error to see its exact message, scene, source file, line, and stack trace.",
            MessageType.Info);

        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField("Recorded Errors", entries.Count.ToString(), EditorStyles.boldLabel);
            if (GUILayout.Button("Clear", GUILayout.Width(70)))
            {
                VarenConsoleErrorCollector.Clear();
                selectedIndex = -1;
            }
        }

        if (entries.Count == 0)
        {
            EditorGUILayout.HelpBox("No errors have been recorded yet. Run the game or reproduce the problem, then return here.", MessageType.None);
            EditorGUILayout.EndScrollView();
            return;
        }

        listScroll = EditorGUILayout.BeginScrollView(listScroll, GUILayout.Height(120));
        for (int i = entries.Count - 1; i >= 0; i--)
        {
            VarenConsoleErrorCollector.ErrorEntry entry = entries[i];
            GUIStyle style = i == selectedIndex ? EditorStyles.toolbarButton : EditorStyles.miniButton;
            if (GUILayout.Button($"[{entry.time}] {entry.type}: {entry.message}", style))
            {
                selectedIndex = i;
                selectedDiff = null;
            }
        }
        EditorGUILayout.EndScrollView();

        if (selectedIndex < 0 || selectedIndex >= entries.Count)
        {
            EditorGUILayout.EndScrollView();
            return;
        }

        VarenConsoleErrorCollector.ErrorEntry selected = entries[selectedIndex];
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Error Details", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Scene", string.IsNullOrEmpty(selected.scene) ? "No scene" : selected.scene);

        if (TryGetSourceLocation(selected.message, selected.stackTrace, out string path, out int line, out int column))
        {
            string location = column > 0 ? $"{path} : line {line}, column {column}" : $"{path} : line {line}";
            EditorGUILayout.LabelField("Source", location);
            if (GUILayout.Button("Open Source File"))
                OpenSourceFile(path, line);

            DrawSourceContext(path, line, column);
            if (GUILayout.Button("Show Exact Uncommitted Changes"))
                selectedDiff = GetGitDiff(path);

            if (selectedDiff != null)
            {
                EditorGUILayout.LabelField("Changes Since Last Commit", EditorStyles.boldLabel);
                diffScroll = EditorGUILayout.BeginScrollView(diffScroll, GUILayout.Height(260));
                EditorGUILayout.SelectableLabel(
                    string.IsNullOrEmpty(selectedDiff) ? "No uncommitted changes were found for this file." : selectedDiff,
                    EditorStyles.textArea,
                    GUILayout.MinHeight(240));
                EditorGUILayout.EndScrollView();
            }
        }
        else
        {
            EditorGUILayout.HelpBox("Unity did not provide an Assets source location for this error. Read the stack trace below for the originating package or engine call.", MessageType.Warning);
        }

        if (GUILayout.Button("Copy Debug Report"))
            EditorGUIUtility.systemCopyBuffer = BuildReport(selected, path, line, column, selectedDiff);

        detailScroll = EditorGUILayout.BeginScrollView(detailScroll);
        EditorGUILayout.SelectableLabel(
            $"Message:\n{selected.message}\n\nStack Trace:\n{selected.stackTrace}",
            EditorStyles.textArea,
            GUILayout.Height(160));
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndScrollView();
    }

    private static bool TryGetSourceLocation(string message, string stackTrace, out string path, out int line, out int column)
    {
        Match match = CompilerLocation.Match(message ?? string.Empty);
        if (match.Success)
        {
            path = match.Groups[1].Value.Replace('\\', '/');
            int.TryParse(match.Groups[2].Value, out line);
            return int.TryParse(match.Groups[3].Value, out column);
        }

        match = SourceLocation.Match(stackTrace ?? string.Empty);
        if (!match.Success)
        {
            path = null;
            line = 0;
            column = 0;
            return false;
        }

        path = match.Groups[1].Value.Replace('\\', '/');
        column = 0;
        return int.TryParse(match.Groups[2].Value, out line);
    }

    private static void DrawSourceContext(string path, int line, int column)
    {
        string absolutePath = Path.Combine(Application.dataPath, "..", path);
        if (!File.Exists(absolutePath)) return;

        string[] lines = File.ReadAllLines(absolutePath);
        int firstLine = Mathf.Max(1, line - 2);
        int lastLine = Mathf.Min(lines.Length, line + 2);
        StringBuilder context = new StringBuilder();
        for (int currentLine = firstLine; currentLine <= lastLine; currentLine++)
        {
            context.AppendLine($"{currentLine,4} | {lines[currentLine - 1]}");
            if (currentLine == line && column > 0)
                context.AppendLine($"     | {new string(' ', Mathf.Max(0, column - 1))}^");
        }

        EditorGUILayout.LabelField("Code at Error", EditorStyles.boldLabel);
        EditorGUILayout.SelectableLabel(context.ToString(), EditorStyles.textArea, GUILayout.Height(115));
    }

    private static string GetGitDiff(string path)
    {
        try
        {
            string projectPath = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            ProcessStartInfo info = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = $"diff --no-ext-diff --unified=2 -- \"{path}\"",
                WorkingDirectory = projectPath,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            using (Process process = Process.Start(info))
                return process.StandardOutput.ReadToEnd();
        }
        catch (Exception exception)
        {
            return "Could not read Git changes: " + exception.Message;
        }
    }

    private static void OpenSourceFile(string path, int line)
    {
        UnityEngine.Object asset = AssetDatabase.LoadMainAssetAtPath(path);
        if (asset != null)
            AssetDatabase.OpenAsset(asset, line);
    }

    private static string BuildReport(VarenConsoleErrorCollector.ErrorEntry entry, string path, int line, int column, string diff)
    {
        StringBuilder report = new StringBuilder();
        report.AppendLine("VAREN Unity Error Report");
        report.AppendLine($"Time: {entry.time}");
        report.AppendLine($"Scene: {entry.scene}");
        report.AppendLine($"Type: {entry.type}");
        if (!string.IsNullOrEmpty(path)) report.AppendLine($"Source: {path}:{line}:{column}");
        report.AppendLine();
        report.AppendLine("Message:");
        report.AppendLine(entry.message);
        report.AppendLine();
        report.AppendLine("Stack Trace:");
        report.AppendLine(entry.stackTrace);
        if (!string.IsNullOrEmpty(diff))
        {
            report.AppendLine();
            report.AppendLine("Uncommitted Changes:");
            report.AppendLine(diff);
        }
        return report.ToString();
    }
}
#endif
