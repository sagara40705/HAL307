using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

// Batch-only regression probe. Uses the real Pause scene and Input System key states.
[InitializeOnLoad]
public static class PauseMenuInputValidation
{
    private const string SessionKey = "PauseMenuInputValidation.Running";
    private const string BootKey = "PauseMenuInputValidation.Boot";
    private const string ExitKey = "PauseMenuInputValidation.ExitCode";
    private const string ScenePath = "Assets/_Shared/InGameUI/Menu/Scenes/Pause.unity";
    private static IEnumerator routine;
    private static readonly List<string> results = new();
    private static Keyboard keyboard;
    private static VoiceMainMenuController main;
    private static HowToController howTo;
    private static MicSettingController mic;
    private static GameObject mainPanel;
    private static GameObject howToPanel;
    private static int actionCount;
    private static int failures;
    private static InputSettings.UpdateMode previousUpdateMode;
    private static InputSettings.BackgroundBehavior previousBackgroundBehavior;
    private static InputSettings.EditorInputBehaviorInPlayMode previousEditorInputBehavior;

    static PauseMenuInputValidation()
    {
        EditorApplication.playModeStateChanged += OnPlayMode;
    }

    public static void RunBatch()
    {
        if (!Application.isBatchMode) throw new InvalidOperationException("Run this probe in a separate batch editor.");
        SessionState.SetBool(BootKey, false);
        SessionState.SetBool(SessionKey, true);
        // Keep this isolated probe out of the normal Boot/gameplay sequence.
        MethodInfo callback = typeof(PlayFromBoot).GetMethod("OnPlayModeStateChanged", BindingFlags.Static | BindingFlags.NonPublic);
        EditorApplication.playModeStateChanged -= (Action<PlayModeStateChange>)Delegate.CreateDelegate(typeof(Action<PlayModeStateChange>), callback);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        EditorSceneManager.playModeStartScene = null;
        EditorApplication.isPlaying = true;
    }

    public static void RunBootBatch()
    {
        if (!Application.isBatchMode) throw new InvalidOperationException("Run this probe in a separate batch editor.");
        SessionState.SetBool(BootKey, true);
        SessionState.SetBool(SessionKey, true);
        EditorSceneManager.OpenScene("Assets/_Shared/Prototype/Scenes/Prototype.unity");
        EditorApplication.isPlaying = true;
    }

    private static void OnPlayMode(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode && SessionState.GetInt(ExitKey, -1) >= 0)
        {
            int exitCode = SessionState.GetInt(ExitKey, 1);
            SessionState.EraseInt(ExitKey);
            EditorApplication.Exit(exitCode);
            return;
        }
        if (!SessionState.GetBool(SessionKey, false) || state != PlayModeStateChange.EnteredPlayMode) return;
        routine = CoroutineUtility.Run(Run());
        EditorApplication.update += Tick;
    }

    private static void Tick()
    {
        try
        {
            if (routine.Current is AsyncOperation operation && !operation.isDone) return;
            if (routine.MoveNext()) return;
            Finish();
        }
        catch (Exception ex)
        {
            results.Add("HARNESS ERROR: " + ex);
            failures++;
            Finish();
        }
    }

    private static void Finish()
    {
        EditorApplication.update -= Tick;
        SessionState.EraseBool(SessionKey);
        if (keyboard != null) InputSystem.RemoveDevice(keyboard);
        InputSystem.settings.updateMode = previousUpdateMode;
        InputSystem.settings.backgroundBehavior = previousBackgroundBehavior;
        InputSystem.settings.editorInputBehaviorInPlayMode = previousEditorInputBehavior;
        Time.timeScale = 1f;
        results.Add($"TOTAL: {results.Count} observations; failures={failures}");
        Directory.CreateDirectory("Library/UIInvestigation");
        string suffix = SessionState.GetBool(BootKey, false) ? "boot-validation.txt" : "validation.txt";
        File.WriteAllLines("Library/UIInvestigation/" + suffix, results);
        Debug.Log("[PauseMenuInputValidation]\n" + string.Join("\n", results));
        // Stop Play normally so persistent managers receive OnApplicationQuit.
        SessionState.SetInt(ExitKey, failures == 0 ? 0 : 1);
        EditorApplication.isPlaying = false;
    }

    private static IEnumerator Run()
    {
        previousUpdateMode = InputSystem.settings.updateMode;
        previousBackgroundBehavior = InputSystem.settings.backgroundBehavior;
        previousEditorInputBehavior = InputSystem.settings.editorInputBehaviorInPlayMode;
        InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsManually;
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        bool boot = SessionState.GetBool(BootKey, false);
        if (boot)
        {
            double deadline = EditorApplication.timeSinceStartup + 120;
            while (GamePhaseManager.Instance == null || !GamePhaseManager.Instance.IsInitialized ||
                   GamePhaseManager.Instance.CurrentPhase != GamePhase.InGame ||
                   !SceneManager.GetSceneByName("crowdtest").isLoaded ||
                   !SceneManager.GetSceneByPath(ScenePath).isLoaded ||
                   PauseManager.Instance == null || !PauseManager.Instance.IsInitialized ||
                   SceneLoader.Instance == null || SceneLoader.Instance.IsBusy)
            {
                if (EditorApplication.timeSinceStartup > deadline) throw new TimeoutException("Boot/Prototype did not finish loading");
                yield return null;
            }
        }
        else
        {
            SceneManager.sceneLoaded += DisableAutomaticUpdates;
            yield return SceneManager.LoadSceneAsync(ScenePath, LoadSceneMode.Additive);
            SceneManager.sceneLoaded -= DisableAutomaticUpdates;
        }
        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        var scripts = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<MonoBehaviour>(true)).ToArray();
        main = scripts.OfType<VoiceMainMenuController>().Single();
        howTo = scripts.OfType<HowToController>().Single();
        mic = scripts.OfType<MicSettingController>().Single();
        mainPanel = Field<GameObject>(main, "menuPanel");
        howToPanel = Field<GameObject>(howTo, "howToPanel");
        if (!boot)
        {
            Invoke(main, "Start");
            Invoke(howTo, "Start");
            Invoke(mic, "Start");
        }
        else
        {
            main.enabled = false;
            main.OpenMenu();
            yield return WaitForPhase();
            howTo.OpenHowTo();
            yield return NextFrame();
            yield return NextFrame();
            Observe(howToPanel.activeInHierarchy && !mainPanel.activeInHierarchy, "Boot: first HowTo open survives its initial Start");
            howTo.CloseHowTo();
            mic.OpenMicSetting();
            yield return NextFrame();
            yield return NextFrame();
            Observe(Field<GameObject>(mic, "micSettingPanel").activeInHierarchy && !mainPanel.activeInHierarchy,
                "Boot: first mic settings open survives its initial Start");
            mic.CloseMicSetting();
            foreach (var script in scripts) if (script != null) script.enabled = false;
        }
        // Count a retire decision without ending the editor or quitting the application.
        var actions = Field<UnityEngine.Events.UnityEvent[]>(main, "onDecideEvents");
        actions[1].SetPersistentListenerState(0, UnityEngine.Events.UnityEventCallState.Off);
        actions[1].AddListener(() => actionCount++);
        keyboard = InputSystem.AddDevice<Keyboard>();
        keyboard.MakeCurrent();

        foreach (bool howToFirst in new[] { false, true })
        {
            ResetView();
            yield return NextFrame();
            SetField(main, "currentIndex", 3);
            main.RefreshSelection();
            yield return NextFrame();
            Press(Key.Enter, howToFirst);
            yield return NextFrame();
            Press(Key.UpArrow, howToFirst);
            yield return NextFrame();
            Press(Key.UpArrow, howToFirst);
            Observe(Field<int>(main, "currentIndex") == 3, $"Hidden main selection stays at 3; howToFirst={howToFirst}; actual={Field<int>(main, "currentIndex")}");
            yield return NextFrame();
            Press(Key.Enter, howToFirst);
            Observe(actionCount == 0, $"HowTo decision never invokes hidden retire; howToFirst={howToFirst}; retire={actionCount}");

            ResetView();
            yield return NextFrame();
            SetField(main, "currentIndex", 3);
            main.RefreshSelection();
            yield return NextFrame();
            Press(Key.Enter, howToFirst);
            yield return NextFrame();
            Press(Key.DownArrow, howToFirst);
            yield return NextFrame();
            Press(Key.Enter, howToFirst);
            Observe(mainPanel.activeInHierarchy && !howToPanel.activeInHierarchy && Time.timeScale == 0f,
                $"Return keeps pause active; howToFirst={howToFirst}; main={mainPanel.activeInHierarchy}; howTo={howToPanel.activeInHierarchy}; scale={Time.timeScale}");
            Observe(Field<GameObject[]>(main, "selectLights")[3].activeSelf && Field<RectTransform>(main, "cursor").gameObject.activeSelf,
                $"Return restores cursor and selected light; howToFirst={howToFirst}");
            Observe(Field<int>(main, "currentIndex") == 3, $"Return preserves main selection; howToFirst={howToFirst}");
        }

        ResetView();
        yield return NextFrame();
        howTo.OpenHowTo();
        yield return NextFrame();
        Press(Key.T, false);
        Observe(!mainPanel.activeInHierarchy && howToPanel.activeInHierarchy, "T does not overlap the child and main panels");
        yield return NextFrame();
        Press(Field<Key>(main, "closeMenuKey"), false);
        yield return WaitForPhase();
        Observe(!mainPanel.activeInHierarchy && !howToPanel.activeInHierarchy && Time.timeScale == 1f,
            "Configured close key closes the whole pause view and restores time");
        yield return NextFrame();
        Press(Key.T, false);
        yield return WaitForPhase();
        Observe(mainPanel.activeInHierarchy && !howToPanel.activeInHierarchy && Time.timeScale == 0f,
            "T reopens only the main view after closing pause");

        ResetView();
        yield return NextFrame();
        SetField(main, "currentIndex", 2);
        main.RefreshSelection();
        yield return NextFrame();
        Press(Key.Enter, false);
        Observe(Field<int>(mic, "sensitivity") == 70, "Opening mic settings does not reuse Enter to change sensitivity");
        yield return NextFrame();
        PressMic(Key.DownArrow, false);
        yield return NextFrame();
        PressMic(Key.Enter, false);
        Observe(mainPanel.activeInHierarchy && !Field<GameObject>(mic, "micSettingPanel").activeInHierarchy &&
                Field<int>(main, "currentIndex") == 2 && Time.timeScale == 0f,
            "Mic settings returns without moving or reopening the hidden main selection");
        Observe(Field<GameObject[]>(main, "selectLights")[2].activeSelf, "Mic settings restores selected light");

        // Exercise the real voice router, bypassing only microphone recognition and its time window.
        var router = scripts.OfType<MenuVoiceCommandAction>().Single();
        ResetView();
        yield return NextFrame();
        howTo.OpenHowTo();
        int hiddenIndex = Field<int>(main, "currentIndex");
        main.OnVoiceCommand("sita");
        Observe(Field<int>(main, "currentIndex") == hiddenIndex, "A direct voice command cannot move a hidden main menu");
        main.enabled = howTo.enabled = mic.enabled = true;
        typeof(MenuVoiceCommandAction).GetMethod("RoutePauseCommand", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(router, new object[] { "sita" });
        Observe(Field<int>(howTo, "currentIndex") == 1 && Field<int>(main, "currentIndex") == hiddenIndex,
            "Voice router sends Down only to the visible HowTo panel");
        typeof(MenuVoiceCommandAction).GetMethod("RoutePauseCommand", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(router, new object[] { "kettei" });
        main.enabled = howTo.enabled = mic.enabled = false;
        Observe(mainPanel.activeInHierarchy && !howToPanel.activeInHierarchy && Time.timeScale == 0f,
            "Voice return restores main view while keeping pause active");

        for (int i = 0; i < 20; i++)
        {
            ResetView();
            yield return NextFrame();
            SetField(main, "currentIndex", 3);
            main.RefreshSelection();
            yield return NextFrame();
            Press(Key.Enter, i % 2 == 0);
            yield return NextFrame();
            Press(Key.RightArrow, i % 2 == 0);
            yield return NextFrame();
            Press(Key.LeftArrow, i % 2 == 0);
            Observe(Field<int>(howTo, "pageIndex") == 0, $"Page next/previous round trip #{i + 1}");
            yield return NextFrame();
            Press(Key.DownArrow, i % 2 == 0);
            yield return NextFrame();
            Press(Key.Enter, i % 2 == 0);
            Observe(mainPanel.activeInHierarchy && !howToPanel.activeInHierarchy && Time.timeScale == 0f && Field<int>(main, "currentIndex") == 3,
                $"Open/page/return round trip #{i + 1}");
        }

        // Restore the real action only after input-leak checks are finished.
        {
            actions[1].SetPersistentListenerState(0, UnityEngine.Events.UnityEventCallState.RuntimeOnly);
            SetField(main, "currentIndex", 1);
            main.RefreshSelection();
            yield return NextFrame();
            Press(Key.Enter, false);
            scripts.OfType<RetireButtonAction>().Single().OnClickRetire();
            if (boot)
            {
                yield return WaitForPhase();
            }
            else
            {
                double deadline = EditorApplication.timeSinceStartup + 30;
                while (SceneManager.GetActiveScene().name != "Title")
                {
                    if (EditorApplication.timeSinceStartup > deadline) throw new TimeoutException("Standalone retire did not load Title");
                    yield return null;
                }
            }
            Observe(EditorApplication.isPlaying, "Retire keeps Play Mode running");
            Observe(SceneManager.GetActiveScene().name == "Title" && (!boot || GamePhaseManager.Instance.CurrentPhase == GamePhase.Title),
                "Retire returns to Title (and synchronizes phase when booted)");
            Observe((!boot || !PauseManager.Instance.IsPaused) && Time.timeScale == 1f, "Retire clears pause and restores time");
            if (boot) Observe(!SceneManager.GetSceneByName("Prototype").isLoaded, "Retire unloads the previous integrated game scene");
        }
    }

    private static void DisableAutomaticUpdates(Scene scene, LoadSceneMode mode)
    {
        if (scene.path != ScenePath) return;
        foreach (var script in scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<MonoBehaviour>(true)))
            if (script != null) script.enabled = false;
    }

    private static IEnumerator NextFrame()
    {
        int frame = Time.frameCount;
        while (Time.frameCount == frame) yield return null;
    }

    private static IEnumerator WaitForPhase()
    {
        double deadline = EditorApplication.timeSinceStartup + 30;
        while (GamePhaseManager.Instance != null && GamePhaseManager.Instance.IsChangingPhase)
        {
            if (EditorApplication.timeSinceStartup > deadline) throw new TimeoutException("Pause phase did not settle");
            yield return null;
        }
    }

    private static void ResetView()
    {
        howTo.CloseHowTo();
        mic.CloseMicSetting();
        main.OpenMenu();
        actionCount = 0;
        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        InputSystem.Update();
    }

    private static void Press(Key key, bool howToFirst)
    {
        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        InputSystem.Update();
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(key));
        InputSystem.Update();
        if (!keyboard[key].wasPressedThisFrame) throw new InvalidOperationException("Synthetic key press was not registered");
        if (howToFirst && howToPanel.activeInHierarchy) Invoke(howTo, "Update");
        Invoke(main, "Update");
        if (!howToFirst && howToPanel.activeInHierarchy) Invoke(howTo, "Update");
        if (Field<GameObject>(mic, "micSettingPanel").activeInHierarchy) Invoke(mic, "Update");
    }

    private static void PressMic(Key key, bool micFirst)
    {
        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        InputSystem.Update();
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(key));
        InputSystem.Update();
        if (micFirst) Invoke(mic, "Update");
        Invoke(main, "Update");
        if (!micFirst && Field<GameObject>(mic, "micSettingPanel").activeInHierarchy) Invoke(mic, "Update");
    }

    private static void Observe(bool pass, string message)
    {
        results.Add((pass ? "PASS: " : "FAIL: ") + message);
        if (!pass) failures++;
    }

    private static T Field<T>(object target, string name) => (T)target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
    private static void SetField(object target, string name, object value) => target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    private static void Invoke(object target, string method) => target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, null);
}
