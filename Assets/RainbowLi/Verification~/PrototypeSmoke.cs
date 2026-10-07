using System;
using System.Reflection;
using RainbowLi;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class PrototypeSmoke
{
    private static double start;
    private static bool started;
    private static ContainmentGame game;
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/RainbowLi/Scenes/ContainmentPrototype.unity");
        if (UnityEngine.Object.FindFirstObjectByType<ContainmentPrototype>() == null)
            throw new Exception("Scene controller is missing");
        EditorSettings.enterPlayModeOptionsEnabled = true;
        EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
        start = EditorApplication.timeSinceStartup;
        Application.logMessageReceived += HandleLog;
        EditorApplication.update += Tick;
        EditorApplication.EnterPlaymode();
    }
    private static void HandleLog(string message, string stack, LogType type)
    {
        // Unity 6's search index can throw in an empty, package-minimal batch project.
        // Preserve this diagnostic; only this editor-internal stack is excluded.
        if (stack.Contains("UnityEditor.Search.SearchDatabase") && stack.Contains("IndexationOnStartup"))
        { Console.WriteLine("EDITOR DIAGNOSTIC: search indexing exception (no gameplay stack)."); return; }
        if (type == LogType.Exception || type == LogType.Error)
        { Console.WriteLine("SMOKE FAILED: " + message + "\n" + stack); EditorApplication.Exit(1); }
    }
    private static void Tick()
    {
        if (EditorApplication.timeSinceStartup - start > 80)
        { Console.WriteLine("SMOKE FAILED: timeout"); EditorApplication.Exit(2); return; }
        if (!EditorApplication.isPlaying || EditorApplication.isPaused) return;
        if (!started)
        {
            var controller = UnityEngine.Object.FindFirstObjectByType<ContainmentPrototype>();
            game = (ContainmentGame)typeof(ContainmentPrototype).GetField("game", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(controller);
            if (game == null || game.Deck.Count != 18) throw new Exception("Awake failed");
            game.StartBattle();
            game.AddCard(game.Hand[0]); game.Confirm();
            started = true;
        }
        if (game.Round >= 2)
        {
            Console.WriteLine("PASS: Unity scene loads, Awake initializes, and Update auto-resolves into round 2.");
            EditorApplication.update -= Tick;
            EditorApplication.Exit(0);
        }
    }
}
