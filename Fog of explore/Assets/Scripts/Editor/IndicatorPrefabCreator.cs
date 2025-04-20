#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using HUDIndicator;
using UnityEngine.UI;

public class IndicatorPrefabCreator : EditorWindow
{
    private Sprite iconSprite;
    private GameObject arrowPrefab;
    private Color iconColor = Color.white;
    private Color arrowColor = Color.white;
    private float iconSize = 50f;
    private float arrowSize = 30f;
    private string prefabName = "CustomIndicator";
    private IndicatorArrowStyle arrowStyle = new IndicatorArrowStyle();

    [MenuItem("Tools/Indicator Prefab Creator")]
    public static void ShowWindow()
    {
        GetWindow<IndicatorPrefabCreator>("指示器预制体创建器");
    }

    private void OnGUI()
    {
        GUILayout.Label("指示器预制体创建器", EditorStyles.boldLabel);
        
        EditorGUILayout.Space();
        
        prefabName = EditorGUILayout.TextField("预制体名称", prefabName);
        
        EditorGUILayout.Space();
        GUILayout.Label("图标设置", EditorStyles.boldLabel);
        
        iconSprite = (Sprite)EditorGUILayout.ObjectField("图标Sprite", iconSprite, typeof(Sprite), false);
        iconColor = EditorGUILayout.ColorField("图标颜色", iconColor);
        iconSize = EditorGUILayout.FloatField("图标大小", iconSize);
        
        EditorGUILayout.Space();
        GUILayout.Label("箭头设置", EditorStyles.boldLabel);
        
        arrowPrefab = (GameObject)EditorGUILayout.ObjectField("箭头预制体", arrowPrefab, typeof(GameObject), false);
        arrowColor = EditorGUILayout.ColorField("箭头颜色", arrowColor);
        arrowSize = EditorGUILayout.FloatField("箭头大小", arrowSize);
        
        // 显示纹理设置
        EditorGUILayout.LabelField("箭头纹理设置");
        arrowStyle.texture = (Texture)EditorGUILayout.ObjectField("箭头纹理", arrowStyle.texture, typeof(Texture), false);
        arrowStyle.color = EditorGUILayout.ColorField("箭头颜色", arrowStyle.color);
        arrowStyle.margin = EditorGUILayout.FloatField("箭头边距", arrowStyle.margin);
        arrowStyle.width = EditorGUILayout.FloatField("箭头宽度", arrowStyle.width);
        arrowStyle.height = EditorGUILayout.FloatField("箭头高度", arrowStyle.height);
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("创建预制体"))
        {
            CreateIndicatorPrefab();
        }
    }

    private void CreateIndicatorPrefab()
    {
        // 确保保存路径存在
        string savePath = "Assets/Resources/Indicators";
        if (!System.IO.Directory.Exists(savePath))
        {
            System.IO.Directory.CreateDirectory(savePath);
        }
        
        // 创建基础游戏对象
        GameObject indicatorObj = new GameObject(prefabName);
        
        // 添加IndicatorOffScreen组件
        IndicatorOffScreen indicator = indicatorObj.AddComponent<IndicatorOffScreen>();
        indicator.arrowStyle = arrowStyle;
        indicator.showArrow = true;
        
        // 创建图标
        GameObject iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(indicatorObj.transform);
        iconObj.transform.localPosition = Vector3.zero;
        
        // 添加Canvas组件
        Canvas canvas = iconObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        
        // 添加图片组件
        GameObject imageObj = new GameObject("Image");
        imageObj.transform.SetParent(iconObj.transform);
        Image image = imageObj.AddComponent<Image>();
        image.sprite = iconSprite;
        image.color = iconColor;
        
        // 设置图标大小
        RectTransform rectTransform = imageObj.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(iconSize, iconSize);
        
        // 创建箭头
        if (arrowPrefab != null)
        {
            GameObject arrowObj = Instantiate(arrowPrefab, indicatorObj.transform);
            arrowObj.name = "Arrow";
            
            // 设置箭头位置和大小
            arrowObj.transform.localPosition = new Vector3(0, -iconSize/2 - arrowSize/2, 0);
            arrowObj.transform.localScale = new Vector3(arrowSize/100f, arrowSize/100f, arrowSize/100f);
            
            // 设置箭头颜色
            Image arrowImage = arrowObj.GetComponent<Image>();
            if (arrowImage != null)
            {
                arrowImage.color = arrowColor;
            }
        }
        
        // 保存预制体
        string prefabPath = $"{savePath}/{prefabName}.prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(indicatorObj, prefabPath);
        DestroyImmediate(indicatorObj);
        
        if (prefab != null)
        {
            EditorUtility.DisplayDialog("成功", $"指示器预制体已创建: {prefabPath}", "确定");
            Selection.activeObject = prefab;
        }
        else
        {
            EditorUtility.DisplayDialog("失败", "预制体创建失败", "确定");
        }
    }
}
#endif 