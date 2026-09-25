# Prototype templates

Starting points for a prototype's menu item, logic driver and variant switcher, in the prototype's own assembly (`defineConstraints: ["UNITY_EDITOR"]`, so `UnityEditor` needs no `#if`). Replace every `<...>` placeholder: `<Name>` (the prototype), `<Host>` (the host scene), `<Model>`, `<Field>` and `<Action>` (the pure class and its members). Without an asmdef, wrap each file whole in `#if UNITY_EDITOR`.

## Menu item

```csharp
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace Prototypes.<Name>
{
    static class PrototypeMenu
    {
        const string PrototypeScene = "Assets/_Prototypes/<Name>/<Name>.unity";
        const string HostScene = "Assets/Scenes/<Host>.unity"; // a standalone greybox scene drops this line and the additive OpenScene

        [MenuItem("Prototypes/<Name>")]
        static void Open()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var prototype = EditorSceneManager.OpenScene(PrototypeScene, OpenSceneMode.Single);
            EditorSceneManager.OpenScene(HostScene, OpenSceneMode.Additive);
            SceneManager.SetActiveScene(prototype);
            EditorApplication.EnterPlaymode();
        }
    }
}
```

A logic prototype's menu item opens the driver instead, with no Play:

```csharp
[MenuItem("Prototypes/<Name>")]
static void Open() => EditorWindow.GetWindow<<Name>Driver>("<Name>");
```

## Logic driver

The pure class holds the logic and uses no `UnityEngine` or `UnityEditor` type; the window is a thin shell over it. Label everything in domain language.

```csharp
using UnityEditor;
using UnityEngine;

namespace Prototypes.<Name>
{
    sealed class <Name>Driver : EditorWindow
    {
        <Model> model = new <Model>();
        int tab;
        int step;

        static readonly string[] Scenarios = { "Happy path", "Edge case", "Should be illegal" };

        void OnGUI()
        {
            EditorGUILayout.HelpBox("<The question this prototype answers.>", MessageType.None);

            EditorGUILayout.LabelField("Current state", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("<Field>", model.<Field>.ToString());

            EditorGUILayout.LabelField("Free play", EditorStyles.boldLabel);
            if (GUILayout.Button("<Action>")) model.<Action>();

            EditorGUILayout.Space();
            var picked = GUILayout.Toolbar(tab, Scenarios);
            if (picked != tab) { tab = picked; step = 0; model = new <Model>(); }
            // One ordered button per step of the chosen scenario; each performs its action and advances `step`.
        }
    }
}
```

## Variant switcher

Put it on a GameObject in the prototype scene and fill `variants` with each variant's root. It draws with IMGUI and reads keys from IMGUI events, and skips the arrow keys while an IMGUI text field has keyboard focus. When the prototype has uGUI or UI Toolkit text fields, extend the guard with their focus check (unverified here: the selected input field's `isFocused`, or the panel's focused element being a `TextField`).

```csharp
using UnityEngine;

namespace Prototypes.<Name>
{
    public sealed class PrototypeSwitcher : MonoBehaviour
    {
        public GameObject[] variants;
        public int Current { get; private set; }

        void Start() => Show(0);

        void Step(int delta) => Show(Current + delta);

        public void Show(int index)
        {
            if (variants == null || variants.Length == 0) { Debug.LogWarning("[prototype] no variants assigned"); return; }
            Current = (index % variants.Length + variants.Length) % variants.Length;
            for (int i = 0; i < variants.Length; i++) variants[i].SetActive(i == Current);
            Debug.Log($"[prototype] variant {Current}: {variants[Current].name}");
        }

        void OnGUI()
        {
            var e = Event.current;
            if (e.type == EventType.KeyDown && GUIUtility.keyboardControl == 0)
            {
                if (e.keyCode == KeyCode.LeftArrow) { Step(-1); e.Use(); }
                else if (e.keyCode == KeyCode.RightArrow) { Step(1); e.Use(); }
            }

            if (variants == null || variants.Length == 0) return;
            const float width = 320, height = 36;
            var bar = new Rect((Screen.width - width) / 2, Screen.height - height - 12, width, height);
            GUI.Box(bar, GUIContent.none);
            if (GUI.Button(new Rect(bar.x + 4, bar.y + 4, 40, height - 8), "<")) Step(-1);
            GUI.Label(new Rect(bar.x + 50, bar.y + 8, width - 100, height - 8), $"{Current + 1}/{variants.Length}: {variants[Current].name}");
            if (GUI.Button(new Rect(bar.xMax - 44, bar.y + 4, 40, height - 8), ">")) Step(1);
        }
    }
}
```
