using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine.Splines;
using UnityEditor.Splines;
using System.Linq;
using System.IO;

public class SplineMapCreatorTool : EditorWindow
{
    // 引用
    private GameObject nodeParent;
    private GameObject pathParent;
    private SplineContainer splineContainer;
    
    // 节点设置
    private Sprite nodeSprite;
    private float nodeSize = 1.0f;
    
    // 路径设置
    private Material pathMaterial;
    private Color pathColor = Color.black;
    private float pathWidth = 0.1f;
    
    // 路径类型样式配置
    [System.Serializable]
    public class PathTypeStyle
    {
        public Color color = Color.black;
        public float width = 0.1f;
        public Material material;
        public string displayName;
        
        public PathTypeStyle(Color color, float width, string displayName)
        {
            this.color = color;
            this.width = width;
            this.displayName = displayName;
        }
    }
    
    // 每种路径类型的样式映射
    private Dictionary<MapPath.RoadType, PathTypeStyle> roadTypeStyles = new Dictionary<MapPath.RoadType, PathTypeStyle>();
    
    // 编辑器设置
    private Vector2 scrollPosition;
    private Vector2 segmentScrollPosition;
    private MapPath.RoadType selectedRoadType = MapPath.RoadType.Main;
    private Dictionary<int, MapPath.RoadType> splineRoadTypes = new Dictionary<int, MapPath.RoadType>();
    private Dictionary<string, MapPath.RoadType> segmentRoadTypes = new Dictionary<string, MapPath.RoadType>();
    
    // 配置保存路径
    private const string CONFIG_DIRECTORY = "Assets/Editor/SplineMapCreator";
    private const string TYPE_STYLES_FILE = "PathTypeStyles.json";
    private const string SEGMENT_TYPES_FILE = "SegmentTypes.json";
    private bool showSegmentSettings = false;
    
    // 创建菜单项
    [MenuItem("Tools/Spline Map Creator")]
    public static void ShowWindow()
    {
        GetWindow<SplineMapCreatorTool>("样条地图创建工具");
    }
    
    private void OnEnable()
    {
        // 初始化路径类型样式
        InitializeRoadTypeStyles();
        
        // 加载保存的配置
        LoadTypeStyles();
        LoadSegmentTypes();
    }
    
    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        GUILayout.Label("Unity Spline 地图创建工具", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        // 显示使用说明
        EditorGUILayout.HelpBox(
            "使用步骤:\n" +
            "1. 使用Unity的Spline工具创建样条\n" +
            "2. 选择Spline容器并分配到下方\n" +
            "3. 设置路径类型样式\n" +
            "4. 配置segment类型\n" +
            "5. 点击生成按钮创建节点和路径", 
            MessageType.Info);
        
        EditorGUILayout.Space();
        
        // 样条容器选择
        EditorGUILayout.LabelField("样条设置", EditorStyles.boldLabel);
        splineContainer = (SplineContainer)EditorGUILayout.ObjectField("样条容器", splineContainer, typeof(SplineContainer), true);
        
        // 父物体设置
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("父物体设置", EditorStyles.boldLabel);
        nodeParent = (GameObject)EditorGUILayout.ObjectField("节点父物体", nodeParent, typeof(GameObject), true);
        pathParent = (GameObject)EditorGUILayout.ObjectField("路径父物体", pathParent, typeof(GameObject), true);
        
        if (GUILayout.Button("创建默认父物体"))
        {
            CreateDefaultParents();
        }
        
        // 节点设置
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("节点设置", EditorStyles.boldLabel);
        nodeSprite = (Sprite)EditorGUILayout.ObjectField("节点精灵", nodeSprite, typeof(Sprite), false);
        nodeSize = EditorGUILayout.FloatField("节点大小", nodeSize);
        
        // 路径类型样式设置
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("路径类型样式设置", EditorStyles.boldLabel);
        
        // 为每种路径类型显示样式设置
        foreach (MapPath.RoadType roadType in System.Enum.GetValues(typeof(MapPath.RoadType)))
        {
            // 确保字典中包含此类型
            if (!roadTypeStyles.ContainsKey(roadType))
            {
                roadTypeStyles[roadType] = new PathTypeStyle(Color.black, 0.1f, roadType.ToString());
            }
            
            PathTypeStyle style = roadTypeStyles[roadType];
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // 显示类型名称
            EditorGUILayout.LabelField($"{roadType} ({style.displayName}):", EditorStyles.boldLabel);
            
            // 显示名称
            style.displayName = EditorGUILayout.TextField("显示名称", style.displayName);
            
            // 颜色选择
            style.color = EditorGUILayout.ColorField("路径颜色", style.color);
            
            // 宽度设置
            style.width = EditorGUILayout.FloatField("路径宽度", style.width);
            
            // 材质设置
            style.material = (Material)EditorGUILayout.ObjectField("路径材质", style.material, typeof(Material), false);
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }
        
        // 当选择了splineContainer，显示每条spline的设置
        if (splineContainer != null && splineContainer.Splines.Count > 0)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("样条路径类型设置", EditorStyles.boldLabel);
            
            // 全局设置当前选中的路径类型
            selectedRoadType = (MapPath.RoadType)EditorGUILayout.EnumPopup("路径类型", selectedRoadType);
            
            if (GUILayout.Button("将选中类型应用到所有样条"))
            {
                for (int i = 0; i < splineContainer.Splines.Count; i++)
                {
                    splineRoadTypes[i] = selectedRoadType;
                }
            }
            
            // 显示每条spline的设置
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("单独样条设置:");
            
            for (int i = 0; i < splineContainer.Splines.Count; i++)
            {
                // 确保字典中有此spline的路径类型
                if (!splineRoadTypes.ContainsKey(i))
                {
                    splineRoadTypes[i] = MapPath.RoadType.Main;
                }
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"样条 {i}:", GUILayout.Width(60));
                splineRoadTypes[i] = (MapPath.RoadType)EditorGUILayout.EnumPopup(splineRoadTypes[i]);
                EditorGUILayout.EndHorizontal();
            }
            
            // Segment单独设置
            EditorGUILayout.Space();
            showSegmentSettings = EditorGUILayout.Foldout(showSegmentSettings, "Segment单独设置");
            
            if (showSegmentSettings)
            {
                EditorGUILayout.HelpBox("您可以为每个路径段单独设置路径类型。这些设置将优先于整条Spline的设置。", MessageType.Info);
                
                // 显示单独segment设置
                segmentScrollPosition = EditorGUILayout.BeginScrollView(segmentScrollPosition, GUILayout.Height(200));
                
                for (int splineIndex = 0; splineIndex < splineContainer.Splines.Count; splineIndex++)
                {
                    Spline spline = splineContainer.Splines[splineIndex];
                    
                    if (spline.Count < 2) continue;
                    
                    EditorGUILayout.LabelField($"Spline {splineIndex}:", EditorStyles.boldLabel);
                    
                    for (int segmentIndex = 0; segmentIndex < spline.Count - 1; segmentIndex++)
                    {
                        string segmentKey = $"{splineIndex}_{segmentIndex}_{segmentIndex+1}";
                        
                        // 确保字典中有此segment的路径类型
                        if (!segmentRoadTypes.ContainsKey(segmentKey))
                        {
                            // 默认使用spline的路径类型
                            segmentRoadTypes[segmentKey] = splineRoadTypes.ContainsKey(splineIndex) ? 
                                splineRoadTypes[splineIndex] : MapPath.RoadType.Main;
                        }
                        
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField($"  段 {segmentIndex} → {segmentIndex+1}:", GUILayout.Width(100));
                        segmentRoadTypes[segmentKey] = (MapPath.RoadType)EditorGUILayout.EnumPopup(segmentRoadTypes[segmentKey]);
                        
                        // 重置按钮
                        if (GUILayout.Button("重置", GUILayout.Width(60)))
                        {
                            // 重置为spline的默认类型
                            segmentRoadTypes[segmentKey] = splineRoadTypes.ContainsKey(splineIndex) ? 
                                splineRoadTypes[splineIndex] : MapPath.RoadType.Main;
                        }
                        
                        EditorGUILayout.EndHorizontal();
                    }
                    
                    // 闭合spline额外处理
                    if (spline.Closed && spline.Count > 1)
                    {
                        string segmentKey = $"{splineIndex}_{spline.Count-1}_0";
                        
                        if (!segmentRoadTypes.ContainsKey(segmentKey))
                        {
                            segmentRoadTypes[segmentKey] = splineRoadTypes.ContainsKey(splineIndex) ? 
                                splineRoadTypes[splineIndex] : MapPath.RoadType.Main;
                        }
                        
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField($"  段 {spline.Count-1} → 0 (闭合):", GUILayout.Width(100));
                        segmentRoadTypes[segmentKey] = (MapPath.RoadType)EditorGUILayout.EnumPopup(segmentRoadTypes[segmentKey]);
                        
                        if (GUILayout.Button("重置", GUILayout.Width(60)))
                        {
                            segmentRoadTypes[segmentKey] = splineRoadTypes.ContainsKey(splineIndex) ? 
                                splineRoadTypes[splineIndex] : MapPath.RoadType.Main;
                        }
                        
                        EditorGUILayout.EndHorizontal();
                    }
                    
                    EditorGUILayout.Space();
                }
                
                EditorGUILayout.EndScrollView();
                
                // 保存和加载配置按钮
                EditorGUILayout.BeginHorizontal();
                
                if (GUILayout.Button("保存Segment配置"))
                {
                    SaveSegmentTypes();
                }
                
                if (GUILayout.Button("加载Segment配置"))
                {
                    LoadSegmentTypes();
                }
                
                EditorGUILayout.EndHorizontal();
            }
        }
        
        // 随机化按钮
        EditorGUILayout.Space();
        if (GUILayout.Button("随机化所有segment路径类型"))
        {
            RandomizeRoadTypes();
        }
        
        // 配置保存和加载按钮
        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("保存样式配置"))
        {
            SaveTypeStyles();
        }
        
        if (GUILayout.Button("加载样式配置"))
        {
            LoadTypeStyles();
        }
        
        EditorGUILayout.EndHorizontal();
        
        // 更新/生成按钮
        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        
        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("生成/更新地图", GUILayout.Height(40)))
        {
            GenerateMap();
        }
        GUI.backgroundColor = Color.white;
        
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.EndScrollView();
    }
    
    // 创建默认父物体
    private void CreateDefaultParents()
    {
        if (nodeParent == null)
        {
            nodeParent = new GameObject("Nodes");
        }
        
        if (pathParent == null)
        {
            pathParent = new GameObject("Paths");
        }
    }
    
    // 随机化路径类型
    private void RandomizeRoadTypes()
    {
        if (splineContainer == null || splineContainer.Splines.Count == 0)
        {
            EditorUtility.DisplayDialog("错误", "请先选择一个包含样条的容器！", "确定");
            return;
        }
        
        System.Array roadTypeValues = System.Enum.GetValues(typeof(MapPath.RoadType));
        System.Random random = new System.Random();
        
        int segmentsRandomized = 0;
        
        // 遍历所有spline的所有segment
        for (int splineIndex = 0; splineIndex < splineContainer.Splines.Count; splineIndex++)
        {
            Spline spline = splineContainer.Splines[splineIndex];
            
            if (spline.Count < 2) continue; // 跳过只有一个knot的spline
            
            // 随机化每个segment的类型
            for (int segmentIndex = 0; segmentIndex < spline.Count - 1; segmentIndex++)
            {
                string segmentKey = $"{splineIndex}_{segmentIndex}_{segmentIndex+1}";
                segmentRoadTypes[segmentKey] = (MapPath.RoadType)roadTypeValues.GetValue(random.Next(roadTypeValues.Length));
                segmentsRandomized++;
            }
            
            // 处理闭合spline的最后一个segment
            if (spline.Closed && spline.Count > 1)
            {
                string segmentKey = $"{splineIndex}_{spline.Count-1}_0";
                segmentRoadTypes[segmentKey] = (MapPath.RoadType)roadTypeValues.GetValue(random.Next(roadTypeValues.Length));
                segmentsRandomized++;
            }
        }
        
        Debug.Log($"已随机化 {segmentsRandomized} 个路径段的类型");
    }
    
    // 生成地图
    private void GenerateMap()
    {
        if (splineContainer == null || splineContainer.Splines.Count == 0)
        {
            EditorUtility.DisplayDialog("错误", "请先选择一个包含样条的容器！", "确定");
            return;
        }
        
        // 创建默认父物体
        CreateDefaultParents();
        
        // 清除现有节点和路径
        ClearExistingMap();
        
        // 创建节点和路径
        Dictionary<SplineKnotIndex, MapNode> knotToNodeMap = new Dictionary<SplineKnotIndex, MapNode>();
        
        // 使用世界坐标作为键的字典，用于处理相同位置的knot
        Dictionary<Vector3, MapNode> positionToNodeMap = new Dictionary<Vector3, MapNode>();
        
        // 第一步：遍历所有knots创建节点，处理相同位置的情况
        for (int splineIndex = 0; splineIndex < splineContainer.Splines.Count; splineIndex++)
        {
            Spline spline = splineContainer.Splines[splineIndex];
            
            for (int knotIndex = 0; knotIndex < spline.Count; knotIndex++)
            {
                SplineKnotIndex knotKey = new SplineKnotIndex(splineIndex, knotIndex);
                
                // 获取knot在世界空间中的位置
                Vector3 knotPosition = splineContainer.transform.TransformPoint(spline[knotIndex].Position);
                // 确保Z坐标正确（在2D环境中）
                knotPosition.z = 0;
                
                // 使用向量近似比较，处理浮点精度问题
                Vector3 roundedPosition = new Vector3(
                    Mathf.Round(knotPosition.x * 1000) / 1000,
                    Mathf.Round(knotPosition.y * 1000) / 1000,
                    0
                );
                
                // 检查是否已经在同一位置创建了节点
                MapNode existingNode = null;
                foreach (var pos in positionToNodeMap.Keys)
                {
                    if (Vector3.Distance(pos, roundedPosition) < 0.01f)
                    {
                        existingNode = positionToNodeMap[pos];
                        break;
                    }
                }
                
                if (existingNode != null)
                {
                    // 使用已有的节点
                    knotToNodeMap.Add(knotKey, existingNode);
                    Debug.Log($"在位置 {roundedPosition} 发现重复节点，复用已有节点 {existingNode.nodeName}");
                }
                else
                {
                    // 创建新节点
                    MapNode node = CreateNode(knotPosition, $"Node_{splineIndex}_{knotIndex}");
                    knotToNodeMap.Add(knotKey, node);
                    positionToNodeMap.Add(roundedPosition, node);
                }
            }
        }
        
        // 第二步：遍历所有splines创建路径
        for (int splineIndex = 0; splineIndex < splineContainer.Splines.Count; splineIndex++)
        {
            Spline spline = splineContainer.Splines[splineIndex];
            
            if (spline.Count < 2) continue; // 跳过只有一个knot的spline
            
            for (int segmentIndex = 0; segmentIndex < spline.Count - 1; segmentIndex++)
            {
                SplineKnotIndex startKnotKey = new SplineKnotIndex(splineIndex, segmentIndex);
                SplineKnotIndex endKnotKey = new SplineKnotIndex(splineIndex, segmentIndex + 1);
                
                MapNode startNode = knotToNodeMap[startKnotKey];
                MapNode endNode = knotToNodeMap[endKnotKey];
                
                // 跳过起点和终点相同的路径
                if (startNode == endNode)
                {
                    Debug.LogWarning($"跳过起点和终点相同的路径: {startNode.nodeName}");
                    continue;
                }
                
                // 获取此segment的路径类型（优先使用segment单独配置的类型）
                string segmentKey = $"{splineIndex}_{segmentIndex}_{segmentIndex+1}";
                MapPath.RoadType roadType;
                
                if (segmentRoadTypes.ContainsKey(segmentKey))
                {
                    // 使用segment单独配置的类型
                    roadType = segmentRoadTypes[segmentKey];
                    Debug.Log($"使用segment '{segmentKey}'单独配置的路径类型: {roadType}");
                }
                else if (splineRoadTypes.ContainsKey(splineIndex))
                {
                    // 使用spline的类型
                    roadType = splineRoadTypes[splineIndex];
                }
                else
                {
                    // 使用默认类型
                    roadType = MapPath.RoadType.Main;
                }
                
                // 根据spline段创建路径
                CreatePath(startNode, endNode, spline, segmentIndex, roadType);
            }
            
            // 如果spline是闭合的，创建起点到终点的路径
            if (spline.Closed && spline.Count > 1)
            {
                SplineKnotIndex startKnotKey = new SplineKnotIndex(splineIndex, spline.Count - 1);
                SplineKnotIndex endKnotKey = new SplineKnotIndex(splineIndex, 0);
                
                MapNode startNode = knotToNodeMap[startKnotKey];
                MapNode endNode = knotToNodeMap[endKnotKey];
                
                // 跳过起点和终点相同的路径
                if (startNode == endNode)
                {
                    Debug.LogWarning($"跳过起点和终点相同的闭合路径: {startNode.nodeName}");
                    continue;
                }
                
                // 获取闭合segment的路径类型
                string segmentKey = $"{splineIndex}_{spline.Count-1}_0";
                MapPath.RoadType roadType;
                
                if (segmentRoadTypes.ContainsKey(segmentKey))
                {
                    // 使用segment单独配置的类型
                    roadType = segmentRoadTypes[segmentKey];
                    Debug.Log($"使用闭合segment '{segmentKey}'单独配置的路径类型: {roadType}");
                }
                else if (splineRoadTypes.ContainsKey(splineIndex))
                {
                    // 使用spline的类型
                    roadType = splineRoadTypes[splineIndex];
                }
                else
                {
                    // 使用默认类型
                    roadType = MapPath.RoadType.Main;
                }
                
                // 创建闭合路径
                CreatePath(startNode, endNode, spline, spline.Count - 1, roadType);
            }
        }
        
        Debug.Log($"地图生成完成：创建了 {positionToNodeMap.Count} 个节点和多条路径");
        
        // 添加注释，提醒用户在PlayerMovement中调整贝塞尔曲线运动速度
        Debug.Log("提示：要使角色在贝塞尔曲线上匀速移动，请修改PlayerMovement脚本，使用曲线弧长参数化t值。");
    }
    
    // 清除现有地图
    private void ClearExistingMap()
    {
        // 清除节点
        if (nodeParent != null)
        {
            while (nodeParent.transform.childCount > 0)
            {
                DestroyImmediate(nodeParent.transform.GetChild(0).gameObject);
            }
        }
        
        // 清除路径
        if (pathParent != null)
        {
            while (pathParent.transform.childCount > 0)
            {
                DestroyImmediate(pathParent.transform.GetChild(0).gameObject);
            }
        }
    }
    
    // 创建节点
    private MapNode CreateNode(Vector3 position, string nodeName)
    {
        GameObject nodeObj = new GameObject(nodeName);
        nodeObj.transform.SetParent(nodeParent.transform);
        nodeObj.transform.position = position;
        
        SpriteRenderer sr = nodeObj.AddComponent<SpriteRenderer>();
        if (nodeSprite != null)
        {
            sr.sprite = nodeSprite;
            nodeObj.transform.localScale = new Vector3(nodeSize, nodeSize, 1);
        }
        else
        {
            // 当没有设置精灵时，直接设置为null而不是创建默认精灵
            sr.sprite = null;
            nodeObj.transform.localScale = new Vector3(nodeSize, nodeSize, 1);
        }
        
        MapNode mapNode = nodeObj.AddComponent<MapNode>();
        mapNode.nodeName = nodeName;
        
        return mapNode;
    }
    
    // 创建路径
    private void CreatePath(MapNode startNode, MapNode endNode, Spline spline, int segmentIndex, MapPath.RoadType roadType)
    {
        if (pathParent == null)
        {
            pathParent = new GameObject("Paths");
        }
        
        // 路径名称基于两个节点的名称
        string pathName = $"Path_{startNode.nodeName}_to_{endNode.nodeName}";
        GameObject pathObj = new GameObject(pathName);
        pathObj.transform.SetParent(pathParent.transform);
        
        LineRenderer lineRenderer = pathObj.AddComponent<LineRenderer>();
        
        // 获取路径类型对应的样式
        PathTypeStyle style = null;
        if (roadTypeStyles.ContainsKey(roadType))
        {
            style = roadTypeStyles[roadType];
        }
        else
        {
            // 如果没有找到样式，使用默认样式
            style = new PathTypeStyle(pathColor, pathWidth, roadType.ToString());
        }
        
        // 应用样式设置
        lineRenderer.startWidth = style.width;
        lineRenderer.endWidth = style.width;
        
        if (style.material != null)
        {
            lineRenderer.material = style.material;
        }
        else if (pathMaterial != null)
        {
            lineRenderer.material = pathMaterial;
        }
        else
        {
            // 使用默认材质
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        }
        
        lineRenderer.startColor = style.color;
        lineRenderer.endColor = style.color;
        
        MapPath mapPath = pathObj.AddComponent<MapPath>();
        mapPath.nodeA = startNode;
        mapPath.nodeB = endNode;
        mapPath.roadType = roadType; // 设置路径类型
        
        // 根据样条段的类型，设置路径类型和控制点
        BezierKnot startKnot = spline[segmentIndex];
        BezierKnot endKnot = spline[(segmentIndex + 1) % spline.Count]; // 使用模运算处理闭合样条
        
        // 检查是否是曲线（有有效的切线）
        Vector3 tangentOut = startKnot.TangentOut;
        Vector3 tangentIn = endKnot.TangentIn;
        bool hasControlPoints = !Mathf.Approximately(tangentOut.magnitude, 0) || !Mathf.Approximately(tangentIn.magnitude, 0);
        
        if (hasControlPoints)
        {
            // 有控制点，使用贝塞尔曲线
            mapPath.pathType = MapPath.PathType.Bezier;
            
            // 获取knot在世界空间的位置
            Vector3 worldStartPos = splineContainer.transform.TransformPoint(startKnot.Position);
            Vector3 worldEndPos = splineContainer.transform.TransformPoint(endKnot.Position);
            worldStartPos.z = 0;
            worldEndPos.z = 0;
            
            // 计算控制点位置 - 考虑knot的旋转和tangent的长度
            Vector3 worldCP1 = CalculateControlPointPosition(splineContainer.transform, startKnot, tangentOut, true);
            Vector3 worldCP2 = CalculateControlPointPosition(splineContainer.transform, endKnot, tangentIn, false);
            
            // 确保在2D平面上
            worldCP1.z = 0;
            worldCP2.z = 0;
            
            // 检查控制点位置相对于路径方向的合理性
            Vector3 pathDirection = (worldEndPos - worldStartPos).normalized;
            float dotCP1 = Vector3.Dot((worldCP1 - worldStartPos).normalized, pathDirection);
            float dotCP2 = Vector3.Dot((worldCP2 - worldEndPos).normalized, -pathDirection);
            
            // 如果控制点方向与路径方向相反，可能是需要交换控制点
            if (dotCP1 < 0 && dotCP2 < 0)
            {
                // 交换控制点
                Vector3 temp = worldCP1;
                worldCP1 = worldCP2;
                worldCP2 = temp;
                Debug.Log("控制点方向不合理，已交换控制点位置");
            }
            
            // 创建控制点游戏对象
            GameObject cp1 = new GameObject("ControlPoint1");
            GameObject cp2 = new GameObject("ControlPoint2");
            cp1.transform.SetParent(pathObj.transform);
            cp2.transform.SetParent(pathObj.transform);
            cp1.transform.position = worldCP1;
            cp2.transform.position = worldCP2;
            
            // 设置MapPath的控制点
            mapPath.controlPoints = new Transform[2] { cp1.transform, cp2.transform };
            
            // 调试信息
            Debug.Log($"路径 {pathName} - 创建贝塞尔曲线控制点: CP1={worldCP1}, CP2={worldCP2}");
        }
        else
        {
            // 没有控制点，使用直线
            mapPath.pathType = MapPath.PathType.Straight;
            Debug.Log($"路径 {pathName} - 创建直线路径");
        }
        
        // 更新路径视觉效果
        mapPath.UpdatePathVisual();
    }
    
    // 计算贝塞尔曲线控制点的位置
    private Vector3 CalculateControlPointPosition(Transform containerTransform, BezierKnot knot, Vector3 tangent, bool isTangentOut)
    {
        // 获取knot的位置和旋转
        Vector3 knotPosition = containerTransform.TransformPoint(knot.Position);
        Quaternion knotRotation = containerTransform.rotation * knot.Rotation;
        
        // 在2D环境中，我们需要调整方向为上/下/左/右
        Vector2 direction;
        float tangentLength = tangent.magnitude;
        
        // 在2D空间中，Unity Spline的tangent方向需要旋转90度
        // 在Spline系统中，向右是Z轴正方向，而在2D中我们需要X轴正方向
        if (isTangentOut)
        {
            // 对于TangentOut，我们需要使用前向方向，在2D中是(0,1,0)旋转到XY平面
            direction = (knotRotation * Vector3.forward).normalized;
        }
        else
        {
            // 对于TangentIn，我们需要使用后向方向，在2D中是(0,-1,0)旋转到XY平面
            direction = (knotRotation * Vector3.back).normalized;
        }
        
        // 根据方向和长度计算控制点的偏移量
        Vector2 offset = direction * tangentLength;
        
        // 将偏移量应用到knot位置
        Vector3 controlPointPosition = new Vector3(
            knotPosition.x + offset.x,
            knotPosition.y + offset.y,
            knotPosition.z
        );
        
        // 调试信息
        Debug.Log($"计算控制点: Knot位置={knotPosition}, 方向={direction}, 长度={tangentLength}, 结果={controlPointPosition}");
        
        return controlPointPosition;
    }
    
    // 帮助结构，用于标识样条中的特定结点
    private struct SplineKnotIndex
    {
        public int SplineIndex;
        public int KnotIndex;
        
        public SplineKnotIndex(int splineIndex, int knotIndex)
        {
            SplineIndex = splineIndex;
            KnotIndex = knotIndex;
        }
    }
    
    // 初始化路径类型样式默认值
    private void InitializeRoadTypeStyles()
    {
        if (roadTypeStyles.Count == 0)
        {
            roadTypeStyles[MapPath.RoadType.Main] = new PathTypeStyle(Color.black, 0.15f, "主路");
            roadTypeStyles[MapPath.RoadType.Secondary] = new PathTypeStyle(Color.gray, 0.1f, "次要道路");
            roadTypeStyles[MapPath.RoadType.Trail] = new PathTypeStyle(new Color(0.7f, 0.5f, 0.2f), 0.08f, "小径");
            roadTypeStyles[MapPath.RoadType.Gravel] = new PathTypeStyle(new Color(0.7f, 0.5f, 0.2f), 0.08f, "碎石路");
            roadTypeStyles[MapPath.RoadType.Unpaved] = new PathTypeStyle(new Color(0.5f, 0.5f, 0.5f), 0.12f, "土路");
        }
    }
    
    // 保存路径类型样式
    private void SaveTypeStyles()
    {
        // 创建配置目录
        if (!Directory.Exists(CONFIG_DIRECTORY))
        {
            Directory.CreateDirectory(CONFIG_DIRECTORY);
        }
        
        // 转换为可序列化格式
        Dictionary<string, JsonPathTypeStyle> serializableStyles = new Dictionary<string, JsonPathTypeStyle>();
        foreach (var kvp in roadTypeStyles)
        {
            PathTypeStyle style = kvp.Value;
            JsonPathTypeStyle jsonStyle = new JsonPathTypeStyle
            {
                colorR = style.color.r,
                colorG = style.color.g,
                colorB = style.color.b,
                colorA = style.color.a,
                width = style.width,
                materialPath = style.material != null ? AssetDatabase.GetAssetPath(style.material) : "",
                displayName = style.displayName
            };
            serializableStyles[kvp.Key.ToString()] = jsonStyle;
        }
        
        // 序列化为JSON
        string json = JsonUtility.ToJson(new JsonPathTypeStyleDictionary { styles = serializableStyles }, true);
        
        // 保存文件
        File.WriteAllText(Path.Combine(CONFIG_DIRECTORY, TYPE_STYLES_FILE), json);
        Debug.Log("路径类型样式已保存");
    }
    
    // 加载路径类型样式
    private void LoadTypeStyles()
    {
        string filePath = Path.Combine(CONFIG_DIRECTORY, TYPE_STYLES_FILE);
        if (File.Exists(filePath))
        {
            try
            {
                string json = File.ReadAllText(filePath);
                JsonPathTypeStyleDictionary data = JsonUtility.FromJson<JsonPathTypeStyleDictionary>(json);
                
                foreach (var kvp in data.styles)
                {
                    // 解析枚举值
                    if (System.Enum.TryParse<MapPath.RoadType>(kvp.Key, out MapPath.RoadType roadType))
                    {
                        JsonPathTypeStyle jsonStyle = kvp.Value;
                        
                        // 创建颜色
                        Color color = new Color(jsonStyle.colorR, jsonStyle.colorG, jsonStyle.colorB, jsonStyle.colorA);
                        
                        // 加载材质
                        Material material = null;
                        if (!string.IsNullOrEmpty(jsonStyle.materialPath))
                        {
                            material = AssetDatabase.LoadAssetAtPath<Material>(jsonStyle.materialPath);
                        }
                        
                        // 创建样式对象
                        PathTypeStyle style = new PathTypeStyle(color, jsonStyle.width, jsonStyle.displayName);
                        style.material = material;
                        
                        // 添加到字典
                        roadTypeStyles[roadType] = style;
                    }
                }
                
                Debug.Log("已加载路径类型样式");
            }
            catch (System.Exception e)
            {
                Debug.LogError("加载路径类型样式失败: " + e.Message);
            }
        }
    }
    
    // 保存segment类型配置
    private void SaveSegmentTypes()
    {
        // 创建配置目录
        if (!Directory.Exists(CONFIG_DIRECTORY))
        {
            Directory.CreateDirectory(CONFIG_DIRECTORY);
        }
        
        // 转换为可序列化格式
        Dictionary<string, string> serializableSegments = new Dictionary<string, string>();
        foreach (var kvp in segmentRoadTypes)
        {
            serializableSegments[kvp.Key] = kvp.Value.ToString();
        }
        
        // 序列化为JSON
        string json = JsonUtility.ToJson(new JsonSegmentTypesDictionary { segments = serializableSegments }, true);
        
        // 保存文件
        File.WriteAllText(Path.Combine(CONFIG_DIRECTORY, SEGMENT_TYPES_FILE), json);
        Debug.Log("Segment类型配置已保存");
    }
    
    // 加载segment类型配置
    private void LoadSegmentTypes()
    {
        string filePath = Path.Combine(CONFIG_DIRECTORY, SEGMENT_TYPES_FILE);
        if (File.Exists(filePath))
        {
            try
            {
                string json = File.ReadAllText(filePath);
                JsonSegmentTypesDictionary data = JsonUtility.FromJson<JsonSegmentTypesDictionary>(json);
                
                segmentRoadTypes.Clear();
                foreach (var kvp in data.segments)
                {
                    // 解析枚举值
                    if (System.Enum.TryParse<MapPath.RoadType>(kvp.Value, out MapPath.RoadType roadType))
                    {
                        segmentRoadTypes[kvp.Key] = roadType;
                    }
                }
                
                Debug.Log("已加载Segment类型配置");
            }
            catch (System.Exception e)
            {
                Debug.LogError("加载Segment类型配置失败: " + e.Message);
            }
        }
    }
    
    // JSON序列化辅助类
    [System.Serializable]
    private class JsonPathTypeStyle
    {
        public float colorR, colorG, colorB, colorA;
        public float width;
        public string materialPath;
        public string displayName;
    }
    
    [System.Serializable]
    private class JsonPathTypeStyleDictionary
    {
        public Dictionary<string, JsonPathTypeStyle> styles;
    }
    
    [System.Serializable]
    private class JsonSegmentTypesDictionary
    {
        public Dictionary<string, string> segments;
    }
    
    private void OnDisable()
    {
        // 保存配置
        SaveTypeStyles();
        SaveSegmentTypes();
    }
} 