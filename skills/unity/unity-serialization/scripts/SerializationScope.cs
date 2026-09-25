// The scope of a reserialize after a serialized rename: every asset file that
// serializes a changed type, and the reserialize itself. Editor-only; SKILL.md
// says when to run each entry point. <types> is a comma-separated list of full
// type names, namespace included.
//
// Entry points (how to run C# in the Editor is unity-verification's):
//   Find(types)                   connected editor, from a scratch copy outside Assets/
//   Reserialize(types, pathList)  connected editor; pathList is the approved list, one path per line
//   FindFromCommandLine,          headless, from a copy in an Editor folder under Assets/
//   ReserializeFromCommandLine    (removed after, with its .meta), reading
//                                   -serializationScopeTypes <types> -serializationScopeOut <file>
//                                   and, to reserialize, -serializationScopePaths <file>
//
// Find returns one project-relative path per line, sorted; lines starting with "#" are notes.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class SerializationScope
{
    public static void FindFromCommandLine() =>
        FromCommandLine(() => Find(Arg("-serializationScopeTypes")));

    public static void ReserializeFromCommandLine() =>
        FromCommandLine(() => Reserialize(Arg("-serializationScopeTypes"), File.ReadAllText(Arg("-serializationScopePaths"))));

    static void FromCommandLine(Func<string> run)
    {
        try
        {
            File.WriteAllText(Arg("-serializationScopeOut"), run());
            EditorApplication.Exit(0);
        }
        catch (Exception e)
        {
            Debug.LogError("[SerializationScope] " + e);
            EditorApplication.Exit(1);
        }
    }

    // Rewrites the approved files with ForceReserializeAssets. It leaves a scene's prefab
    // instance overrides on the old propertyPath, so each listed scene is then opened and
    // those overrides re-recorded under the new name before it is saved. It refuses while an
    // open scene has unsaved changes, and reopens the user's scenes after.
    public static string Reserialize(string typeNames, string paths)
    {
        var hosts = Hosts(Targets(typeNames));
        var list = paths.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Trim()).Where(p => p.Length > 0 && !p.StartsWith("#")).ToArray();
        var scenes = list.Where(p => p.EndsWith(".unity", StringComparison.OrdinalIgnoreCase)).ToArray();
        var open = EditorSceneManager.GetSceneManagerSetup();
        if (scenes.Length > 0 && Enumerable.Range(0, EditorSceneManager.sceneCount).Any(i => EditorSceneManager.GetSceneAt(i).isDirty))
            throw new InvalidOperationException("an open scene has unsaved changes: ask the user to save or discard them first");
        AssetDatabase.ForceReserializeAssets(list);

        int recorded = 0;
        foreach (var path in scenes)
        {
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            foreach (var root in scene.GetRootGameObjects())
                foreach (var component in root.GetComponentsInChildren<MonoBehaviour>(true))
                    if (component != null && hosts.Contains(component.GetType()) && PrefabUtility.IsPartOfPrefabInstance(component))
                    {
                        PrefabUtility.RecordPrefabInstancePropertyModifications(component);
                        recorded++;
                    }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
        if (scenes.Length > 0)
        {
            if (open.Length > 0 && open.All(s => !string.IsNullOrEmpty(s.path))) EditorSceneManager.RestoreSceneManagerSetup(open);
            else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }
        return $"reserialized {list.Length} files; re-recorded {recorded} prefab instance components in {scenes.Length} scenes";
    }

    public static string Find(string typeNames)
    {
        var notes = new List<string>();
        // 1. Type closure.
        var targets = Targets(typeNames);
        var hosts = Hosts(targets);

        // 2. Types to script GUIDs. A .cs script is always fileID 11500000; a DLL's scripts carry their own.
        var needles = new HashSet<string>();
        var scripts = MonoImporter.GetAllRuntimeMonoScripts()
            .Concat(AssetDatabase.FindAssets("t:MonoScript").Select(g => AssetDatabase.LoadAssetAtPath<MonoScript>(AssetDatabase.GUIDToAssetPath(g))))
            .Where(s => s != null);
        var found = new HashSet<Type>();
        foreach (var script in scripts)
        {
            var type = script.GetClass();
            if (type == null || !hosts.Contains(type)) continue;
            if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(script, out var guid, out long fileId)) continue;
            needles.Add($"fileID: {fileId}, guid: {guid}");
            found.Add(type);
        }
        var projectAssemblies = CompilationPipeline.GetAssemblies()
            .Where(a => a.sourceFiles.Any(f => !Path.GetFullPath(f).Replace('\\', '/').Contains("/Library/PackageCache/")))
            .Select(a => a.name)
            .ToHashSet();
        foreach (var type in hosts.Where(t => !found.Contains(t) && !t.IsAbstract && projectAssemblies.Contains(t.Assembly.GetName().Name)))
            notes.Add($"# no script asset for {type.FullName}: files using it cannot be found by GUID (file name must match the class name)");

        // 3. GUIDs to files, to a fixpoint: every matched prefab adds its own GUID, which pulls in
        // nested instances, variants and scene instances whose overrides may carry the old name.
        var files = Directory.EnumerateFiles("Assets", "*", SearchOption.AllDirectories)
            .Concat(Directory.Exists("Packages") ? Directory.EnumerateFiles("Packages", "*", SearchOption.AllDirectories) : Enumerable.Empty<string>())
            .Where(p => !p.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
            .Select(p => p.Replace('\\', '/'))
            .Select(p => (path: p, text: ReadUnityYaml(p)))
            .Where(f => f.text != null)
            .ToList();

        var matched = new SortedSet<string>(StringComparer.Ordinal);
        bool grew = true;
        while (grew)
        {
            grew = false;
            foreach (var (path, text) in files)
            {
                if (matched.Contains(path) || !needles.Any(text.Contains)) continue;
                matched.Add(path);
                grew = true;
                if (!path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase)) continue;
                var prefabGuid = AssetDatabase.AssetPathToGUID(path);
                if (prefabGuid.Length > 0) needles.Add("guid: " + prefabGuid);
                else notes.Add($"# no GUID for {path}: files nesting it were not followed");
            }
        }

        // 4. Output.
        notes.Insert(0, $"# {matched.Count} files serialize {string.Join(", ", targets.Select(t => t.FullName))} ({hosts.Count} host types)");
        return string.Join("\n", notes.Concat(matched));
    }

    static string Arg(string name)
    {
        var args = Environment.GetCommandLineArgs();
        int i = Array.IndexOf(args, name);
        if (i < 0 || i + 1 >= args.Length) throw new ArgumentException("missing " + name);
        return args[i + 1];
    }

    static Type[] Targets(string typeNames) =>
        typeNames.Split(',').Select(n => n.Trim()).Where(n => n.Length > 0).Select(Resolve).ToArray();

    // Type closure: every MonoBehaviour and ScriptableObject type that serializes a target at any depth.
    static HashSet<Type> Hosts(Type[] targets)
    {
        var closure = new Closure(targets);
        return TypeCache.GetTypesDerivedFrom<MonoBehaviour>()
            .Concat(TypeCache.GetTypesDerivedFrom<ScriptableObject>())
            .Concat(targets.Where(t => typeof(UnityEngine.Object).IsAssignableFrom(t)))
            .Where(closure.Hosts)
            .ToHashSet();
    }

    static Type Resolve(string name)
    {
        var type = Type.GetType(name) ?? AppDomain.CurrentDomain.GetAssemblies()
            .Select(a => a.GetType(name)).FirstOrDefault(t => t != null);
        return type ?? throw new ArgumentException("type not found: " + name + " (use the full name, namespace included)");
    }

    static string ReadUnityYaml(string path)
    {
        try
        {
            using (var reader = new StreamReader(path))
            {
                var buffer = new char[5];
                if (reader.Read(buffer, 0, 5) != 5 || new string(buffer) != "%YAML") return null;
                return "%YAML" + reader.ReadToEnd();
            }
        }
        catch (IOException) { return null; }
    }

    // Unity's field rules, applied recursively from a host type down to the targets.
    class Closure
    {
        readonly Type[] targets;
        readonly Dictionary<Type, bool> memo = new Dictionary<Type, bool>();

        public Closure(Type[] targets) => this.targets = targets;

        public bool Hosts(Type host) =>
            targets.Any(t => t.IsAssignableFrom(host)) || Contains(host);

        // Does a value of this type carry a target in its serialized data?
        bool Contains(Type type)
        {
            if (memo.TryGetValue(type, out var known)) return known;
            memo[type] = false; // cycle guard: a type is not assumed to contain itself
            bool result = SerializedFields(type).Any(CarriesTarget);
            memo[type] = result;
            return result;
        }

        bool CarriesTarget(FieldInfo field)
        {
            bool byReference = field.IsDefined(typeof(SerializeReference), true);
            foreach (var element in Elements(field.FieldType))
            {
                if (typeof(UnityEngine.Object).IsAssignableFrom(element)) continue; // a reference, not inline data
                if (!byReference)
                {
                    if (Matches(element) || (IsSerializableCustom(element) && Contains(element))) return true;
                    continue;
                }
                // [SerializeReference]: the stored object can be any serializable type assignable to the field.
                if (targets.Any(t => !typeof(UnityEngine.Object).IsAssignableFrom(t) && element.IsAssignableFrom(t))) return true;
                var candidates = TypeCache.GetTypesDerivedFrom(element).Append(element)
                    .Where(c => !c.IsAbstract && !c.IsInterface && IsSerializableCustom(c));
                if (candidates.Any(c => Matches(c) || Contains(c))) return true;
            }
            return false;
        }

        bool Matches(Type type) => targets.Any(t => t.IsAssignableFrom(type));

        static bool IsSerializableCustom(Type type) =>
            !type.IsPrimitive && !type.IsEnum && type != typeof(string) && type.IsDefined(typeof(SerializableAttribute), false);

        static IEnumerable<Type> Elements(Type type)
        {
            if (type.IsArray) return new[] { type.GetElementType() };
            if (type.IsGenericType)
            {
                var definition = type.GetGenericTypeDefinition();
                if (definition == typeof(List<>) || definition == typeof(Dictionary<,>)) return type.GetGenericArguments();
            }
            return new[] { type };
        }

        static IEnumerable<FieldInfo> SerializedFields(Type type)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
            for (var t = type; t != null && t != typeof(object) && t != typeof(MonoBehaviour) && t != typeof(ScriptableObject); t = t.BaseType)
                foreach (var f in t.GetFields(flags))
                    if (!f.IsStatic && !f.IsLiteral && !f.IsInitOnly && !f.IsNotSerialized &&
                        (f.IsPublic || f.IsDefined(typeof(SerializeField), true) || f.IsDefined(typeof(SerializeReference), true)))
                        yield return f;
        }
    }
}
