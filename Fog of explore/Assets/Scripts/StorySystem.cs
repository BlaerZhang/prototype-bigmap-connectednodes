using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class StorySystem : MonoBehaviour
{
    [Header("故事节点设置")]
    [Range(0f, 1f)]
    public float storyNodeProbability = 0.3f; // 节点成为故事节点的概率
    public float playerDetectionRadius = 3f; // 玩家检测半径
    [Tooltip("这些节点不会被转换为故事节点")]
    public List<MapNode> excludedNodes = new List<MapNode>(); // 排除的节点列表

    [Header("视觉效果")]
    public List<Sprite> storyNodeSprites; // 故事节点的可能Sprite集合
    
    [Header("故事文本设置")]
    public TMP_Text storyTextPrefab; // TextMeshPro文本预制体
    [TextArea(3, 10)]
    public List<string> storyTextList = new List<string>(); // 故事文本列表
    
    // 私有变量
    private List<MapNode> allMapNodes = new List<MapNode>(); // 地图上所有的节点
    private List<StoryNodeData> storyNodes = new List<StoryNodeData>(); // 被选中的故事节点
    private Transform playerTransform; // 玩家的Transform
    private bool isInitialized = false;
    
    // 故事节点数据结构
    private class StoryNodeData
    {
        public MapNode node;
        public string storyText;
        public TMP_Text textObject;
        public bool hasBeenRead = false;
        
        public StoryNodeData(MapNode node, string text)
        {
            this.node = node;
            this.storyText = text;
            this.textObject = null;
        }
    }
    
    private void Start()
    {
        // 初始化故事文本
        InitializeStoryTexts();
        
        // 延迟初始化，确保所有地图节点已经加载
        Invoke("Initialize", 0.1f);
    }
    
    // 初始化故事文本列表
    private void InitializeStoryTexts()
    {
        storyTextList.Clear();
        
        // 添加50句独立的故事文本
        storyTextList.Add("这片土地上最后一场雨已经是三十年前的事了，老人们仍然记得雨滴的味道。");
        storyTextList.Add("巨大的金属残骸从沙丘中露出一角，那是古老文明的遗物，没人敢靠近。");
        storyTextList.Add("雾霭中飘浮着微小的发光粒子，人们称它们为'记忆的碎片'。");
        storyTextList.Add("城市边缘的防护罩每晚都会闪烁几次，工程师们说这是正常现象。");
        storyTextList.Add("有传言说北方的荒原上出现了新的绿色植被，但没人能够证实。");
        storyTextList.Add("那个收集旧世界书籍的商人又来了，他的背包里装满了无人能读懂的知识。");
        storyTextList.Add("地下避难所A-7的大门上布满了神秘符号，据说是最后一批科学家留下的。");
        storyTextList.Add("夜晚，天空中偶尔会出现奇怪的光线，老人们说那是卫星的残骸在燃烧。");
        storyTextList.Add("这片区域的磁场异常强烈，指南针在这里毫无用处，人们靠星星导航。");
        storyTextList.Add("那个能预知危险的盲眼女孩又做了噩梦，村庄里的人都紧张起来。");
        storyTextList.Add("传说中的'记忆保管员'住在城市最高的塔楼里，他保存着旧世界的记忆。");
        storyTextList.Add("沙漠中的商队发现了一片金属森林，所有的'树'都指向同一个方向。");
        storyTextList.Add("没人知道那些夜间发光的植物是从何而来，但它们似乎在慢慢蔓延。");
        storyTextList.Add("废墟中的自动机器仍在工作，执行着几个世纪前设定的命令。");
        storyTextList.Add("人们在干涸的湖底发现了一艘巨大的船，上面刻满了无人能解读的文字。");
        storyTextList.Add("城市的水源越来越少，议会宣布将启动神秘的'深井计划'。");
        storyTextList.Add("那个能与动物交流的老萨满说，鸟类已经开始往南方迁徙，这是个不祥之兆。");
        storyTextList.Add("东部山脉的夜晚能听到奇怪的机械运转声，没人敢去调查声音的来源。");
        storyTextList.Add("孩子们发明了一种新游戏，他们称之为'寻找过去'，大人们对此感到不安。");
        storyTextList.Add("遗迹守护者声称在古老的电脑中发现了新的数据，可能与大崩溃有关。");
        storyTextList.Add("边境哨站的士兵报告说，有奇怪的光点在禁区上空盘旋了整整一夜。");
        storyTextList.Add("那个收集旧世界货币的商人付出了高价，买走了一枚印有鹰徽的金属硬币。");
        storyTextList.Add("一支探险队从北方废土归来，带回了一种会自我修复的奇怪材料。");
        storyTextList.Add("古老的广播塔偶尔会播放无人能懂的密码信息，有人认为那是求救信号。");
        storyTextList.Add("城市中心的巨大时钟停在了特定的时刻，没人敢去修复它。");
        storyTextList.Add("据说有一种罕见的变异植物能净化受污染的水源，寻找它的人越来越多。");
        storyTextList.Add("地图绘制者协会宣称发现了一块'移动的土地'，但没人相信这种荒谬的说法。");
        storyTextList.Add("记录员们在废弃的数据中心找到了一段关于旧世界最后时刻的影像。");
        storyTextList.Add("那个研究古代语言的学者声称，壁画上的符号是对未来的警告，而非对过去的记录。");
        storyTextList.Add("沙尘暴后，人们发现地面上出现了完美的几何图案，仿佛被某种力量刻画。");
        storyTextList.Add("城市的能源供应越来越不稳定，工程师们开始研究那个被禁止的能源方案。");
        storyTextList.Add("有传言说，某些孩子出生时眼睛呈现异常的蓝色，能够看到常人看不见的东西。");
        storyTextList.Add("医者公会的成员在废墟中发现了一种新的药草，它似乎能缓解辐射病的症状。");
        storyTextList.Add("那台古老的无线电设备又一次接收到了来自深空的奇怪信号。");
        storyTextList.Add("守望者报告说，禁区的边界似乎在缩小，仿佛那里的危险正在减弱。");
        storyTextList.Add("考古队在沙漠深处发现了一座完好无损的旧世界建筑，里面的空气异常清新。");
        storyTextList.Add("那个能感知金属的女孩说，地下有一大网络的金属管道，连接着所有的主要聚居地。");
        storyTextList.Add("西部绿洲的水变成了奇怪的蓝色，但测试表明它比以往任何时候都更加纯净。");
        storyTextList.Add("档案管理员声称找到了一份详细记录大灾变前世界的文件，议会立即将其封存。");
        storyTextList.Add("天文学家报告说，夜空中出现了一颗新的明亮天体，它在一个月内逐渐变大。");
        storyTextList.Add("边境巡逻队带回了一块能自主移动的金属碎片，它似乎在寻找其他类似的部件。");
        storyTextList.Add("人们在废弃的实验室中发现了一种能在黑暗中发光的液体，它不会随时间衰减。");
        storyTextList.Add("那个古老的AI核心被意外启动，它问的第一个问题是：'多少年过去了？'");
        storyTextList.Add("学者们正在重建旧世界的知识体系，但关于大崩溃的原因仍然众说纷纭。");
        storyTextList.Add("年长的记忆保管员开始培训新的继任者，他们必须记住旧世界的教训。");
        storyTextList.Add("探险家们在远古数据库中发现了一张完整的地球地图，上面的大陆形状与现在完全不同。");
        storyTextList.Add("那个研究天气的科学家声称，大气层正在慢慢自我修复，有一天雨水会重新降临。");
        storyTextList.Add("城市议会决定启动'阿尔忒弥斯计划'，没人知道这意味着什么，但所有人都感到希望。");
        storyTextList.Add("夜空中的星星比以往任何时候都要明亮，仿佛在向人类诉说一个古老而温柔的故事。");
    }
    
    private void Initialize()
    {
        if (isInitialized) return;
        
        // 获取玩家Transform
        playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (playerTransform == null)
        {
            Debug.LogError("StorySystem: 无法找到Player标签的物体！");
        }
        
        // 收集所有的MapNode
        MapNode[] nodes = FindObjectsOfType<MapNode>();
        allMapNodes.AddRange(nodes);
        
        // 从可用节点列表中移除被排除的节点
        foreach (MapNode excludedNode in excludedNodes)
        {
            if (excludedNode != null && allMapNodes.Contains(excludedNode))
            {
                allMapNodes.Remove(excludedNode);
            }
        }
        
        // 确保文本列表不为空
        if (storyTextList.Count == 0)
        {
            Debug.LogWarning("StorySystem: 故事文本列表为空！");
            return;
        }
        
        // 确保有足够的节点来分配所有故事
        if (allMapNodes.Count < storyTextList.Count)
        {
            Debug.LogWarning($"StorySystem: 地图节点数量({allMapNodes.Count})少于故事文本数量({storyTextList.Count})!");
            return;
        }
        
        // 打乱节点和文本顺序，以确保随机性
        ShuffleList(allMapNodes);
        List<string> shuffledStoryTexts = new List<string>(storyTextList);
        ShuffleList(shuffledStoryTexts);
        
        // 分配故事文本到节点
        for (int i = 0; i < shuffledStoryTexts.Count; i++)
        {
            MapNode node = allMapNodes[i];
            string text = shuffledStoryTexts[i];
            
            // 创建故事节点数据
            StoryNodeData storyNode = new StoryNodeData(node, text);
            storyNodes.Add(storyNode);
            
            // 更改节点的sprite
            SpriteRenderer spriteRenderer = node.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null && storyNodeSprites.Count > 0)
            {
                // 随机选择一个sprite
                Sprite randomSprite = storyNodeSprites[Random.Range(0, storyNodeSprites.Count)];
                spriteRenderer.sprite = randomSprite;
            }
        }
        
        isInitialized = true;
        Debug.Log($"StorySystem: 初始化完成，创建了 {storyNodes.Count} 个故事节点。");
    }
    
    private void Update()
    {
        if (!isInitialized || playerTransform == null) return;
        
        // 检查玩家是否接近任何故事节点
        foreach (StoryNodeData storyNode in storyNodes)
        {
            float distance = Vector2.Distance(playerTransform.position, storyNode.node.transform.position);
            
            // 如果玩家靠近节点并且文本尚未显示
            if (distance <= playerDetectionRadius && storyNode.textObject == null)
            {
                ShowStoryText(storyNode);
            }
            // 如果玩家离开了节点并且文本正在显示
            else if (distance > playerDetectionRadius && storyNode.textObject != null)
            {
                HideStoryText(storyNode);
            }
        }
    }
    
    // 显示故事文本
    private void ShowStoryText(StoryNodeData storyNode)
    {
        // 实例化文本预制体
        if (storyTextPrefab != null)
        {
            TMP_Text textObject = Instantiate(storyTextPrefab, storyNode.node.transform.position + new Vector3(0, 1.5f, 0), Quaternion.identity);
            textObject.text = storyNode.storyText;
            textObject.transform.SetParent(storyNode.node.transform);
            
            storyNode.textObject = textObject;
            storyNode.hasBeenRead = true;
        }
        else
        {
            Debug.LogError("StorySystem: 文本预制体未分配!");
        }
    }
    
    // 隐藏故事文本
    private void HideStoryText(StoryNodeData storyNode)
    {
        if (storyNode.textObject != null)
        {
            Destroy(storyNode.textObject.gameObject);
            storyNode.textObject = null;
        }
    }
    
    // Fisher-Yates 洗牌算法
    private void ShuffleList<T>(List<T> list)
    {
        int n = list.Count;
        for (int i = n - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            T temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
    }
    
    // 辅助方法，用于编辑器可视化
    private void OnDrawGizmosSelected()
    {
        foreach (StoryNodeData storyNode in storyNodes)
        {
            if (storyNode.node != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(storyNode.node.transform.position, playerDetectionRadius);
            }
        }
    }
    
    // 如果需要清理
    private void OnDestroy()
    {
        foreach (StoryNodeData storyNode in storyNodes)
        {
            if (storyNode.textObject != null)
            {
                Destroy(storyNode.textObject.gameObject);
            }
        }
    }
} 