using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// This editor-only script makes the scene reproducible from one menu click.
public static class SceneBuilder
{
    private const string ScenePath = "Assets/Scenes/RollABall.unity";

    [MenuItem("Tools/Build Roll-a-Ball Scene")]
    public static void BuildScene()
    {
        EnsureFolders();
        EnsurePlayerTag();
        TryImportTMPResources();
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Material groundMat = MakeMaterial("Ground", new Color(0.18f, 0.48f, 0.22f));
        Material wallMat = MakeMaterial("Walls", new Color(0.32f, 0.36f, 0.4f));
        Material coinMat = MakeMaterial("Coins", Color.yellow);
        Material hazardMat = MakeMaterial("Hazards", Color.red);
        Material playerMat = MakeMaterial("Player", new Color(0.12f, 0.4f, 0.95f));

        CreateCube("Arena Ground", new Vector3(0, -0.5f, 0), new Vector3(20, 1, 20), groundMat);
        CreateCube("North Wall", new Vector3(0, 1, 10), new Vector3(20, 2, 0.5f), wallMat);
        CreateCube("South Wall", new Vector3(0, 1, -10), new Vector3(20, 2, 0.5f), wallMat);
        CreateCube("East Wall", new Vector3(10, 1, 0), new Vector3(0.5f, 2, 20), wallMat);
        CreateCube("West Wall", new Vector3(-10, 1, 0), new Vector3(0.5f, 2, 20), wallMat);

        GameObject player = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        player.name = "Player"; player.tag = "Player"; player.transform.position = new Vector3(0, 0.7f, -7);
        player.GetComponent<Renderer>().sharedMaterial = playerMat;
        Rigidbody body = player.AddComponent<Rigidbody>(); body.mass = 1f;
        // Keep vertical movement free so jumping and the fall-loss condition both work.
        body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        player.AddComponent<PlayerController>();

        Vector3[] coinSpots = { new(-7, .45f, -6), new(-3, .45f, -6), new(2, .45f, -6), new(7, .45f, -6),
            new(-7, .45f, -2), new(-2, .45f, -2), new(3, .45f, -2), new(7, .45f, -1),
            new(-7, .45f, 4), new(-2, .45f, 5), new(3, .45f, 5), new(7, .45f, 6) };
        foreach (Vector3 pos in coinSpots)
        {
            GameObject coin = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            coin.name = "Coin"; coin.transform.position = pos; coin.transform.localScale = new Vector3(.8f, .08f, .8f);
            coin.GetComponent<Renderer>().sharedMaterial = coinMat;
            coin.GetComponent<Collider>().isTrigger = true;
            coin.AddComponent<CoinPickup>();
        }

        AddHazard("Static Hazard 1", new Vector3(-5, .5f, 0), new Vector3(-5, .5f, 0), hazardMat, Hazard.HazardMode.Static);
        AddHazard("Static Hazard 2", new Vector3(5, .5f, 2), new Vector3(5, .5f, 2), hazardMat, Hazard.HazardMode.Static);
        AddHazard("Moving Hazard 1", new Vector3(-6, .5f, 2), new Vector3(-2, .5f, 2), hazardMat, Hazard.HazardMode.Moving);
        AddHazard("Moving Hazard 2", new Vector3(1, .5f, 1), new Vector3(6, .5f, 1), hazardMat, Hazard.HazardMode.Moving);
        AddHazard("Moving Hazard 3", new Vector3(-4, .5f, 7), new Vector3(4, .5f, 7), hazardMat, Hazard.HazardMode.Moving);

        GameObject cameraObject = new GameObject("Main Camera"); cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.AddComponent<Camera>(); camera.fieldOfView = 60;
        cameraObject.AddComponent<AudioListener>(); cameraObject.transform.position = new Vector3(0, 9, -16);
        CameraFollow follow = cameraObject.AddComponent<CameraFollow>(); follow.target = player.transform;
        GameObject lightObject = new GameObject("Directional Light");
        Light light = lightObject.AddComponent<Light>(); light.type = LightType.Directional; light.intensity = 1.2f;
        lightObject.transform.rotation = Quaternion.Euler(50, -30, 0);

        GameManager manager = new GameObject("GameManager").AddComponent<GameManager>();
        manager.totalCoins = coinSpots.Length; manager.timeLimit = 60;
        BuildUI(manager);
        EditorSceneManager.SaveScene(scene, ScenePath);
        var buildScenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        buildScenes.RemoveAll(item => item.path == ScenePath);
        buildScenes.Add(new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = buildScenes.ToArray();
        AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
        if (!Application.isBatchMode)
            EditorUtility.DisplayDialog("Roll-a-Ball Ready", "Scene created at " + ScenePath, "OK");
    }

    private static void AddHazard(string name, Vector3 a, Vector3 b, Material mat, Hazard.HazardMode mode)
    {
        GameObject hazard = GameObject.CreatePrimitive(PrimitiveType.Cube);
        hazard.name = name; hazard.transform.position = a; hazard.transform.localScale = new Vector3(1.5f, 1, 1.5f);
        hazard.GetComponent<Renderer>().sharedMaterial = mat; hazard.GetComponent<Collider>().isTrigger = true;
        Hazard script = hazard.AddComponent<Hazard>(); script.mode = mode; script.pointA = a; script.pointB = b;
    }

    private static GameObject CreateCube(string name, Vector3 pos, Vector3 scale, Material mat)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name; cube.transform.position = pos; cube.transform.localScale = scale;
        cube.GetComponent<Renderer>().sharedMaterial = mat; return cube;
    }

    private static Material MakeMaterial(string name, Color color)
    {
        string path = "Assets/Materials/" + name + ".mat";
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null) { existing.color = color; EditorUtility.SetDirty(existing); return existing; }
        // The project targets URP, so materials use its Lit shader and preserve the palette.
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            throw new System.InvalidOperationException("Universal Render Pipeline/Lit shader was not found. Check that URP is installed and active.");
        Material material = new Material(shader); material.name = name; material.color = color;
        AssetDatabase.CreateAsset(material, path); return material;
    }

    private static void BuildUI(GameManager manager)
    {
        GameObject canvasObject = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObject.GetComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        manager.scoreText = MakeText(canvas.transform, "Score", "Coins: 0 / 12", new Vector2(20, -20), new Vector2(0, 1), new Vector2(0, 1), 26, TextAlignmentOptions.TopLeft);
        manager.timerText = MakeText(canvas.transform, "Timer", "Time: 60", new Vector2(-20, -20), new Vector2(1, 1), new Vector2(1, 1), 26, TextAlignmentOptions.TopRight);
        manager.messageText = MakeText(canvas.transform, "Message", "", Vector2.zero, new Vector2(.5f, .5f), new Vector2(.5f, .5f), 42, TextAlignmentOptions.Center);
        TMP_Text hint = MakeText(canvas.transform, "Controls", "WASD = Move, Space = Jump, R = Restart", new Vector2(0, 20), new Vector2(.5f, 0), new Vector2(.5f, 0), 20, TextAlignmentOptions.Center);
        hint.rectTransform.sizeDelta = new Vector2(600, 40);
    }

    private static TMP_Text MakeText(Transform parent, string name, string value, Vector2 position,
        Vector2 anchor, Vector2 pivot, float size, TextAlignmentOptions alignment)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.GetComponent<RectTransform>(); rect.anchorMin = anchor; rect.anchorMax = anchor;
        rect.pivot = pivot; rect.anchoredPosition = position; rect.sizeDelta = new Vector2(500, 70);
        TextMeshProUGUI text = obj.GetComponent<TextMeshProUGUI>(); text.text = value;
        text.fontSize = size; text.color = Color.white; text.alignment = alignment;
        return text;
    }

    private static void EnsureFolders()
    {
        foreach (string folder in new[] { "Assets/Scenes", "Assets/Materials" })
            if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets", Path.GetFileName(folder));
    }

    private static void EnsurePlayerTag()
    {
        if (UnityEditorInternal.InternalEditorUtility.tags.Contains("Player")) return;
        SerializedObject tags = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty list = tags.FindProperty("tags"); list.InsertArrayElementAtIndex(list.arraySize);
        list.GetArrayElementAtIndex(list.arraySize - 1).stringValue = "Player"; tags.ApplyModifiedProperties();
    }

    // Reflection keeps this builder compiling even when TMP's optional editor importer is absent.
    private static void TryImportTMPResources()
    {
        System.Type importer = System.Type.GetType("TMPro.TMP_PackageResourceImporter, Unity.TextMeshPro.Editor");
        MethodInfo method = importer == null ? null : importer.GetMethod("ImportProjectResources", BindingFlags.Public | BindingFlags.Static);
        if (method != null)
        {
            try { method.Invoke(null, null); }
            catch (System.Exception exception) { Debug.LogWarning("TMP resources were not imported automatically: " + exception.Message); }
        }
    }
}
