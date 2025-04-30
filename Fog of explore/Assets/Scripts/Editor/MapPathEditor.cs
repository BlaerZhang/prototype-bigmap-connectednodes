using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(MapPath))]
public class MapPathEditor : Editor
{
    // 样式配置
    private PathTypeStyleConfig styleConfig;
    
    // 序列化属性
    private SerializedProperty nodeAProp;
    private SerializedProperty nodeBProp;
    private SerializedProperty pathTypeProp;
    private SerializedProperty controlPointsProp;
    private SerializedProperty roadTypeProp;
    
    private void OnEnable()
    {
        // 获取序列化属性
        nodeAProp = serializedObject.FindProperty("nodeA");
        nodeBProp = serializedObject.FindProperty("nodeB");
        pathTypeProp = serializedObject.FindProperty("pathType");
        controlPointsProp = serializedObject.FindProperty("controlPoints");
        roadTypeProp = serializedObject.FindProperty("roadType");
        
        // 加载样式配置
        LoadStyleConfig();
    }
    
    // 加载样式配置
    private void LoadStyleConfig()
    {
        styleConfig = Resources.Load<PathTypeStyleConfig>("PathTypeStyles");
        
        if (styleConfig == null)
        {
            Debug.LogWarning("找不到路径样式配置资源，将使用默认样式。请在Resources文件夹中创建PathTypeStyles资产。");
            
            // 创建一个临时配置，不会保存到资源
            styleConfig = ScriptableObject.CreateInstance<PathTypeStyleConfig>();
            styleConfig.InitializeDefaultStyles();
        }
    }
    
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        EditorGUI.BeginChangeCheck();
        
        // 节点设置
        EditorGUILayout.PropertyField(nodeAProp, new GUIContent("节点A"));
        EditorGUILayout.PropertyField(nodeBProp, new GUIContent("节点B"));
        
        EditorGUILayout.Space();
        
        // 路径形状设置
        EditorGUILayout.PropertyField(pathTypeProp, new GUIContent("路径形状"));
        EditorGUILayout.PropertyField(controlPointsProp, new GUIContent("控制点"));
        
        EditorGUILayout.Space();
        
        // 路径类型设置
        EditorGUILayout.LabelField("路径类型设置", EditorStyles.boldLabel);
        
        // 获取当前路径类型
        MapPath.RoadType currentRoadType = (MapPath.RoadType)roadTypeProp.enumValueIndex;
        
        // 创建选择框，使用友好名称显示
        GUIContent[] options = new GUIContent[System.Enum.GetValues(typeof(MapPath.RoadType)).Length];
        int[] values = new int[System.Enum.GetValues(typeof(MapPath.RoadType)).Length];
        
        int i = 0;
        foreach (MapPath.RoadType roadType in System.Enum.GetValues(typeof(MapPath.RoadType)))
        {
            string displayName = roadType.ToString();
            
            // 获取样式
            PathTypeStyleConfig.PathTypeStyle typeStyle = styleConfig.GetStyleForRoadType(roadType);
            
            // 如果有自定义显示名称，使用它
            if (!string.IsNullOrEmpty(typeStyle.displayName))
            {
                displayName = typeStyle.displayName;
            }
            
            options[i] = new GUIContent($"{roadType} ({displayName})");
            values[i] = (int)roadType;
            i++;
        }
        
        // 显示友好名称的下拉选择框
        roadTypeProp.enumValueIndex = EditorGUILayout.IntPopup(
            new GUIContent("道路类型"), 
            roadTypeProp.enumValueIndex, 
            options, 
            values
        );
        
        // 如果类型更改了，更新样式预览
        MapPath.RoadType newRoadType = (MapPath.RoadType)roadTypeProp.enumValueIndex;
        if (currentRoadType != newRoadType)
        {
            // 标记为修改，以便在Scene视图更新
            EditorUtility.SetDirty(target);
        }
        
        // 显示当前类型的样式预览
        PathTypeStyleConfig.PathTypeStyle currentStyle = styleConfig.GetStyleForRoadType(newRoadType);
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("当前样式预览", EditorStyles.boldLabel);
        
        EditorGUI.BeginDisabledGroup(true); // 禁用编辑，只是预览
        
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        EditorGUILayout.TextField("显示名称", currentStyle.displayName);
        EditorGUILayout.ColorField("路径颜色", currentStyle.color);
        EditorGUILayout.FloatField("路径宽度", currentStyle.width);
        EditorGUILayout.ObjectField("路径材质", currentStyle.material, typeof(Material), false);
        
        EditorGUILayout.EndVertical();
        
        EditorGUI.EndDisabledGroup();
        
        // 添加应用样式按钮
        EditorGUILayout.Space();
        
        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("应用样式", GUILayout.Height(30)))
        {
            ApplyStyle((MapPath)target, newRoadType);
        }
        GUI.backgroundColor = Color.white;
        
        // 增加编辑样式配置的按钮
        EditorGUILayout.Space();
        if (GUILayout.Button("编辑样式配置"))
        {
            SelectStyleConfig();
        }
        
        bool changed = EditorGUI.EndChangeCheck();
        
        serializedObject.ApplyModifiedProperties();
        
        // 如果属性被修改，更新路径视觉效果
        if (changed)
        {
            MapPath mapPath = (MapPath)target;
            mapPath.UpdatePathVisual();
        }
    }
    
    // 选择样式配置资产
    private void SelectStyleConfig()
    {
        if (styleConfig != null)
        {
            // 选中当前配置资产
            Selection.activeObject = styleConfig;
            EditorGUIUtility.PingObject(styleConfig);
        }
        else
        {
            // 显示创建配置的提示
            bool create = EditorUtility.DisplayDialog(
                "找不到样式配置", 
                "未找到路径样式配置资产。是否创建一个新的配置？", 
                "创建", 
                "取消");
                
            if (create)
            {
                PathTypeStyleConfigUtility.CreatePathTypeStyleConfig();
                // 重新加载配置
                LoadStyleConfig();
            }
        }
    }
    
    // 应用样式到路径
    private void ApplyStyle(MapPath mapPath, MapPath.RoadType roadType)
    {
        // 获取样式
        PathTypeStyleConfig.PathTypeStyle style = styleConfig.GetStyleForRoadType(roadType);
        
        // 使用MapPath的公共方法应用样式
        mapPath.ApplyRoadTypeStyle(style.color, style.width, style.material);
        
        Debug.Log($"已应用样式 {roadType} ({style.displayName}) 到路径 {mapPath.name}");
    }
    
    // 在Scene视图中绘制额外的控件
    private void OnSceneGUI()
    {
        MapPath mapPath = (MapPath)target;
        
        if (mapPath.nodeA != null && mapPath.nodeB != null)
        {
            // 获取当前路径类型
            MapPath.RoadType roadType = mapPath.roadType;
            
            // 在路径中点显示控制按钮
            Vector3 midPoint = (mapPath.nodeA.transform.position + mapPath.nodeB.transform.position) * 0.5f;
            
            // 计算适当的大小以便在场景中可见
            float handleSize = HandleUtility.GetHandleSize(midPoint) * 0.2f;
            
            // 获取样式颜色
            Color handleColor = Color.white;
            PathTypeStyleConfig.PathTypeStyle pathStyle = styleConfig.GetStyleForRoadType(roadType);
            handleColor = pathStyle.color;
            
            // 绘制按钮并处理交互
            Handles.color = handleColor;
            if (Handles.Button(midPoint, Quaternion.identity, handleSize, handleSize, Handles.SphereHandleCap))
            {
                // 创建上下文菜单
                GenericMenu menu = new GenericMenu();
                
                // 为每种路径类型添加菜单项
                foreach (MapPath.RoadType type in System.Enum.GetValues(typeof(MapPath.RoadType)))
                {
                    PathTypeStyleConfig.PathTypeStyle typeStyle = styleConfig.GetStyleForRoadType(type);
                    string displayName = !string.IsNullOrEmpty(typeStyle.displayName) ? typeStyle.displayName : type.ToString();
                    
                    // 标记当前选中的类型
                    bool isSelected = (type == roadType);
                    
                    // 添加菜单项，使用闭包捕获类型值
                    MapPath.RoadType capturedType = type;
                    menu.AddItem(new GUIContent($"{type} ({displayName})"), isSelected, () => {
                        ChangeRoadType(mapPath, capturedType);
                    });
                }
                
                // 显示菜单
                menu.ShowAsContext();
            }
            
            // 添加类型标签
            PathTypeStyleConfig.PathTypeStyle currentStyle = styleConfig.GetStyleForRoadType(roadType);
            string label = $"{roadType} ({currentStyle.displayName})";
            
            Handles.Label(midPoint + Vector3.up * handleSize, label);
        }
    }
    
    // 更改路径类型并应用样式
    private void ChangeRoadType(MapPath mapPath, MapPath.RoadType newType)
    {
        // 记录撤销操作
        Undo.RecordObject(mapPath, "更改路径类型");
        
        // 更改类型
        mapPath.roadType = newType;
        
        // 标记为已修改
        EditorUtility.SetDirty(mapPath);
        
        // 应用样式
        ApplyStyle(mapPath, newType);
    }
} 