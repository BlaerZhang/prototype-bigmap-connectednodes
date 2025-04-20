#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using HUDIndicator;

// 确保配置能够在编辑器关闭后保存
[InitializeOnLoad]
public class SpriteRandomizer : EditorWindow
{
    // 定义类路径，用于程序集引用
    private readonly string RandomizedSpriteIndicatorScript = "Assets/Scripts/RandomizedSpriteIndicator.cs";
    
    private GameObject parentObject;
    private List<SpriteProbability> spriteProbabilities = new List<SpriteProbability>();
    private Vector2 scrollPosition;
    private SpriteRandomizerConfig config;
    
    [Header("指示器设置")]
    private bool addIndicator = false;
    private IndicatorOffScreen indicatorPrefab;
    private bool autoRegisterToManager = true;
    private float minDistance = 0f;
    private float maxDistance = 100f;
    
    [Header("指示器大小设置")]
    private float indicatorWidth = 32f;
    private float indicatorHeight = 32f;
    private bool maintainAspectRatio = true;
    private float sizeScale = 1f;

    // 配置路径
    private static readonly string configPath = "Assets/Resources/SpriteRandomizerConfig.asset";
    private static readonly string editorPrefsKey = "SpriteRandomizerConfigPath";

    // 静态构造函数，确保编辑器初始化时运行
    static SpriteRandomizer()
    {
        // 在编辑器退出时保存配置
        EditorApplication.quitting += OnEditorQuitting;
    }

    // 编辑器退出时保存配置
    private static void OnEditorQuitting()
    {
        string savedConfigPath = EditorPrefs.GetString(editorPrefsKey, configPath);
        var config = AssetDatabase.LoadAssetAtPath<SpriteRandomizerConfig>(savedConfigPath);
        
        if (config != null)
        {
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            Debug.Log("SpriteRandomizer配置已保存: " + savedConfigPath);
        }
    }

    [System.Serializable]
    public class SpriteProbability
    {
        public Sprite sprite;
        [Range(0, 100)]
        public float probability = 50f;
        public bool showIndicator = false; // 是否为此Sprite添加指示器
    }

    [MenuItem("Tools/Sprite Randomizer")]
    public static void ShowWindow()
    {
        GetWindow<SpriteRandomizer>("Sprite Randomizer");
    }

    private void OnEnable()
    {
        LoadConfig();
    }

    private void OnDisable()
    {
        SaveConfig();
    }

    private void LoadConfig()
    {
        // 从EditorPrefs中获取配置路径
        string savedConfigPath = EditorPrefs.GetString(editorPrefsKey, configPath);
        
        // 尝试加载现有配置
        config = AssetDatabase.LoadAssetAtPath<SpriteRandomizerConfig>(savedConfigPath);

        // 如果配置不存在，创建新的
        if (config == null)
        {
            config = ScriptableObject.CreateInstance<SpriteRandomizerConfig>();
            
            // 确保Resources文件夹存在
            string resourcesPath = "Assets/Resources";
            if (!Directory.Exists(resourcesPath))
            {
                Directory.CreateDirectory(resourcesPath);
            }
            
            AssetDatabase.CreateAsset(config, configPath);
            AssetDatabase.SaveAssets();
            
            // 保存配置路径到EditorPrefs
            EditorPrefs.SetString(editorPrefsKey, configPath);
        }

        // 加载保存的数据
        parentObject = config.parentObject;
        spriteProbabilities = new List<SpriteProbability>(config.spriteProbabilities);
        addIndicator = config.addIndicator;
        indicatorPrefab = config.indicatorPrefab;
        autoRegisterToManager = config.autoRegisterToManager;
        minDistance = config.minDistance;
        maxDistance = config.maxDistance;
        indicatorWidth = config.indicatorWidth;
        indicatorHeight = config.indicatorHeight;
        maintainAspectRatio = config.maintainAspectRatio;
        sizeScale = config.sizeScale;
    }

    private void SaveConfig()
    {
        if (config == null) return;

        // 保存当前数据
        config.parentObject = parentObject;
        config.spriteProbabilities = new List<SpriteProbability>(spriteProbabilities);
        config.addIndicator = addIndicator;
        config.indicatorPrefab = indicatorPrefab;
        config.autoRegisterToManager = autoRegisterToManager;
        config.minDistance = minDistance;
        config.maxDistance = maxDistance;
        config.indicatorWidth = indicatorWidth;
        config.indicatorHeight = indicatorHeight;
        config.maintainAspectRatio = maintainAspectRatio;
        config.sizeScale = sizeScale;

        // 标记为已修改
        EditorUtility.SetDirty(config);
        AssetDatabase.SaveAssets();
        
        // 保存配置路径到EditorPrefs
        EditorPrefs.SetString(editorPrefsKey, AssetDatabase.GetAssetPath(config));
    }

    private void OnGUI()
    {
        GUILayout.Label("Sprite Randomizer", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("配置路径:", GUILayout.Width(80));
        string savedConfigPath = EditorPrefs.GetString(editorPrefsKey, configPath);
        GUILayout.Label(savedConfigPath, EditorStyles.miniLabel);
        
        if (GUILayout.Button("另存为...", GUILayout.Width(80)))
        {
            string newPath = EditorUtility.SaveFilePanelInProject("保存配置", "SpriteRandomizerConfig", "asset", "请选择保存SpriteRandomizer配置的位置");
            if (!string.IsNullOrEmpty(newPath))
            {
                // 保存到新位置
                AssetDatabase.CreateAsset(Instantiate(config), newPath);
                AssetDatabase.SaveAssets();
                
                // 更新当前配置
                config = AssetDatabase.LoadAssetAtPath<SpriteRandomizerConfig>(newPath);
                EditorPrefs.SetString(editorPrefsKey, newPath);
                SaveConfig();
            }
        }
        
        if (GUILayout.Button("加载...", GUILayout.Width(80)))
        {
            string newPath = EditorUtility.OpenFilePanelWithFilters("加载配置", "Assets", new[] { "配置文件", "asset" });
            if (!string.IsNullOrEmpty(newPath))
            {
                newPath = "Assets" + newPath.Substring(Application.dataPath.Length);
                var newConfig = AssetDatabase.LoadAssetAtPath<SpriteRandomizerConfig>(newPath);
                if (newConfig != null)
                {
                    // 更新当前配置
                    config = newConfig;
                    EditorPrefs.SetString(editorPrefsKey, newPath);
                    LoadConfig();
                }
            }
        }
        EditorGUILayout.EndHorizontal();

        // 父物体选择
        GameObject newParentObject = (GameObject)EditorGUILayout.ObjectField("Parent Object", parentObject, typeof(GameObject), true);
        if (newParentObject != parentObject)
        {
            parentObject = newParentObject;
            SaveConfig();
        }

        // Sprite概率配置
        EditorGUILayout.LabelField("Sprite Probabilities", EditorStyles.boldLabel);
        
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        for (int i = 0; i < spriteProbabilities.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            
            Sprite newSprite = (Sprite)EditorGUILayout.ObjectField(spriteProbabilities[i].sprite, typeof(Sprite), false);
            float newProbability = EditorGUILayout.Slider(spriteProbabilities[i].probability, 0f, 100f);
            
            // 添加指示器选项
            bool newShowIndicator = EditorGUILayout.Toggle("指示器", spriteProbabilities[i].showIndicator);
            
            if (newSprite != spriteProbabilities[i].sprite || 
                newProbability != spriteProbabilities[i].probability ||
                newShowIndicator != spriteProbabilities[i].showIndicator)
            {
                spriteProbabilities[i].sprite = newSprite;
                spriteProbabilities[i].probability = newProbability;
                spriteProbabilities[i].showIndicator = newShowIndicator;
                SaveConfig();
            }
            
            if (GUILayout.Button("Remove"))
            {
                spriteProbabilities.RemoveAt(i);
                SaveConfig();
                i--;
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        EditorGUILayout.EndScrollView();

        if (GUILayout.Button("Add New Sprite"))
        {
            spriteProbabilities.Add(new SpriteProbability());
            SaveConfig();
        }
        
        // 指示器全局设置
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Indicator Settings", EditorStyles.boldLabel);
        
        bool newAddIndicator = EditorGUILayout.Toggle("启用指示器功能", addIndicator);
        if (newAddIndicator != addIndicator)
        {
            addIndicator = newAddIndicator;
            SaveConfig();
        }
        
        if (addIndicator)
        {
            // 指示器预制体
            IndicatorOffScreen newIndicatorPrefab = (IndicatorOffScreen)EditorGUILayout.ObjectField("Indicator Prefab", indicatorPrefab, typeof(IndicatorOffScreen), false);
            if (newIndicatorPrefab != indicatorPrefab)
            {
                indicatorPrefab = newIndicatorPrefab;
                SaveConfig();
            }
            
            // 自动注册到管理器
            bool newAutoRegisterToManager = EditorGUILayout.Toggle("Auto Register to Manager", autoRegisterToManager);
            if (newAutoRegisterToManager != autoRegisterToManager)
            {
                autoRegisterToManager = newAutoRegisterToManager;
                SaveConfig();
            }
            
            // 最小距离
            float newMinDistance = EditorGUILayout.FloatField("Min Distance", minDistance);
            if (newMinDistance != minDistance)
            {
                minDistance = newMinDistance;
                SaveConfig();
            }
            
            // 最大距离
            float newMaxDistance = EditorGUILayout.FloatField("Max Distance", maxDistance);
            if (newMaxDistance != maxDistance)
            {
                maxDistance = newMaxDistance;
                SaveConfig();
            }
            
            // 指示器大小设置
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("指示器大小设置", EditorStyles.boldLabel);
            
            // 宽度
            float newIndicatorWidth = EditorGUILayout.FloatField("宽度", indicatorWidth);
            if (newIndicatorWidth != indicatorWidth)
            {
                indicatorWidth = newIndicatorWidth;
                SaveConfig();
            }
            
            // 高度
            float newIndicatorHeight = EditorGUILayout.FloatField("高度", indicatorHeight);
            if (newIndicatorHeight != indicatorHeight)
            {
                indicatorHeight = newIndicatorHeight;
                SaveConfig();
            }
            
            // 保持比例
            bool newMaintainAspectRatio = EditorGUILayout.Toggle("保持图像比例", maintainAspectRatio);
            if (newMaintainAspectRatio != maintainAspectRatio)
            {
                maintainAspectRatio = newMaintainAspectRatio;
                SaveConfig();
            }
            
            // 缩放比例
            float newSizeScale = EditorGUILayout.Slider("整体缩放比例", sizeScale, 0.1f, 3f);
            if (newSizeScale != sizeScale)
            {
                sizeScale = newSizeScale;
                SaveConfig();
            }
        }

        EditorGUILayout.Space();

        // 生成按钮
        if (GUILayout.Button("Randomize Sprites"))
        {
            RandomizeSprites();
        }
        
        if (GUILayout.Button("立即保存配置"))
        {
            SaveConfig();
            EditorUtility.DisplayDialog("保存成功", "SpriteRandomizer配置已保存", "确定");
        }
    }

    private void RandomizeSprites()
    {
        if (parentObject == null)
        {
            EditorUtility.DisplayDialog("Error", "Please select a parent object", "OK");
            return;
        }

        if (spriteProbabilities.Count == 0)
        {
            EditorUtility.DisplayDialog("Error", "Please add at least one sprite", "OK");
            return;
        }

        // 计算总概率
        float totalProbability = 0f;
        foreach (var sp in spriteProbabilities)
        {
            totalProbability += sp.probability;
        }

        if (totalProbability <= 0)
        {
            EditorUtility.DisplayDialog("Error", "Total probability must be greater than 0", "OK");
            return;
        }

        // 确保RandomizedSpriteIndicator脚本存在
        if (addIndicator && !File.Exists(RandomizedSpriteIndicatorScript))
        {
            EditorUtility.DisplayDialog("Error", "RandomizedSpriteIndicator script not found. Please create it first.", "OK");
            return;
        }

        // 获取所有子物体
        SpriteRenderer[] spriteRenderers = parentObject.GetComponentsInChildren<SpriteRenderer>();

        // 过滤掉无效的SpriteRenderer
        List<SpriteRenderer> validRenderers = new List<SpriteRenderer>();
        foreach (var renderer in spriteRenderers)
        {
            if (renderer != null && renderer.gameObject != null)
            {
                validRenderers.Add(renderer);
            }
        }
        
        // 如果没有有效的渲染器，提前退出
        if (validRenderers.Count == 0)
        {
            EditorUtility.DisplayDialog("Warning", "No valid SpriteRenderers found in the selected parent object.", "OK");
            return;
        }

        // 创建撤销记录点
        Undo.RegisterCompleteObjectUndo(validRenderers.ToArray(), "Randomize Sprites");

        foreach (var renderer in validRenderers)
        {
            // 再次检查渲染器是否有效
            if (renderer == null || renderer.gameObject == null)
            {
                continue;
            }
            
            try
            {
                GameObject rendererGameObject = renderer.gameObject;
                
                // 先清理当前对象上的所有IndicatorOffScreen组件和其子对象
                CleanupIndicators(rendererGameObject);
                
                // 清理RandomizedSpriteIndicator组件
                var existingIndicator = rendererGameObject.GetComponent("RandomizedSpriteIndicator") as MonoBehaviour;
                if (existingIndicator != null)
                {
                    Undo.DestroyObjectImmediate(existingIndicator);
                }
                
            float randomValue = Random.Range(0f, totalProbability);
            float cumulativeProbability = 0f;
                
                bool shouldAddIndicator = false;
                Sprite selectedSprite = null;

            foreach (var sp in spriteProbabilities)
            {
                cumulativeProbability += sp.probability;
                if (randomValue <= cumulativeProbability)
                {
                        selectedSprite = sp.sprite;
                        shouldAddIndicator = sp.showIndicator && addIndicator;
                        
                        // 记录更改并设置sprite
                        if (renderer != null)
                        {
                            Undo.RecordObject(renderer, "Change Sprite");
                            renderer.sprite = selectedSprite;
                        }
                    break;
                    }
                }
                
                // 处理指示器
                if (shouldAddIndicator && selectedSprite != null && rendererGameObject != null)
                {
                    // 添加新组件
                    var indicator = Undo.AddComponent(rendererGameObject, System.Type.GetType("RandomizedSpriteIndicator, Assembly-CSharp")) as MonoBehaviour;
                    
                    if (indicator == null)
                    {
                        Debug.LogError("Could not add RandomizedSpriteIndicator component. Make sure the script exists and is properly compiled.");
                        continue;
                    }
                    
                    // 设置指示器属性 - 使用反射设置属性
                    System.Type type = indicator.GetType();
                    
                    // 设置showIndicator
                    var showIndicatorField = type.GetField("showIndicator");
                    if (showIndicatorField != null) showIndicatorField.SetValue(indicator, true);
                    
                    // 设置indicatorPrefab
                    var indicatorPrefabField = type.GetField("indicatorPrefab");
                    if (indicatorPrefabField != null) indicatorPrefabField.SetValue(indicator, indicatorPrefab);
                    
                    // 设置autoRegisterToManager
                    var autoRegisterField = type.GetField("autoRegisterToManager");
                    if (autoRegisterField != null) autoRegisterField.SetValue(indicator, autoRegisterToManager);
                    
                    // 设置minDistance
                    var minDistanceField = type.GetField("minDistance");
                    if (minDistanceField != null) minDistanceField.SetValue(indicator, minDistance);
                    
                    // 设置maxDistance
                    var maxDistanceField = type.GetField("maxDistance");
                    if (maxDistanceField != null) maxDistanceField.SetValue(indicator, maxDistance);
                    
                    // 设置大小相关属性
                    var indicatorWidthField = type.GetField("indicatorWidth");
                    if (indicatorWidthField != null) indicatorWidthField.SetValue(indicator, indicatorWidth);
                    
                    var indicatorHeightField = type.GetField("indicatorHeight");
                    if (indicatorHeightField != null) indicatorHeightField.SetValue(indicator, indicatorHeight);
                    
                    var maintainAspectRatioField = type.GetField("maintainAspectRatio");
                    if (maintainAspectRatioField != null) maintainAspectRatioField.SetValue(indicator, maintainAspectRatio);
                    
                    var sizeScaleField = type.GetField("sizeScale");
                    if (sizeScaleField != null) sizeScaleField.SetValue(indicator, sizeScale);
                    
                    // 设置useCustomSprite和customSprite
                    var useCustomSpriteField = type.GetField("useCustomSprite");
                    if (useCustomSpriteField != null) useCustomSpriteField.SetValue(indicator, true);
                    
                    var customSpriteField = type.GetField("customSprite");
                    if (customSpriteField != null) customSpriteField.SetValue(indicator, selectedSprite);
                    
                    // 尝试从Sprite直接创建Texture (如果可能)
                    if (selectedSprite != null)
                    {
                        // 生成对应的Texture并设置
                        Texture2D spriteTexture = null;
                        if (selectedSprite.texture != null)
                        {
                            try
                            {
                                // 检查是否已经是完整的Texture
                                if (selectedSprite.rect.width == selectedSprite.texture.width &&
                                    selectedSprite.rect.height == selectedSprite.texture.height)
                                {
                                    spriteTexture = selectedSprite.texture;
                                }
                                else
                                {
                                    // 否则创建新的Texture
                                    spriteTexture = new Texture2D(
                                        (int)selectedSprite.rect.width,
                                        (int)selectedSprite.rect.height,
                                        TextureFormat.RGBA32,
                                        false);
                                    
                                    // 尝试提取Sprite区域的像素
                                    Color[] pixels = selectedSprite.texture.GetPixels(
                                        (int)selectedSprite.rect.x,
                                        (int)selectedSprite.rect.y,
                                        (int)selectedSprite.rect.width,
                                        (int)selectedSprite.rect.height);
                                    
                                    spriteTexture.SetPixels(pixels);
                                    spriteTexture.Apply();
                                    
                                    // 确保在编辑器关闭时清理
                                    Undo.RegisterCreatedObjectUndo(spriteTexture, "Create Sprite Texture");
                                }
                                
                                // 设置customTexture字段
                                var customTextureField = type.GetField("customTexture");
                                if (customTextureField != null) customTextureField.SetValue(indicator, spriteTexture);
                            }
                            catch (System.Exception e)
                            {
                                Debug.LogError($"Failed to create texture from sprite: {e.Message}");
                            }
                        }
                    }
                    
                    // 调用SetupIndicator方法前，直接设置好prefab的样式
                    if (indicatorPrefab != null && indicatorPrefab.style == null)
                    {
                        indicatorPrefab.style = new HUDIndicator.IndicatorIconStyle();
                    }
                    
                    if (indicatorPrefab != null && indicatorPrefab.arrowStyle == null)
                    {
                        indicatorPrefab.arrowStyle = new HUDIndicator.IndicatorArrowStyle();
                    }
                    
                    // 调用SetupIndicator方法
                    var setupMethod = type.GetMethod("SetupIndicator");
                    if (setupMethod != null) setupMethod.Invoke(indicator, null);
                    
                    // 标记为已修改
                    EditorUtility.SetDirty(rendererGameObject);
                }
            }
            catch (System.Exception e)
            {
                // 捕获并记录任何异常，避免整个进程崩溃
                Debug.LogError($"Error while processing sprite renderer: {e.Message}\n{e.StackTrace}");
            }
        }

        EditorUtility.DisplayDialog("Success", "Sprites have been randomized!", "OK");
        
        // 确保配置被保存
        SaveConfig();
    }
    
    // 清理游戏对象上和其子对象中的所有IndicatorOffScreen组件
    private void CleanupIndicators(GameObject gameObject)
    {
        if (gameObject == null) return;
        
        try
        {
            // 找到子对象中所有的IndicatorOffScreen组件
            IndicatorOffScreen[] indicators = gameObject.GetComponentsInChildren<IndicatorOffScreen>(true);
            
            if (indicators == null) return;
            
            foreach (var indicator in indicators)
            {
                if (indicator == null || indicator.gameObject == null) continue;
                
                try
                {
                    // 从管理器中移除（如果已注册）
                    var indicatorManager = GameObject.FindObjectOfType<IndicatorManager>();
                    if (indicatorManager != null && indicatorManager.indicators != null && indicator != null)
                    {
                        if (indicatorManager.indicators.Contains(indicator))
                        {
                            indicatorManager.indicators.Remove(indicator);
                        }
                    }
                    
                    // 使用Undo系统删除对象
                    if (indicator != null && indicator.gameObject != null)
                    {
                        Undo.DestroyObjectImmediate(indicator.gameObject);
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"Error cleaning up indicator: {e.Message}");
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error in CleanupIndicators: {e.Message}");
        }
    }
}

// 配置保存类
public class SpriteRandomizerConfig : ScriptableObject
{
    public GameObject parentObject;
    public List<SpriteRandomizer.SpriteProbability> spriteProbabilities = new List<SpriteRandomizer.SpriteProbability>();
    public bool addIndicator = false;
    public IndicatorOffScreen indicatorPrefab;
    public bool autoRegisterToManager = true;
    public float minDistance = 0f;
    public float maxDistance = 100f;
    public float indicatorWidth = 32f;
    public float indicatorHeight = 32f;
    public bool maintainAspectRatio = true;
    public float sizeScale = 1f;
}
#endif 