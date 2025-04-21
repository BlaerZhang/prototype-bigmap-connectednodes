using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class MapCreatorTool : EditorWindow
{
    private GameObject nodeParent;
    private GameObject pathParent;
    
    private Sprite nodeSprite;
    private Material pathMaterial = default;
    private Color pathColor = Color.black;
    private float pathWidth = 0.1f;
    
    private MapNode selectedNodeA;
    private MapNode selectedNodeB;
    private MapPath.PathType pathType = MapPath.PathType.Straight;
    private int controlPointCount = 1;
    
    // 新增变量
    private string nodeName = "新节点";
    private MapNode lastCreatedNode;
    private bool autoConnectToLastNode = false;
    private bool quickCreateMode = false;
    private Vector2 scrollPosition;
    private List<MapNode> selectedNodes = new List<MapNode>();
    private bool showAllNodes = false;
    private MapNode[] allNodes = new MapNode[0];
    private bool autoIncrementNodeName = false;
    private string nodeNamePrefix = "Node_";
    private int nodeCounter = 1;
    
    // 新增一个标志变量，用于跟踪创建模式的状态
    private bool isSceneCreateModeActive = false;
    
    // 记录选中的节点
    private List<MapNode> selectedSceneNodes = new List<MapNode>();
    private bool quickPathCreationEnabled = true;
    private bool quickPathDeletionEnabled = true;
    
    // 用于存储当前选中待删除的路径
    private MapPath selectedPathToDelete = null;
    
    private enum PathOperation
    {
        Create,
        Delete
    }
    
    [MenuItem("Tools/Map Creator")]
    public static void ShowWindow()
    {
        GetWindow<MapCreatorTool>("地图创建工具");
    }
    
    private void OnEnable()
    {
        // 加载所有节点
        RefreshNodeList();
    }
    
    private void RefreshNodeList()
    {
        allNodes = FindObjectsOfType<MapNode>();
    }
    
    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        GUILayout.Label("地图节点和路径创建工具", EditorStyles.boldLabel);
        
        // 显示当前模式
        if (isSceneCreateModeActive)
        {
            EditorGUILayout.HelpBox("当前处于场景点击创建模式。点击右键或按下ESC键退出。", MessageType.Info);
            
            if (GUILayout.Button("退出点击创建模式", GUILayout.Height(30)))
            {
                ExitSceneCreateMode();
            }
        }
        
        // 显示选中路径的删除选项
        if (selectedPathToDelete != null)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"选中路径: {selectedPathToDelete.name}", EditorStyles.boldLabel);
            
            if (GUILayout.Button("删除选中路径", GUILayout.Height(30)))
            {
                DestroyImmediate(selectedPathToDelete.gameObject);
                selectedPathToDelete = null;
            }
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }
        
        // 显示选中节点的操作选项
        MapNode selectedNode = null;
        if (Selection.activeGameObject != null)
        {
            selectedNode = Selection.activeGameObject.GetComponent<MapNode>();
            if (selectedNode != null)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField($"选中节点: {selectedNode.nodeName}", EditorStyles.boldLabel);
                
                if (GUILayout.Button("删除节点及相连路径", GUILayout.Height(30)))
                {
                    DeleteNodeWithConnectedPaths(selectedNode);
                }
                
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
            }
        }
        
        // 显示批量清理功能
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.Label("批量清理工具", EditorStyles.boldLabel);
        
        if (GUILayout.Button("清理所有非法路径", GUILayout.Height(30)))
        {
            CleanInvalidPaths();
        }
        
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space();
        
        // 显示选中节点间的操作选项
        if (selectedSceneNodes.Count == 2 && quickPathDeletionEnabled)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"选中节点: {selectedSceneNodes[0].nodeName} 和 {selectedSceneNodes[1].nodeName}", EditorStyles.boldLabel);
            
            if (GUILayout.Button("删除这两个节点间的所有路径", GUILayout.Height(30)))
            {
                DeletePathsBetweenNodes(selectedSceneNodes[0], selectedSceneNodes[1]);
            }
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }
        
        // 添加快速路径操作选项
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.Label("快速路径操作", EditorStyles.boldLabel);
        
        quickPathCreationEnabled = EditorGUILayout.Toggle("启用选中节点快速创建路径", quickPathCreationEnabled);
        if (quickPathCreationEnabled)
        {
            GUILayout.Label("选中两个节点后将自动创建路径", EditorStyles.miniLabel);
        }
        
        quickPathDeletionEnabled = EditorGUILayout.Toggle("启用路径快速删除", quickPathDeletionEnabled);
        if (quickPathDeletionEnabled)
        {
            GUILayout.Label("选中路径后显示删除按钮，选中两个节点可删除它们之间的路径", EditorStyles.miniLabel);
        }
        
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.Space();
        
        // 父物体设置
        GUILayout.Label("父物体设置", EditorStyles.boldLabel);
        nodeParent = (GameObject)EditorGUILayout.ObjectField("节点父物体", nodeParent, typeof(GameObject), true);
        pathParent = (GameObject)EditorGUILayout.ObjectField("路径父物体", pathParent, typeof(GameObject), true);
        
        if (GUILayout.Button("创建默认父物体"))
        {
            CreateDefaultParents();
        }
        
        EditorGUILayout.Space();
        
        // 节点设置区域
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.Label("节点设置", EditorStyles.boldLabel);
        
        nodeSprite = (Sprite)EditorGUILayout.ObjectField("节点精灵", nodeSprite, typeof(Sprite), false);
        
        // 节点名称输入
        nodeName = EditorGUILayout.TextField("节点名称", nodeName);
        
        // 快速创建模式
        EditorGUILayout.BeginHorizontal();
        quickCreateMode = EditorGUILayout.Toggle("快速创建模式", quickCreateMode);
        if (quickCreateMode)
        {
            autoConnectToLastNode = EditorGUILayout.Toggle("自动连接上一节点", autoConnectToLastNode);
        }
        EditorGUILayout.EndHorizontal();
        
        // 自动递增节点名称
        if (quickCreateMode)
        {
            EditorGUILayout.BeginHorizontal();
            autoIncrementNodeName = EditorGUILayout.Toggle("自动递增名称", autoIncrementNodeName);
            if (autoIncrementNodeName)
            {
                nodeNamePrefix = EditorGUILayout.TextField("名称前缀", nodeNamePrefix);
                nodeCounter = EditorGUILayout.IntField("开始编号", nodeCounter);
            }
            EditorGUILayout.EndHorizontal();
        }
        
        // 创建节点按钮
        if (GUILayout.Button("创建新节点"))
        {
            GameObject newNode = CreateNewNode();
            
            // 如果启用了自动连接且有上一个创建的节点
            if (quickCreateMode && autoConnectToLastNode && lastCreatedNode != null)
            {
                CreatePathBetween(lastCreatedNode, newNode.GetComponent<MapNode>(), pathType);
            }
            
            // 更新最后创建的节点
            lastCreatedNode = newNode.GetComponent<MapNode>();
            
            // 刷新节点列表
            RefreshNodeList();
        }
        
        if (GUILayout.Button("在场景视图中点击创建节点"))
        {
            // 如果已经在点击创建模式，先退出
            if (isSceneCreateModeActive)
            {
                ExitSceneCreateMode();
            }
            
            // 进入点击创建模式
            SceneView.duringSceneGui += OnSceneGUI;
            isSceneCreateModeActive = true;
            // 聚焦到场景视图
            SceneView.lastActiveSceneView.Focus();
        }
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.Space();
        
        // 路径设置区域
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.Label("路径设置", EditorStyles.boldLabel);
        
        pathMaterial = (Material)EditorGUILayout.ObjectField("路径材质", pathMaterial, typeof(Material), false);
        pathColor = EditorGUILayout.ColorField("路径颜色", pathColor);
        pathWidth = EditorGUILayout.FloatField("路径宽度", pathWidth);
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.Space();
        
        // 创建路径区域
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.Label("创建路径", EditorStyles.boldLabel);
        
        selectedNodeA = (MapNode)EditorGUILayout.ObjectField("起始节点", selectedNodeA, typeof(MapNode), true);
        selectedNodeB = (MapNode)EditorGUILayout.ObjectField("结束节点", selectedNodeB, typeof(MapNode), true);
        
        pathType = (MapPath.PathType)EditorGUILayout.EnumPopup("路径类型", pathType);
        
        if (pathType != MapPath.PathType.Straight)
        {
            controlPointCount = EditorGUILayout.IntSlider("控制点数量", controlPointCount, 1, pathType == MapPath.PathType.Bezier ? 2 : 5);
        }
        
        if (GUILayout.Button("创建路径"))
        {
            CreatePath();
        }
        
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.Space();
        
        // 批量操作区域
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.Label("批量操作", EditorStyles.boldLabel);
        
        showAllNodes = EditorGUILayout.Foldout(showAllNodes, "节点选择");
        
        if (showAllNodes)
        {
            EditorGUI.indentLevel++;
            
            // 刷新节点按钮
            if (GUILayout.Button("刷新节点列表"))
            {
                RefreshNodeList();
            }
            
            // 显示所有节点的列表
            for (int i = 0; i < allNodes.Length; i++)
            {
                EditorGUILayout.BeginHorizontal();
                
                bool isSelected = selectedNodes.Contains(allNodes[i]);
                bool newState = EditorGUILayout.Toggle(isSelected, GUILayout.Width(20));
                
                if (newState != isSelected)
                {
                    if (newState)
                        selectedNodes.Add(allNodes[i]);
                    else
                        selectedNodes.Remove(allNodes[i]);
                }
                
                EditorGUILayout.LabelField(allNodes[i].nodeName);
                
                // 添加点击选择为起点或终点的功能
                if (GUILayout.Button("设为起点", GUILayout.Width(80)))
                {
                    selectedNodeA = allNodes[i];
                }
                
                if (GUILayout.Button("设为终点", GUILayout.Width(80)))
                {
                    selectedNodeB = allNodes[i];
                }
                
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUI.indentLevel--;
            
            EditorGUILayout.Space();
            
            // 批量操作按钮
            if (selectedNodes.Count > 0)
            {
                if (GUILayout.Button("连接选中节点（创建完整连接）"))
                {
                    ConnectSelectedNodes(true);
                }
                
                if (GUILayout.Button("连接选中节点（创建链状连接）"))
                {
                    ConnectSelectedNodes(false);
                }
                
                if (GUILayout.Button("清除选择"))
                {
                    selectedNodes.Clear();
                }
            }
        }
        
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("创建示例地图"))
        {
            CreateExampleMap();
        }
        
        EditorGUILayout.EndScrollView();
    }
    
    private void OnSceneGUI(SceneView sceneView)
    {
        // 检测场景视图中的鼠标单击
        Event e = Event.current;
        
        // 添加对ESC键的支持
        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
        {
            ExitSceneCreateMode();
            e.Use();
            return;
        }
        
        if (e.type == EventType.MouseDown && e.button == 0)
        {
            // 获取鼠标位置
            Vector2 mousePosition = e.mousePosition;
            
            // 将鼠标位置从屏幕坐标转换为世界坐标
            Ray ray = HandleUtility.GUIPointToWorldRay(mousePosition);
            Vector3 worldPoint = ray.origin;
            worldPoint.z = 0; // 确保在2D平面上
            
            // 创建节点
            GameObject newNode = CreateNodeAt(worldPoint, nodeName);
            
            // 如果启用了自动连接且有上一个创建的节点
            if (autoConnectToLastNode && lastCreatedNode != null)
            {
                CreatePathBetween(lastCreatedNode, newNode.GetComponent<MapNode>(), pathType);
            }
            
            // 更新最后创建的节点
            lastCreatedNode = newNode.GetComponent<MapNode>();
            
            // 刷新节点列表
            RefreshNodeList();
            
            // 使事件被使用，防止其他事件处理
            e.Use();
        }
        
        // 右键点击停止节点创建模式
        if (e.type == EventType.MouseDown && e.button == 1)
        {
            ExitSceneCreateMode();
            e.Use();
            return;
        }
        
        // 在场景中显示提示
        Handles.BeginGUI();
        GUI.Label(new Rect(10, 10, 400, 40), "左键点击创建节点，右键点击或按ESC键退出创建模式");
        Handles.EndGUI();
        
        // 强制重绘场景视图
        sceneView.Repaint();
    }
    
    private void ConnectSelectedNodes(bool createFullMesh)
    {
        if (selectedNodes.Count < 2)
        {
            EditorUtility.DisplayDialog("错误", "请至少选择两个节点！", "确定");
            return;
        }
        
        if (createFullMesh)
        {
            // 完整连接模式，每个节点与其他所有节点连接
            for (int i = 0; i < selectedNodes.Count; i++)
            {
                for (int j = i + 1; j < selectedNodes.Count; j++)
                {
                    CreatePathBetween(selectedNodes[i], selectedNodes[j], pathType);
                }
            }
        }
        else
        {
            // 链状连接模式，按顺序连接节点
            for (int i = 0; i < selectedNodes.Count - 1; i++)
            {
                CreatePathBetween(selectedNodes[i], selectedNodes[i + 1], pathType);
            }
        }
    }
    
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
    
    private GameObject CreateNewNode()
    {
        if (nodeParent == null)
        {
            nodeParent = new GameObject("Nodes");
        }
        
        // 使用用户输入的名称或自动递增名称
        string finalNodeName;
        if (quickCreateMode && autoIncrementNodeName)
        {
            finalNodeName = nodeNamePrefix + nodeCounter;
            nodeCounter++; // 递增计数器
        }
        else
        {
            finalNodeName = string.IsNullOrEmpty(nodeName) ? "Node" + Random.Range(100, 999) : nodeName;
        }
        
        GameObject nodeObj = new GameObject(finalNodeName);
        nodeObj.transform.SetParent(nodeParent.transform);
        nodeObj.transform.position = SceneView.lastActiveSceneView.camera.transform.position + SceneView.lastActiveSceneView.camera.transform.forward * 5;
        
        SpriteRenderer sr = nodeObj.AddComponent<SpriteRenderer>();
        if (nodeSprite != null)
        {
            sr.sprite = nodeSprite;
        }
        else
        {
            // 创建一个默认的圆形
            Texture2D tex = new Texture2D(128, 128);
            Color[] colors = new Color[128 * 128];
            for (int y = 0; y < 128; y++)
            {
                for (int x = 0; x < 128; x++)
                {
                    float dx = x - 64;
                    float dy = y - 64;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    colors[y * 128 + x] = dist < 60 ? Color.white : Color.clear;
                }
            }
            tex.SetPixels(colors);
            tex.Apply();
            
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, 128, 128), new Vector2(0.5f, 0.5f));
        }
        
        MapNode mapNode = nodeObj.AddComponent<MapNode>();
        mapNode.nodeName = finalNodeName;
        
        Selection.activeGameObject = nodeObj;
        
        return nodeObj;
    }
    
    private void CreatePath()
    {
        if (selectedNodeA == null || selectedNodeB == null)
        {
            EditorUtility.DisplayDialog("错误", "请选择两个节点！", "确定");
            return;
        }
        
        if (selectedNodeA == selectedNodeB)
        {
            EditorUtility.DisplayDialog("错误", "不能连接节点到自身！", "确定");
            return;
        }
        
        CreatePathBetween(selectedNodeA, selectedNodeB, pathType, controlPointCount);
    }
    
    private void CreateExampleMap()
    {
        if (EditorUtility.DisplayDialog("创建示例地图", "这将创建一个包含节点和路径的示例地图。继续？", "创建", "取消"))
        {
            CreateDefaultParents();
            
            // 删除现有节点和路径
            while (nodeParent.transform.childCount > 0)
            {
                DestroyImmediate(nodeParent.transform.GetChild(0).gameObject);
            }
            
            while (pathParent.transform.childCount > 0)
            {
                DestroyImmediate(pathParent.transform.GetChild(0).gameObject);
            }
            
            // 创建一个简单的网格节点
            List<MapNode> nodes = new List<MapNode>();
            
            // 创建中心节点
            GameObject centerNode = CreateNodeAt(Vector3.zero, "中心城镇");
            nodes.Add(centerNode.GetComponent<MapNode>());
            
            // 创建外围节点
            GameObject northNode = CreateNodeAt(new Vector3(0, 8, 0), "北方村庄");
            GameObject eastNode = CreateNodeAt(new Vector3(10, 0, 0), "东方哨所");
            GameObject southNode = CreateNodeAt(new Vector3(0, -8, 0), "南方农场");
            GameObject westNode = CreateNodeAt(new Vector3(-10, 0, 0), "西方矿场");
            
            nodes.Add(northNode.GetComponent<MapNode>());
            nodes.Add(eastNode.GetComponent<MapNode>());
            nodes.Add(southNode.GetComponent<MapNode>());
            nodes.Add(westNode.GetComponent<MapNode>());
            
            // 创建斜向节点
            GameObject northEastNode = CreateNodeAt(new Vector3(7, 7, 0), "东北森林");
            GameObject southEastNode = CreateNodeAt(new Vector3(7, -7, 0), "东南湖泊");
            GameObject southWestNode = CreateNodeAt(new Vector3(-7, -7, 0), "西南墓地");
            GameObject northWestNode = CreateNodeAt(new Vector3(-7, 7, 0), "西北山脉");
            
            nodes.Add(northEastNode.GetComponent<MapNode>());
            nodes.Add(southEastNode.GetComponent<MapNode>());
            nodes.Add(southWestNode.GetComponent<MapNode>());
            nodes.Add(northWestNode.GetComponent<MapNode>());
            
            // 刷新节点列表
            RefreshNodeList();
            
            // 创建路径
            // 从中心到四个主方向
            CreatePathBetween(centerNode.GetComponent<MapNode>(), northNode.GetComponent<MapNode>(), MapPath.PathType.Straight);
            CreatePathBetween(centerNode.GetComponent<MapNode>(), eastNode.GetComponent<MapNode>(), MapPath.PathType.Straight);
            CreatePathBetween(centerNode.GetComponent<MapNode>(), southNode.GetComponent<MapNode>(), MapPath.PathType.Straight);
            CreatePathBetween(centerNode.GetComponent<MapNode>(), westNode.GetComponent<MapNode>(), MapPath.PathType.Straight);
            
            // 创建环形路径
            CreatePathBetween(northNode.GetComponent<MapNode>(), northEastNode.GetComponent<MapNode>(), MapPath.PathType.Straight);
            CreatePathBetween(eastNode.GetComponent<MapNode>(), northEastNode.GetComponent<MapNode>(), MapPath.PathType.Straight);
            
            CreatePathBetween(eastNode.GetComponent<MapNode>(), southEastNode.GetComponent<MapNode>(), MapPath.PathType.Straight);
            CreatePathBetween(southNode.GetComponent<MapNode>(), southEastNode.GetComponent<MapNode>(), MapPath.PathType.Straight);
            
            CreatePathBetween(southNode.GetComponent<MapNode>(), southWestNode.GetComponent<MapNode>(), MapPath.PathType.Straight);
            CreatePathBetween(westNode.GetComponent<MapNode>(), southWestNode.GetComponent<MapNode>(), MapPath.PathType.Straight);
            
            CreatePathBetween(westNode.GetComponent<MapNode>(), northWestNode.GetComponent<MapNode>(), MapPath.PathType.Straight);
            CreatePathBetween(northNode.GetComponent<MapNode>(), northWestNode.GetComponent<MapNode>(), MapPath.PathType.Straight);
            
            // 创建一些曲线路径
            CreatePathBetween(northWestNode.GetComponent<MapNode>(), southEastNode.GetComponent<MapNode>(), MapPath.PathType.Bezier, 2);
            CreatePathBetween(northEastNode.GetComponent<MapNode>(), southWestNode.GetComponent<MapNode>(), MapPath.PathType.Bezier, 2);
            
            // 创建一些折线路径
            CreatePathBetween(northWestNode.GetComponent<MapNode>(), southNode.GetComponent<MapNode>(), MapPath.PathType.Segmented, 3);
            CreatePathBetween(northEastNode.GetComponent<MapNode>(), westNode.GetComponent<MapNode>(), MapPath.PathType.Segmented, 3);
            
            // 创建玩家
            if (FindObjectOfType<PlayerMovement>() == null)
            {
                GameObject player = new GameObject("Player");
                SpriteRenderer playerSR = player.AddComponent<SpriteRenderer>();
                
                // 创建简单的玩家精灵
                Texture2D playerTex = new Texture2D(64, 64);
                Color[] playerColors = new Color[64 * 64];
                for (int y = 0; y < 64; y++)
                {
                    for (int x = 0; x < 64; x++)
                    {
                        float dx = x - 32;
                        float dy = y - 32;
                        float dist = Mathf.Sqrt(dx * dx + dy * dy);
                        playerColors[y * 64 + x] = dist < 30 ? Color.red : Color.clear;
                    }
                }
                playerTex.SetPixels(playerColors);
                playerTex.Apply();
                
                playerSR.sprite = Sprite.Create(playerTex, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f));
                player.AddComponent<PlayerMovement>();
                
                player.transform.position = centerNode.transform.position;
                player.transform.position = new Vector3(player.transform.position.x, player.transform.position.y, -0.1f);
            }
            
            // 创建GameManager
            if (FindObjectOfType<GameManager>() == null)
            {
                GameObject managerObj = new GameObject("GameManager");
                GameManager manager = managerObj.AddComponent<GameManager>();
                
                // 创建玩家预制体
                if (FindObjectOfType<PlayerMovement>() != null)
                {
                    GameObject playerObj = FindObjectOfType<PlayerMovement>().gameObject;
                    manager.playerPrefab = playerObj;
                }
            }
            
            // 创建摄像机
            if (Camera.main == null)
            {
                GameObject cameraObj = new GameObject("Main Camera");
                Camera camera = cameraObj.AddComponent<Camera>();
                camera.orthographic = true;
                camera.orthographicSize = 12;
                camera.backgroundColor = new Color(0.1f, 0.1f, 0.1f);
                camera.transform.position = new Vector3(0, 0, -10);
                camera.tag = "MainCamera";
            }
        }
    }
    
    private GameObject CreateNodeAt(Vector3 position, string nodeName)
    {
        if (nodeParent == null)
        {
            nodeParent = new GameObject("Nodes");
        }
        
        // 确保节点名称非空或使用自动递增名称
        string finalNodeName;
        if (quickCreateMode && autoIncrementNodeName)
        {
            finalNodeName = nodeNamePrefix + nodeCounter;
            nodeCounter++; // 递增计数器
        }
        else
        {
            finalNodeName = string.IsNullOrEmpty(nodeName) ? "Node" + Random.Range(100, 999) : nodeName;
        }
        
        GameObject nodeObj = new GameObject(finalNodeName);
        nodeObj.transform.SetParent(nodeParent.transform);
        nodeObj.transform.position = position;
        
        SpriteRenderer sr = nodeObj.AddComponent<SpriteRenderer>();
        if (nodeSprite != null)
        {
            sr.sprite = nodeSprite;
        }
        else
        {
            // 创建一个默认的圆形
            Texture2D tex = new Texture2D(128, 128);
            Color[] colors = new Color[128 * 128];
            for (int y = 0; y < 128; y++)
            {
                for (int x = 0; x < 128; x++)
                {
                    float dx = x - 64;
                    float dy = y - 64;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    colors[y * 128 + x] = dist < 60 ? Color.white : Color.clear;
                }
            }
            tex.SetPixels(colors);
            tex.Apply();
            
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, 128, 128), new Vector2(0.5f, 0.5f));
        }
        
        MapNode mapNode = nodeObj.AddComponent<MapNode>();
        mapNode.nodeName = finalNodeName;
        
        Selection.activeGameObject = nodeObj;
        
        return nodeObj;
    }
    
    private void CreatePathBetween(MapNode nodeA, MapNode nodeB, MapPath.PathType pathType, int controlPointCount = 0)
    {
        if (pathParent == null)
        {
            pathParent = new GameObject("Paths");
        }
        
        // 路径名称基于两个节点的名称
        string pathName = "Path_" + nodeA.nodeName + "_to_" + nodeB.nodeName;
        GameObject pathObj = new GameObject(pathName);
        pathObj.transform.SetParent(pathParent.transform);
        
        LineRenderer lineRenderer = pathObj.AddComponent<LineRenderer>();
        lineRenderer.startWidth = pathWidth > 0 ? pathWidth : 0.5f;
        lineRenderer.endWidth = pathWidth > 0 ? pathWidth : 0.5f;
        
        if (pathMaterial != null)
        {
            lineRenderer.material = pathMaterial;
        }
        else
        {
            // 使用默认材质
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        }
        
        lineRenderer.startColor = pathColor;
        lineRenderer.endColor = pathColor;
        lineRenderer.positionCount = 2; // 默认设置，会在MapPath的Start中更新
        
        MapPath mapPath = pathObj.AddComponent<MapPath>();
        mapPath.nodeA = nodeA;
        mapPath.nodeB = nodeB;
        mapPath.pathType = pathType;
        
        // 创建控制点
        if (pathType != MapPath.PathType.Straight && controlPointCount > 0)
        {
            mapPath.controlPoints = new Transform[controlPointCount];
            
            for (int i = 0; i < controlPointCount; i++)
            {
                GameObject controlPoint = new GameObject("ControlPoint" + (i + 1));
                controlPoint.transform.SetParent(pathObj.transform);
                
                // 设置控制点位置
                Vector3 posA = nodeA.transform.position;
                Vector3 posB = nodeB.transform.position;
                float t = (i + 1.0f) / (controlPointCount + 1.0f);
                
                if (pathType == MapPath.PathType.Bezier)
                {
                    // 对于贝塞尔曲线，我们偏移控制点以创建曲线效果
                    Vector3 midPoint = Vector3.Lerp(posA, posB, t);
                    Vector3 perpendicular = new Vector3(-(posB.y - posA.y), posB.x - posA.x, 0).normalized;
                    
                    // 控制点1向一个方向偏移，控制点2向相反方向偏移
                    float offset = Vector3.Distance(posA, posB) * 0.3f;
                    if (i == 0)
                        controlPoint.transform.position = midPoint + perpendicular * offset;
                    else
                        controlPoint.transform.position = midPoint - perpendicular * offset;
                }
                else // PathType.Segmented
                {
                    // 对于折线，我们在直线上放置控制点
                    controlPoint.transform.position = Vector3.Lerp(posA, posB, t);
                    
                    // 随机偏移，使折线更自然
                    Vector3 perpendicular = new Vector3(-(posB.y - posA.y), posB.x - posA.x, 0).normalized;
                    float offset = Vector3.Distance(posA, posB) * 0.2f * Mathf.Sin((i + 1) * Mathf.PI / (controlPointCount + 1));
                    controlPoint.transform.position += perpendicular * offset;
                }
                
                mapPath.controlPoints[i] = controlPoint.transform;
            }
        }
        
        // 更新路径视觉效果
        mapPath.UpdatePathVisual();
        
        // 选择新创建的路径
        Selection.activeGameObject = pathObj;
    }
    
    // 添加退出场景创建模式的方法
    private void ExitSceneCreateMode()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
        isSceneCreateModeActive = false;
        Repaint(); // 刷新编辑器窗口
        
        // 输出日志确认已退出
        Debug.Log("已退出点击创建模式");
    }
    
    // 确保在关闭窗口时退出创建模式
    private void OnDestroy()
    {
        // 移除事件监听，防止场景视图事件残留
        SceneView.duringSceneGui -= OnSceneGUI;
    }
    
    // 添加选择变化事件监听
    private void OnSelectionChange()
    {
        // 获取当前选中的物体
        GameObject[] selection = Selection.gameObjects;
        
        // 检查是否选中了路径，如果启用了快速删除功能，则显示删除按钮
        if (quickPathDeletionEnabled)
        {
            foreach (GameObject obj in selection)
            {
                MapPath path = obj.GetComponent<MapPath>();
                if (path != null)
                {
                    // 找到了路径，直接处理
                    HandleSelectedPath(path);
                    return;
                }
            }
        }
        
        // 如果快速创建路径功能禁用，则跳过节点处理
        if (!quickPathCreationEnabled && !quickPathDeletionEnabled) return;
        
        // 清除之前的选择
        selectedSceneNodes.Clear();
        
        // 收集所有选中的MapNode
        foreach (GameObject obj in selection)
        {
            MapNode node = obj.GetComponent<MapNode>();
            if (node != null)
            {
                selectedSceneNodes.Add(node);
            }
        }
        
        // 当选中恰好两个节点时
        if (selectedSceneNodes.Count == 2)
        {
            // 设置工具中的节点选择
            selectedNodeA = selectedSceneNodes[0];
            selectedNodeB = selectedSceneNodes[1];
            
            // 根据启用的功能进行操作
            if (quickPathCreationEnabled)
            {
                // 创建路径
                CreatePathBetween(selectedNodeA, selectedNodeB, pathType, controlPointCount);
            }
            
            // 刷新窗口，用于显示操作菜单
            Repaint();
        }
    }
    
    // 处理选中的路径
    private void HandleSelectedPath(MapPath path)
    {
        // 标记为删除状态，在OnGUI中显示删除按钮
        selectedPathToDelete = path;
        Repaint();
    }
    
    // 删除两个节点之间的所有路径
    private void DeletePathsBetweenNodes(MapNode nodeA, MapNode nodeB)
    {
        // 查找所有连接这两个节点的路径
        List<MapPath> pathsToDelete = new List<MapPath>();
        MapPath[] allPaths = FindObjectsOfType<MapPath>();
        
        foreach (MapPath path in allPaths)
        {
            if ((path.nodeA == nodeA && path.nodeB == nodeB) ||
                (path.nodeA == nodeB && path.nodeB == nodeA))
            {
                pathsToDelete.Add(path);
            }
        }
        
        // 删除找到的路径
        if (pathsToDelete.Count > 0)
        {
            foreach (MapPath path in pathsToDelete)
            {
                DestroyImmediate(path.gameObject);
            }
            
            Debug.Log($"已删除 {pathsToDelete.Count} 条连接 {nodeA.nodeName} 和 {nodeB.nodeName} 的路径");
        }
        else
        {
            Debug.Log($"未找到连接 {nodeA.nodeName} 和 {nodeB.nodeName} 的路径");
        }
    }

    // 为编辑器添加快捷键
    [MenuItem("Tools/Map Creator/Delete Selected Path _#d")]
    static void DeleteSelectedPath()
    {
        // 获取选中的对象
        GameObject selected = Selection.activeGameObject;
        if (selected != null)
        {
            MapPath path = selected.GetComponent<MapPath>();
            if (path != null)
            {
                DestroyImmediate(path.gameObject);
            }
        }
    }
    
    // 添加删除节点及相连路径的菜单项
    [MenuItem("Tools/Map Creator/Delete Node With Connected Paths _#n")]
    static void DeleteSelectedNodeWithPaths()
    {
        // 获取选中的对象
        GameObject selected = Selection.activeGameObject;
        if (selected != null)
        {
            MapNode node = selected.GetComponent<MapNode>();
            if (node != null)
            {
                MapCreatorTool window = GetWindow<MapCreatorTool>();
                window.DeleteNodeWithConnectedPaths(node);
            }
        }
    }
    
    // 添加清理非法路径的菜单项
    [MenuItem("Tools/Map Creator/Clean Invalid Paths _#i")]
    static void CleanInvalidPathsMenu()
    {
        MapCreatorTool window = GetWindow<MapCreatorTool>();
        window.CleanInvalidPaths();
    }
    
    // 验证删除节点菜单项是否可用
    [MenuItem("Tools/Map Creator/Delete Node With Connected Paths _#n", true)]
    static bool ValidateDeleteSelectedNodeWithPaths()
    {
        // 仅当选中的是MapNode时该菜单项才可用
        GameObject selected = Selection.activeGameObject;
        return selected != null && selected.GetComponent<MapNode>() != null;
    }
    
    // 验证删除路径菜单项是否可用
    [MenuItem("Tools/Map Creator/Delete Selected Path _#d", true)]
    static bool ValidateDeleteSelectedPath()
    {
        // 仅当选中的是MapPath时该菜单项才可用
        GameObject selected = Selection.activeGameObject;
        return selected != null && selected.GetComponent<MapPath>() != null;
    }
    
    // 删除节点和与其相连的所有路径
    private void DeleteNodeWithConnectedPaths(MapNode node)
    {
        if (node == null) return;
        
        // 查找所有与该节点相连的路径
        List<MapPath> connectedPaths = FindPathsConnectedToNode(node);
        
        // 删除找到的路径
        foreach (MapPath path in connectedPaths)
        {
            DestroyImmediate(path.gameObject);
        }
        
        // 最后删除节点本身
        DestroyImmediate(node.gameObject);
        
        Debug.Log($"已删除节点 {node.nodeName} 及与其相连的 {connectedPaths.Count} 条路径");
        
        // 刷新节点列表
        RefreshNodeList();
    }
    
    // 查找与指定节点相连的所有路径
    private List<MapPath> FindPathsConnectedToNode(MapNode node)
    {
        List<MapPath> connectedPaths = new List<MapPath>();
        MapPath[] allPaths = FindObjectsOfType<MapPath>();
        
        foreach (MapPath path in allPaths)
        {
            if (path.nodeA == node || path.nodeB == node)
            {
                connectedPaths.Add(path);
            }
        }
        
        return connectedPaths;
    }
    
    // 清理所有非法路径（至少一端未连接到节点的路径）
    private void CleanInvalidPaths()
    {
        MapPath[] allPaths = FindObjectsOfType<MapPath>();
        int removedCount = 0;
        
        foreach (MapPath path in allPaths)
        {
            if (path.nodeA == null || path.nodeB == null)
            {
                DestroyImmediate(path.gameObject);
                removedCount++;
            }
        }
        
        if (removedCount > 0)
        {
            Debug.Log($"已删除 {removedCount} 条非法路径");
        }
        else
        {
            Debug.Log("未发现非法路径");
        }
    }
    
    // 监听Unity的对象删除
    [InitializeOnLoadMethod]
    private static void RegisterEditorDeleteCallback()
    {
        // 添加对象删除事件的监听
        EditorApplication.hierarchyChanged += OnHierarchyChanged;
    }
    
    private static void OnHierarchyChanged()
    {
        // 由于该方法会频繁调用，使用一个标记来判断是否刚刚删除了节点
        if (Event.current != null && Event.current.commandName == "Delete")
        {
            // 在下一帧检查是否有节点被删除
            EditorApplication.delayCall += CheckForDeletedNodes;
        }
    }
    
    private static List<MapNode> previousNodes = new List<MapNode>();
    
    private static void CheckForDeletedNodes()
    {
        // 获取当前所有节点
        MapNode[] currentNodes = Object.FindObjectsOfType<MapNode>();
        List<MapNode> newNodesList = new List<MapNode>(currentNodes);
        
        // 如果是首次运行，就记录当前节点并返回
        if (previousNodes.Count == 0)
        {
            previousNodes = newNodesList;
            return;
        }
        
        // 找出被删除的节点
        List<MapNode> deletedNodes = new List<MapNode>();
        foreach (MapNode prevNode in previousNodes)
        {
            if (!newNodesList.Contains(prevNode))
            {
                deletedNodes.Add(prevNode);
            }
        }
        
        // 更新节点列表
        previousNodes = newNodesList;
        
        // 如果有节点被删除，则删除与之相连的路径
        if (deletedNodes.Count > 0)
        {
            MapPath[] allPaths = Object.FindObjectsOfType<MapPath>();
            List<MapPath> pathsToDelete = new List<MapPath>();
            
            // 找出所有与被删除节点相连的路径
            foreach (MapPath path in allPaths)
            {
                foreach (MapNode deletedNode in deletedNodes)
                {
                    if (path.nodeA == deletedNode || path.nodeB == deletedNode)
                    {
                        if (!pathsToDelete.Contains(path))
                        {
                            pathsToDelete.Add(path);
                        }
                    }
                }
            }
            
            // 删除这些路径
            foreach (MapPath path in pathsToDelete)
            {
                Object.DestroyImmediate(path.gameObject);
            }
            
            if (pathsToDelete.Count > 0)
            {
                Debug.Log($"自动清理：删除了与被删除节点相连的 {pathsToDelete.Count} 条路径");
            }
        }
    }
} 