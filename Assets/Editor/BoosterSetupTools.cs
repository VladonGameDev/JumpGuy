using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using System.Collections.Generic;
using System.IO;

public static class BoosterSetupTools
{
    private const string ModelFolder = "Assets/Models/Boosters";
    private const string PrefabFolder = "Assets/Prefabs/Boosts";
    private const string IconFolder = "Assets/UI/Boosters";
    private const int IconSize = 256;
    private const int IconPadding = 10;

    private const string PackRoot = "Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Sprite_Common";
    private const string PackHud = "Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Sprite_Common/HUD";
    private const string PackSlider = "Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Sprite_Common/Slider";

    private static readonly Vector3 IconOrigin = new Vector3(100000f, 100000f, 100000f);

    // Лёгкий трёхчетвертной разворот: иконка читается как объёмная модель,
    // но силуэт плоских воксельных фигур ещё не «схлопывается» в ребро.
    private const float IconYaw = 16f;
    private const float IconPitch = 7f;

    private struct BoosterDef
    {
        public string modelFile;   // файл .obj в ModelFolder
        public string assetName;   // имя иконки (Icon_<assetName>.png) и префаба
        public string prefabName;  // имя префаба без расширения
        public BoosterType type;
        public float colliderHeight;
        public float colliderRadius;
        public Color tint;

        public BoosterDef(string modelFile, string assetName, string prefabName, BoosterType type, float height, float radius, Color tint)
        {
            this.modelFile = modelFile;
            this.assetName = assetName;
            this.prefabName = prefabName;
            this.type = type;
            this.colliderHeight = height;
            this.colliderRadius = radius;
            this.tint = tint;
        }
    }

    // Модели X05_1 / X05_2 — это «половинные» бустеры (HalfCoins / HalfScore),
    // поэтому файл модели и имя иконки у них расходятся.
    private static readonly BoosterDef[] Defs = new BoosterDef[]
    {
        new BoosterDef("Magnet", "Magnet", "MagnetBooster", BoosterType.Magnet, 1.4f, 0.65f, new Color(0.68f, 0.36f, 0.96f)),
        new BoosterDef("Shield", "Shield", "ShieldBooster", BoosterType.Shield, 1.9f, 0.95f, new Color(0.30f, 0.78f, 1f)),
        new BoosterDef("DoubleCoins", "DoubleCoins", "DoubleCoinsBooster", BoosterType.DoubleCoins, 1.7f, 0.62f, new Color(1f, 0.78f, 0.20f)),
        new BoosterDef("DoubleScore", "DoubleScore", "DoubleScoreBooster", BoosterType.DoubleScore, 1.8f, 0.62f, new Color(0.40f, 0.96f, 0.52f)),
        new BoosterDef("Timer", "Timer", "TimerBooster", BoosterType.Timer, 1.8f, 0.90f, new Color(0.13f, 0.90f, 1f)),
        new BoosterDef("X05_1", "HalfCoins", "HalfCoinsBooster", BoosterType.HalfCoins, 1.9f, 0.62f, new Color(1f, 0.78f, 0.20f)),
        new BoosterDef("X05_2", "HalfScore", "HalfScoreBooster", BoosterType.HalfScore, 1.9f, 0.62f, new Color(0.40f, 0.96f, 0.52f)),
        new BoosterDef("GlitchCube", "GlitchCube", "GlitchCube", BoosterType.GlitchCube, 1.4f, 0.99f, new Color(1f, 0.16f, 0.60f))
    };

    // Глич-куб спавнится по сейф-зонам, а Half-бустеры выдаёт сам куб,
    // поэтому в обычный пул спавна они не входят.
    private static bool IsSpawnableInPool(BoosterType type)
    {
        return type == BoosterType.Magnet
            || type == BoosterType.Shield
            || type == BoosterType.DoubleCoins
            || type == BoosterType.DoubleScore
            || type == BoosterType.Timer;
    }

    // У глич-куба нет слота в панели активных бустеров.
    private static bool IsUiBooster(BoosterType type)
    {
        return type != BoosterType.GlitchCube;
    }

    [MenuItem("JumpGuy/Boosters/1. Generate Icons")]
    public static void GenerateIcons()
    {
        EnsureFolder("Assets/UI", "Boosters");
        string absDir = Path.Combine(Application.dataPath, "UI", "Boosters");

        UnityEngine.Rendering.AmbientMode bakMode = RenderSettings.ambientMode;
        Color bakAmb = RenderSettings.ambientLight;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.40f, 0.40f, 0.46f, 1f);

        GameObject camGO = new GameObject("__IconCam");
        camGO.hideFlags = HideFlags.HideAndDontSave;
        Camera cam = camGO.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
        cam.orthographic = true;
        cam.enabled = false;
        cam.allowHDR = false;
        cam.allowMSAA = true;
        cam.rect = new Rect(0f, 0f, 1f, 1f);
        cam.nearClipPlane = 0.01f;
        cam.farClipPlane = 100f;
        camGO.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>().renderPostProcessing = false;

        GameObject lightGO = new GameObject("__IconLight");
        lightGO.hideFlags = HideFlags.HideAndDontSave;
        Light light = lightGO.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.15f;
        light.cullingMask = ~0;
        lightGO.transform.position = IconOrigin + new Vector3(0f, 0f, -20f);
        // Ключевой свет доворачиваем вместе с камерой, иначе у моделей,
        // развёрнутых в одну сторону, тени падали бы по-разному.
        lightGO.transform.rotation = Quaternion.Euler(12f + IconPitch, IconYaw + 22f, 0f);

        float[] margins = new float[] { 1.10f, 1.16f, 1.22f, 1.30f, 1.42f, 1.60f, 1.85f, 2.10f, 2.45f, 2.90f };

        foreach (BoosterDef def in Defs)
        {
            GameObject modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(ModelFolder + "/" + def.modelFile + ".obj");
            if (modelAsset == null)
            {
                Debug.LogWarning("[BoosterSetup] model missing: " + def.modelFile);
                continue;
            }

            byte[] chosen = null;
            string chosenInfo = "none";

            foreach (float margin in margins)
            {
                IconStats stats;
                byte[] png = RenderModel(modelAsset, cam, margin, out stats);
                if (png == null) continue;

                bool fits = stats.fitsWithPadding(IconSize, IconPadding);
                chosen = png;
                chosenInfo = "margin=" + margin.ToString("F2") + " bbox=(" + stats.minX + "," + stats.minY + ")-(" + stats.maxX + "," + stats.maxY + ") cover=" + stats.coverage.ToString("P1") + " fits=" + fits;

                if (fits && stats.coverage >= 0.06f) break;
            }

            if (chosen == null)
            {
                Debug.LogWarning("[BoosterSetup] render failed: " + def.modelFile);
                continue;
            }

            File.WriteAllBytes(Path.Combine(absDir, "Icon_" + def.assetName + ".png"), chosen);
            Debug.Log("[BoosterSetup] icon " + def.assetName + " <- " + def.modelFile + ".obj " + chosenInfo);
        }

        RenderSettings.ambientMode = bakMode;
        RenderSettings.ambientLight = bakAmb;
        Object.DestroyImmediate(camGO);
        Object.DestroyImmediate(lightGO);

        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        foreach (BoosterDef def in Defs)
        {
            string path = IconFolder + "/Icon_" + def.assetName + ".png";
            TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti == null) continue;

            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.alphaIsTransparency = true;
            ti.mipmapEnabled = false;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.filterMode = FilterMode.Bilinear;
            ti.maxTextureSize = 512;
            ti.SaveAndReimport();
        }

        AssetDatabase.SaveAssets();
    }

    private struct IconStats
    {
        public int minX;
        public int minY;
        public int maxX;
        public int maxY;
        public float coverage;

        public bool fitsWithPadding(int size, int pad)
        {
            if (maxX < minX || maxY < minY) return false;
            return minX >= pad && minY >= pad && maxX <= size - 1 - pad && maxY <= size - 1 - pad;
        }
    }

    private static byte[] RenderModel(GameObject src, Camera cam, float margin, out IconStats stats)
    {
        stats = new IconStats();
        stats.minX = IconSize;
        stats.minY = IconSize;
        stats.maxX = -1;
        stats.maxY = -1;

        GameObject inst = (GameObject)Object.Instantiate(src);
        inst.hideFlags = HideFlags.HideAndDontSave;
        inst.transform.position = IconOrigin;
        inst.transform.rotation = Quaternion.identity;
        inst.transform.localScale = Vector3.one;

        Renderer[] renderers = inst.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            Object.DestroyImmediate(inst);
            return null;
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);

        float extent = Mathf.Max(bounds.extents.x, bounds.extents.y);
        cam.orthographicSize = Mathf.Max(0.0001f, extent) * margin;
        cam.aspect = 1f;

        Vector3 viewDir = Quaternion.Euler(IconPitch, IconYaw, 0f) * Vector3.back;
        cam.transform.position = bounds.center + viewDir * 10f;
        cam.transform.rotation = Quaternion.LookRotation(-viewDir, Vector3.up);

        RenderTexture rt = RenderTexture.GetTemporary(IconSize, IconSize, 24, RenderTextureFormat.ARGB32);
        RenderTexture prev = RenderTexture.active;
        cam.targetTexture = rt;
        cam.Render();
        RenderTexture.active = rt;

        Texture2D tex = new Texture2D(IconSize, IconSize, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, IconSize, IconSize), 0, 0);
        tex.Apply();

        RenderTexture.active = prev;
        cam.targetTexture = null;
        RenderTexture.ReleaseTemporary(rt);
        Object.DestroyImmediate(inst);

        Color32[] px = tex.GetPixels32();
        int opaque = 0;

        for (int y = 0; y < IconSize; y++)
        {
            for (int x = 0; x < IconSize; x++)
            {
                if (px[y * IconSize + x].a <= 8) continue;
                opaque++;
                if (x < stats.minX) stats.minX = x;
                if (x > stats.maxX) stats.maxX = x;
                if (y < stats.minY) stats.minY = y;
                if (y > stats.maxY) stats.maxY = y;
            }
        }

        stats.coverage = (float)opaque / (IconSize * IconSize);

        byte[] png = tex.EncodeToPNG();
        Object.DestroyImmediate(tex);
        return png;
    }

    [MenuItem("JumpGuy/Boosters/2. Create Prefabs")]
    public static void CreatePrefabs()
    {
        EnsureFolder("Assets/Prefabs", "Boosts");
        int count = 0;

        foreach (BoosterDef def in Defs)
        {
            string path = PrefabFolder + "/" + def.prefabName + ".prefab";

            // Уже настроенные префабы не пересобираем: у глич-куба внутри лежит
            // настроенный список goodPrefabs/badPrefabs, его потерять нельзя.
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
            {
                Debug.Log("[BoosterSetup] prefab exists, skipped: " + path);
                continue;
            }

            GameObject modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(ModelFolder + "/" + def.modelFile + ".obj");
            if (modelAsset == null)
            {
                Debug.LogWarning("[BoosterSetup] model missing: " + def.modelFile);
                continue;
            }

            GameObject root = new GameObject(def.assetName);
            root.tag = "Untagged";
            root.layer = 0;

            GameObject visual = (GameObject)Object.Instantiate(modelAsset, root.transform);
            visual.name = "Visual";
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);
            visual.transform.localPosition = Vector3.zero;

            Renderer[] visualRenderers = visual.GetComponentsInChildren<Renderer>();
            Bounds bounds;
            if (visualRenderers.Length > 0)
            {
                bounds = visualRenderers[0].bounds;
                for (int i = 1; i < visualRenderers.Length; i++) bounds.Encapsulate(visualRenderers[i].bounds);
            }
            else
            {
                bounds = new Bounds(Vector3.zero, new Vector3(1f, 1f, 1f));
            }

            visual.transform.localPosition = new Vector3(
                visual.transform.localPosition.x - bounds.center.x,
                visual.transform.localPosition.y - bounds.min.y,
                visual.transform.localPosition.z - bounds.center.z);

            float meshHeight = Mathf.Max(0.1f, bounds.size.y);
            float meshRadius = Mathf.Max(0.1f, Mathf.Max(bounds.extents.x, bounds.extents.z));

            CapsuleCollider collider = root.AddComponent<CapsuleCollider>();
            collider.isTrigger = true;
            collider.direction = 1;
            collider.height = meshHeight;
            collider.radius = meshRadius;
            collider.center = new Vector3(0f, meshHeight * 0.5f, 0f);

            Booster booster = root.AddComponent<Booster>();
            booster.boosterType = def.type;
            booster.rotationSpeed = 120f;

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);

            if (prefab != null)
            {
                count++;
                Debug.Log("[BoosterSetup] prefab " + path + " type=" + def.type);
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[BoosterSetup] created " + count + " prefabs");
    }

    [MenuItem("JumpGuy/Boosters/3. Wire Into Scene")]
    public static void WireIntoScene()
    {
        BoosterManager manager = Object.FindObjectOfType<BoosterManager>();
        BoosterSpawner spawner = Object.FindObjectOfType<BoosterSpawner>();

        if (manager == null)
        {
            GameObject host = null;
            RunProgressionManager progression = Object.FindObjectOfType<RunProgressionManager>();
            if (progression != null) host = progression.gameObject;
            else
            {
                StatsManager stats = Object.FindObjectOfType<StatsManager>();
                if (stats != null) host = stats.gameObject;
            }
            if (host == null)
            {
                Debug.LogError("[BoosterSetup] no host GameObject for BoosterManager");
                return;
            }
            manager = host.AddComponent<BoosterManager>();
            Debug.Log("[BoosterSetup] added BoosterManager to " + host.name);
        }

        if (manager.boosterDurations == null) manager.boosterDurations = new List<BoosterManager.BoosterDuration>();

        if (manager.boosterDurations.Count == 0)
        {
            manager.boosterDurations.Add(new BoosterManager.BoosterDuration { type = BoosterType.Magnet, duration = 12f });
            manager.boosterDurations.Add(new BoosterManager.BoosterDuration { type = BoosterType.Shield, duration = 10f });
            manager.boosterDurations.Add(new BoosterManager.BoosterDuration { type = BoosterType.DoubleCoins, duration = 15f });
            manager.boosterDurations.Add(new BoosterManager.BoosterDuration { type = BoosterType.DoubleScore, duration = 15f });
        }

        if (spawner == null)
        {
            GameObject host = manager.gameObject;
            spawner = host.AddComponent<BoosterSpawner>();
            Debug.Log("[BoosterSetup] added BoosterSpawner to " + host.name);
        }

        List<GameObject> prefabs = new List<GameObject>();
        foreach (BoosterDef def in Defs)
        {
            if (!IsSpawnableInPool(def.type)) continue;

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "/" + def.prefabName + ".prefab");
            if (prefab != null) prefabs.Add(prefab);
        }
        spawner.boosterPrefabs = prefabs.ToArray();

        foreach (BoosterDef def in Defs)
        {
            if (def.type != BoosterType.GlitchCube) continue;

            GameObject cube = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "/" + def.prefabName + ".prefab");
            if (cube != null) spawner.glitchCubePrefab = cube;
        }

        spawner.worldSpeed = 5f;
        spawner.spawnYPosition = 1.35f;
        spawner.spawnClearance = 5f;
        spawner.destroyDistance = -15f;

        BoosterEffects effects = Object.FindObjectOfType<BoosterEffects>();
        if (effects == null)
        {
            effects = manager.gameObject.AddComponent<BoosterEffects>();
            Debug.Log("[BoosterSetup] added BoosterEffects to " + manager.gameObject.name);
        }
        effects.boosterManager = manager;

        EditorUtility.SetDirty(manager);
        EditorUtility.SetDirty(spawner);
        EditorUtility.SetDirty(effects);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();

        Debug.Log("[BoosterSetup] wired manager durations=" + manager.boosterDurations.Count + " spawner prefabs=" + prefabs.Count);
    }

    [MenuItem("JumpGuy/Boosters/4. Build UI")]
    public static void BuildUI()
    {
        BoosterManager manager = Object.FindObjectOfType<BoosterManager>();
        if (manager == null)
        {
            Debug.LogError("[BoosterSetup] BoosterManager not found - run step 3 first");
            return;
        }

        Canvas canvas = null;
        foreach (Canvas c in Object.FindObjectsOfType<Canvas>())
        {
            if (c.renderMode == RenderMode.ScreenSpaceOverlay) { canvas = c; break; }
        }
        if (canvas == null)
        {
            Debug.LogError("[BoosterSetup] overlay Canvas not found");
            return;
        }

        Transform existing = canvas.transform.Find("BoosterPanel");
        if (existing != null) Object.DestroyImmediate(existing.gameObject);

        Sprite slotSprite = AssetDatabase.LoadAssetAtPath<Sprite>(PackHud + "/BuffSlot_01_Bg.png");
        Sprite barBgSprite = AssetDatabase.LoadAssetAtPath<Sprite>(PackSlider + "/Slider_01_White_Bg.png");
        Sprite barFillSprite = AssetDatabase.LoadAssetAtPath<Sprite>(PackSlider + "/Slider_01_White_Fill.png");

        if (slotSprite == null) Debug.LogWarning("[BoosterSetup] BuffSlot_01_Bg missing");
        if (barFillSprite == null) Debug.LogWarning("[BoosterSetup] bar fill sprite missing");

        const float iconSize = 84f;
        const float barWidth = 150f;
        const float barHeight = 18f;
        const float gap = 6f;
        const float slotHeight = 96f;
        const float slotStep = 106f;

        int uiCount = 0;
        foreach (BoosterDef def in Defs) if (IsUiBooster(def.type)) uiCount++;

        GameObject panel = new GameObject("BoosterPanel", typeof(RectTransform));
        panel.transform.SetParent(canvas.transform, false);
        RectTransform panelRt = panel.GetComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0f, 0.5f);
        panelRt.anchorMax = new Vector2(0f, 0.5f);
        panelRt.pivot = new Vector2(0f, 0.5f);
        panelRt.anchoredPosition = new Vector2(24f, 0f);
        panelRt.sizeDelta = new Vector2(iconSize + gap + barWidth, slotStep * uiCount);

        BoosterUI ui = panel.AddComponent<BoosterUI>();
        ui.boosterManager = manager;

        List<BoosterSlotUI> slots = new List<BoosterSlotUI>();

        int defIndex = 0;
        foreach (BoosterDef def in Defs)
        {
            if (!IsUiBooster(def.type)) continue;

            Sprite icon = AssetDatabase.LoadAssetAtPath<Sprite>(IconFolder + "/Icon_" + def.assetName + ".png");

            GameObject slot = new GameObject(def.type + "Slot", typeof(RectTransform));
            slot.transform.SetParent(panel.transform, false);
            RectTransform slotRt = slot.GetComponent<RectTransform>();
            slotRt.anchorMin = new Vector2(0f, 1f);
            slotRt.anchorMax = new Vector2(0f, 1f);
            slotRt.pivot = new Vector2(0f, 1f);
            slotRt.sizeDelta = new Vector2(iconSize + gap + barWidth, slotHeight);
            slotRt.anchoredPosition = new Vector2(0f, -defIndex * slotStep);

            GameObject iconGo = new GameObject("Icon", typeof(RectTransform));
            iconGo.transform.SetParent(slot.transform, false);
            RectTransform iconRt = iconGo.GetComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0f, 0.5f);
            iconRt.anchorMax = new Vector2(0f, 0.5f);
            iconRt.pivot = new Vector2(0f, 0.5f);
            iconRt.sizeDelta = new Vector2(iconSize, iconSize);
            iconRt.anchoredPosition = new Vector2(0f, 0f);
            Image iconImg = iconGo.AddComponent<Image>();
            iconImg.sprite = icon;
            iconImg.preserveAspect = true;
            iconImg.color = Color.white;
            iconImg.raycastTarget = false;

            GameObject barGroup = new GameObject("TimeBarGroup", typeof(RectTransform));
            barGroup.transform.SetParent(slot.transform, false);
            RectTransform barGroupRt = barGroup.GetComponent<RectTransform>();
            barGroupRt.anchorMin = new Vector2(0f, 0.5f);
            barGroupRt.anchorMax = new Vector2(0f, 0.5f);
            barGroupRt.pivot = new Vector2(0f, 0.5f);
            barGroupRt.sizeDelta = new Vector2(barWidth, barHeight);
            barGroupRt.anchoredPosition = new Vector2(iconSize + gap, 0f);

            GameObject barBg = new GameObject("TimeBarBg", typeof(RectTransform));
            barBg.transform.SetParent(barGroup.transform, false);
            RectTransform barBgRt = barBg.GetComponent<RectTransform>();
            barBgRt.anchorMin = Vector2.zero;
            barBgRt.anchorMax = Vector2.one;
            barBgRt.offsetMin = Vector2.zero;
            barBgRt.offsetMax = Vector2.zero;
            Image barBgImg = barBg.AddComponent<Image>();
            barBgImg.sprite = barBgSprite;
            barBgImg.type = Image.Type.Sliced;
            barBgImg.color = new Color(1f, 1f, 1f, 0.18f);
            barBgImg.raycastTarget = false;

            GameObject bar = new GameObject("TimeBar", typeof(RectTransform));
            bar.transform.SetParent(barBg.transform, false);
            RectTransform barRt = bar.GetComponent<RectTransform>();
            barRt.anchorMin = Vector2.zero;
            barRt.anchorMax = Vector2.one;
            barRt.offsetMin = new Vector2(2f, 2f);
            barRt.offsetMax = new Vector2(-2f, -2f);
            Image barImg = bar.AddComponent<Image>();
            barImg.sprite = barFillSprite;
            barImg.type = Image.Type.Filled;
            barImg.fillMethod = Image.FillMethod.Horizontal;
            barImg.fillOrigin = 0;
            barImg.fillAmount = 1f;
            barImg.color = def.tint;
            barImg.raycastTarget = false;

            BoosterSlotUI slotUi = slot.AddComponent<BoosterSlotUI>();
            SerializedObject so = new SerializedObject(slotUi);
            so.FindProperty("boosterType").enumValueIndex = (int)def.type;
            so.FindProperty("iconImage").objectReferenceValue = iconImg;
            so.FindProperty("iconSprite").objectReferenceValue = icon;
            so.FindProperty("fillImage").objectReferenceValue = barImg;
            so.ApplyModifiedPropertiesWithoutUndo();

            slot.SetActive(false);
            slots.Add(slotUi);

            defIndex++;
        }

        ui.slots = slots.ToArray();
        EditorUtility.SetDirty(ui);

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();

        Debug.Log("[BoosterSetup] UI panel built with " + slots.Count + " slots");
    }

    [MenuItem("JumpGuy/Boosters/Run All")]
    public static void RunAll()
    {
        GenerateIcons();
        CreatePrefabs();
        WireIntoScene();
        BuildUI();
        Debug.Log("[BoosterSetup] ALL DONE");
    }

    private static void EnsureFolder(string parent, string child)
    {
        if (!AssetDatabase.IsValidFolder(parent))
        {
            int slash = parent.LastIndexOf('/');
            if (slash > 0) EnsureFolder(parent.Substring(0, slash), parent.Substring(slash + 1));
            else AssetDatabase.CreateFolder("Assets", parent);
        }
        if (!AssetDatabase.IsValidFolder(parent + "/" + child)) AssetDatabase.CreateFolder(parent, child);
    }
}
