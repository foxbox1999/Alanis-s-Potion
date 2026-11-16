using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Threading.Tasks;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class UIFaceCustomizer : BaseUIView
{
    #region 组件
    [Header("角色显示组件")]
    public Image EyebrowsDisplay;
    public Image EyesDisplay;
    public Image MouthDisplay;
    public Image BackHairDisplay;
    public Image FrontHairDisplay;

    [Header("分类标签")]
    public Button EyebrowsTab;
    public Button EyesTab;
    public Button MouthTab;
    public Button BackHairTab;
    public Button FrontHairTab;

    [Header("素材容器")]
    public Transform materialContent;
    public Transform ColorMaterialContent;

    [Header("按钮预制体")]
    public GameObject materialButtonPrefab;
    public GameObject ColorMaterialButtonPrefab;

    [Header("确认按钮")]
    public Button confirmButton;

    [Header("颜色下拉菜单 - 拖拽GameObject")]
    public Button hairColorButton;
    public Button eyeColorButton;
    public Transform hairColorDropdownContent;
    public Transform eyeColorDropdownContent;

    [Header("素材资源 - 按类型和颜色组织")]
    private List<FacePartData> eyebrowsData;
    private List<FacePartData> eyesData;
    private List<FacePartData> backHairData;
    private List<FacePartData> frontHairData;
    private List<FacePartData> mouthData;

    private List<FacePartData> eyebrowsDataToShow;
    private List<FacePartData> eyesDataToShow;
    private List<FacePartData> backHairDataToShow;
    private List<FacePartData> frontHairDataToShow;
    private List<FacePartData> mouthDataToShow;
    #endregion

    #region 配置
    [System.Serializable]
    public class FacePartData
    {
        public string typeName;
        public List<Sprite> colorVariants;
    }

    // 私有变量
    private string currentCategory;
    private Dictionary<string, Sprite> currentSelections = new Dictionary<string, Sprite>();

    private Dictionary<string, int> currentTypeIndex = new Dictionary<string, int>();
    private Dictionary<string, int> currentColorIndex = new Dictionary<string, int>();


    private int currentGlobalHairColorIndex = 0;
    private int currentGlobalEyeColorIndex = 0;


    [Header("素材文件夹路径")]
    private IEventBus _eventBus;

    #endregion

    #region 初始化
    protected override void OnOpen()
    {
        base.OnOpen();
        Initialize();
    }
    public async void Initialize()
    {
        await InitializeAsync();
    }
    public async Task InitializeAsync()
    {
        if (_eventBus == null)
            _eventBus = EventBus.Instance;

        // ① 先等待资源加载（✅ 用 await，不阻塞主线程）
        await LoadAllFaceSprites();

        // ② 资源加载完成后再初始化 UI
        SetupColorAndStyle();
        SetupCategoryTabs();
        InitColorChooseButton();
        SwitchToCategory("Eyebrows");

        confirmButton.onClick.RemoveAllListeners();
        confirmButton.onClick.AddListener(() => ConfirmSelection());
    }
    private void SetupColorAndStyle()
    {
        //应该先从SaveManager里面获取当前的内容，反正默认情况都是00
        Dictionary<string, string> part1s = PlayerDataManager.Instance?.GetAllPartIdentifiers();

        if (part1s != null)
        {
            // 初始化默认选择和索引
            Dictionary<string, string> currentParts = part1s;
            foreach (var item in currentParts)
            {
                string[] parts = item.Value.Split('_');
                if (parts.Length == 3)
                {
                    //获取所有的类型（确保了样式的正确
                    currentTypeIndex[item.Key] = (parts.Length >= 3 && int.TryParse(parts[1], out int type)) ? type : 0;
                    if (item.Key == "BackHair" || item.Key == "Eyes")
                    {
                        //确保了颜色的统一
                        currentColorIndex[item.Key] = (parts.Length >= 3 && int.TryParse(parts[2], out int color)) ? color : 0;
                    }
                }
            }
            // 应用初始颜色
            OnHairColorChanged(currentColorIndex["BackHair"]);
            OnEyeColorChanged(currentColorIndex["Eyes"]);
        }
        else
        {
            LogWarning("存档为空，请检查");
            SetAllTypeDefault();
            OnHairColorChanged(0);
            OnEyeColorChanged(0);
        }
    }

    //只有进捏脸的时候读取不到存档才会调用
    private void SetAllTypeDefault()
    {
        // 初始化默认选择和索引
        string[] categories = { "Eyebrows", "Eyes", "Mouth", "BackHair", "FrontHair" };

        foreach (string category in categories)
        {
            currentTypeIndex[category] = 0; // 默认选择第一种类型
        }
    }

    void SetupCategoryTabs()
    {
        //先移除再添加，确保唯一
        EyebrowsTab.onClick.RemoveAllListeners();
        EyesTab.onClick.RemoveAllListeners();
        MouthTab.onClick.RemoveAllListeners();
        BackHairTab.onClick.RemoveAllListeners();
        FrontHairTab.onClick.RemoveAllListeners();

        EyebrowsTab.onClick.AddListener(() => SwitchToCategory("Eyebrows"));
        EyesTab.onClick.AddListener(() => SwitchToCategory("Eyes"));
        MouthTab.onClick.AddListener(() => SwitchToCategory("Mouth"));
        BackHairTab.onClick.AddListener(() => SwitchToCategory("BackHair"));
        FrontHairTab.onClick.AddListener(() => SwitchToCategory("FrontHair"));

    }
    #endregion

    #region 生命周期
    private void Start()
    {

    }
    public void UpdataUI()
    {

    }
    public bool onBackButton()
    {
        return false;
    }
    public void RemoveUI()
    {
        //清理缓存
    }

    #endregion

    #region 预加载策略

    //加载所有的用于显示立绘和用于显示素材的图片
    /// <summary>
    /// 从指定文件夹加载所有脸部素材
    /// </summary>
    private  async Task LoadAllFaceSprites()
    {
        // 单次加载所有资源（性能优化：避免5次LoadAll）
        Sprite[] allSprites = await AssetManager.Instance.LoadAllAssetsAsync<Sprite>(ResourcePaths.FaceSculptingDir);

        if (allSprites == null || allSprites.Length == 0)
        {
            LogError($"路径 '{ResourcePaths.FaceSculptingDir}' 未找到任何Sprite资源！");
            return;
        }

        Sprite[] allMaterials = await AssetManager.Instance.LoadAllAssetsAsync<Sprite>(ResourcePaths.FaceSculptingToChooseDir);
        if (allMaterials == null || allMaterials.Length == 0)
        {
            LogError($"路径 '{ResourcePaths.FaceSculptingToChooseDir}' 未找到任何Sprite资源！");
            return;
        }
        // 按类别过滤并构建数据
        eyebrowsData = FilterAndGroupSprites(allSprites, "Eyebrows");
        eyesData = FilterAndGroupSprites(allSprites, "Eyes");
        backHairData = FilterAndGroupSprites(allSprites, "BackHair");
        frontHairData = FilterAndGroupSprites(allSprites, "FrontHair");
        mouthData = FilterAndGroupSprites(allSprites, "Mouth");

        eyebrowsDataToShow = FilterAndGroupSprites(allMaterials, "Eyebrows");
        eyesDataToShow = FilterAndGroupSprites(allMaterials, "Eyes");
        backHairDataToShow = FilterAndGroupSprites(allMaterials, "BackHair");
        frontHairDataToShow = FilterAndGroupSprites(allMaterials, "FrontHair");
        mouthDataToShow = FilterAndGroupSprites(allMaterials, "Mouth");
    }

    /// <summary>
    /// 从混合Sprite数组中过滤并分组指定类别的资源
    /// </summary>
    private List<FacePartData> FilterAndGroupSprites(Sprite[] allSprites, string category)
    {
        List<FacePartData> partDataList = new List<FacePartData>();

        if (allSprites == null || allSprites.Length == 0)
        {
            Debug.LogWarning($"[捏脸系统] 资源数组为空，跳过 {category} 加载");
            return partDataList;
        }

        // 字典：key=样式索引(字符串), value=该样式的颜色变体列表
        Dictionary<string, List<Sprite>> styleGroups = new Dictionary<string, List<Sprite>>();

        // ===== 1. 精准过滤 + 分组 =====
        foreach (Sprite sprite in allSprites)
        {
            // 【关键】双重验证避免误匹配 (例: "FrontHair" 不匹配 "FrontHairExtra")
            if (!sprite.name.StartsWith(category + "_") || string.IsNullOrEmpty(sprite?.name)) continue;

            string[] parts = sprite.name.Split('_');
            // 验证: 部位名匹配 + 格式正确 (至少3段)
            if (parts.Length < 3 || parts[0] != category) continue;

            string styleIndex = parts[1]; // 样式索引 (如 "1")

            // 创建/获取该样式的颜色列表
            if (!styleGroups.ContainsKey(styleIndex))
                styleGroups[styleIndex] = new List<Sprite>();
            //categroy的styleIndex添加图片
            styleGroups[styleIndex].Add(sprite);
        }

        // ===== 2. 构建FacePartData并排序 =====
        //对categroy的所有类别
        foreach (var group in styleGroups)
        {
            //获取到了categroyd的某一个样式中的图片列表
            string styleIndex = group.Key;
            List<Sprite> colorSprites = group.Value;

            // 按颜色索引排序 (从文件名第三段提取)
            colorSprites.Sort((a, b) =>
            {
                int aColor = ExtractColorIndex(a.name);
                int bColor = ExtractColorIndex(b.name);
                //利用CompareTo，大的在后，小的在前，等于的不动
                return aColor.CompareTo(bColor);
            });

            // 构建数据对象,将颜色排序好的样式放入列表
            partDataList.Add(new FacePartData
            {
                typeName = $"{category} 类型{styleIndex}", // 保持原格式
                colorVariants = colorSprites
            });
        }

        // 按样式索引数值排序 (避免"10"排在"2"前面)
        partDataList.Sort((a, b) =>
        {
            int aStyle = ExtractStyleIndex(a.typeName);
            int bStyle = ExtractStyleIndex(b.typeName);
            return aStyle.CompareTo(bStyle);
        });
        string partToShow = partDataList.Count > 0 ? partDataList[0].typeName : "无";
    
        return partDataList;
    }

    #endregion

    #region 核心方法

    #region 素材面板控制

    //================标签按钮显示======================
    public void SwitchToCategory(string category)
    {
        currentCategory = category;
        UpdateButtonColor(category);
        ClearMaterialButtons();
        ShowMaterialButtons(category);
    }

    void UpdateButtonColor(string selectedCategory)
    {
        Button selectedButton = GetButtonByCategory(selectedCategory);
        if (selectedButton == null) return;

        ResetAllButtonColor();

        SetButtonSelectedAppearance(selectedButton);
    }

    private void ResetAllButtonColor()
    {
        Button[] buttons = { EyebrowsTab, EyesTab, MouthTab, BackHairTab, FrontHairTab };
        foreach (Button button in buttons)
        {
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.9433962f, 0.8410466f, 0.8410466f);
            button.colors = colors;
        }
    }

    void SetButtonSelectedAppearance(Button button)
    {
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.8490566f, 0.8490566f, 0.8490566f);
        colors.selectedColor = new Color(0.8490566f, 0.8490566f, 0.8490566f);
        colors.pressedColor = new Color(0.8490566f, 0.8490566f, 0.8490566f);
        colors.highlightedColor = new Color(0.9433962f, 0.8410466f, 0.8410466f);
        button.colors = colors;
    }

    //==================素材面板控制=================
    void ClearMaterialButtons()
    {
        foreach (Transform child in materialContent)
        {
            Destroy(child.gameObject);
        }
    }
    void ShowMaterialButtons(string category)
    {
        //显示当前类别的所有类型
        List<FacePartData> partData = GetPartDataByCategory(category);
        if (partData != null && category == "Mouth")
        {
           
        }
        for (int typeIndex = 0; typeIndex < partData.Count; typeIndex++)
        {
            updateMaterialButton(category, typeIndex);
        }
    }

    void updateMaterialButton(string category, int typeIndex)
    {
        //将预制件挂载到父容器下
        GameObject buttonObj = Instantiate(materialButtonPrefab, materialContent);

        Button button = buttonObj.GetComponent<Button>();
        Image image = ToolMethods.GetComponentInStrictChildren<Image>(buttonObj);

        if (category == "Mouth")
        {
            //嘴巴没有color的控制
            image.sprite = GetSpriteShowByIndex(category, typeIndex, 0);
        }
        else
        {
            // 显示该类型的默认颜色变体
            image.sprite = GetSpriteShowByIndex(category, typeIndex, currentColorIndex[category]);
        }

        button.onClick.AddListener(() => OnMaterialSelected(category, typeIndex));
    }



    #endregion

    #region 颜色控制
    //初始化毛发和眼睛颜色控制按钮的功能
    private void InitColorChooseButton()
    {
        if (hairColorButton != null && eyeColorButton != null)
        {
            hairColorButton.onClick.RemoveAllListeners();
            eyeColorButton.onClick.RemoveAllListeners();
            hairColorButton.onClick.AddListener(() => UpdateColorButton("Hair"));
            eyeColorButton.onClick.AddListener(() => UpdateColorButton("Eyes"));
            UpdateColorButton("Hair");
        }
    }

    //初始化按钮功能
    private void UpdateColorButton(string categroy)
    {
        ClearColorMaterialButtons();
        //将该按钮置为前
        switch (categroy)
        {
            case "Hair":
                hairColorDropdownContent.SetAsLastSibling();
                break;
            case "Eyes":
                eyeColorDropdownContent.SetAsLastSibling();
                break;
        }
        if (categroy != null)
        {
            SetAllColorButton(categroy);
        }
    }
    void ClearColorMaterialButtons()
    {
        foreach (Transform child in ColorMaterialContent)
        {
            Destroy(child.gameObject);
        }
    }

    //根据类别将按钮添加到容器内
    private void SetAllColorButton(string categroy)
    {
        if (categroy == "Hair")
        {
            Sprite[] HairSprites = Resources.LoadAll<Sprite>(ResourcePaths.HairColorDir);
            //根据图片的数量生成按钮并挂在容器下
            CreatColorButton(HairSprites, categroy);
        }
        else if (categroy == "Eyes")
        {
            Sprite[] EyesSprites = Resources.LoadAll<Sprite>(ResourcePaths.EyeColorDir);
            CreatColorButton(EyesSprites, categroy);
        }
        else
        {
            LogWarning("错误的颜色");
        }
    }

    private void CreatColorButton(Sprite[] sprites, string categroy)
    {
        foreach (Sprite sprite in sprites)
        {
            string[] parts = sprite.name.Split('_');

            int index = (parts.Length >= 2 && int.TryParse(parts[1], out int color)) ? color : 0;
            GameObject colorButton = Instantiate(ColorMaterialButtonPrefab, ColorMaterialContent);
            colorButton.SetActive(true);
            Button button = colorButton.GetComponent<Button>();
            Image image = colorButton.GetComponent<Image>();
            if (image != null && button != null)
            {
                image.sprite = sprite;
            }
            button.onClick.RemoveAllListeners();
            if (categroy == parts[0])
            {
                button.onClick.AddListener(() => OnColorChoosed(categroy, index));
            }
            else
            {
                LogWarning("文件名与颜色类别不匹配");
            }
        }
    }

    private void OnColorChoosed(string categroy, int index)
    {
        if (categroy == "Hair")
        {
            OnHairColorChanged(index);
        }
        else if (categroy == "Eyes")
        {
            OnEyeColorChanged(index);
        }
        else
        {
            LogWarning("错误的颜色");
        }
    }
    //头发颜色改变
    void OnHairColorChanged(int colorIndex)
    {
        currentGlobalHairColorIndex = colorIndex;
        ApplyHairColorToAllParts();
        if (currentCategory != "Mouth" || currentCategory != "Eyes")
        {
            ClearMaterialButtons();
            ShowMaterialButtons(currentCategory);

        }

    }
    //眼睛颜色改变
    void OnEyeColorChanged(int colorIndex)
    {
        currentGlobalEyeColorIndex = colorIndex;
        ApplyEyeColorToAllParts();
        if (currentCategory == "Eyes")
        {
            ClearMaterialButtons();
            ShowMaterialButtons(currentCategory);

        }
    }
    #endregion

    #region 样式控制
    void OnMaterialSelected(string category, int typeIndex)
    {
        // 根据当前全局颜色选择来确定使用哪种颜色变体
        int colorIndex = 0;

        if (category == "Eyebrows" || category == "BackHair" || category == "FrontHair" || category == "BaseHair")
        {
            colorIndex = currentGlobalHairColorIndex;
        }
        else if (category == "Eyes")
        {
            colorIndex = currentGlobalEyeColorIndex;
        }
        // 鼻子没有颜色变体，使用默认颜色

        UpdatePartDisplay(category, typeIndex, colorIndex);
    }
    #endregion

    #region 总体显示

    void ApplyHairColorToAllParts()
    {
        // 更新所有毛发相关部位
        if (currentTypeIndex.ContainsKey("Eyebrows"))
        {
            UpdatePartDisplay("Eyebrows", currentTypeIndex["Eyebrows"], currentGlobalHairColorIndex);
        }

        if (currentTypeIndex.ContainsKey("BackHair"))
        {
            UpdatePartDisplay("BackHair", currentTypeIndex["BackHair"], currentGlobalHairColorIndex);
        }

        if (currentTypeIndex.ContainsKey("FrontHair"))
        {
            UpdatePartDisplay("FrontHair", currentTypeIndex["FrontHair"], currentGlobalHairColorIndex);
        }
    }

    void ApplyEyeColorToAllParts()
    {
        // 更新眼睛
        if (currentTypeIndex.ContainsKey("Eyes"))
        {
            UpdatePartDisplay("Eyes", currentTypeIndex["Eyes"], currentGlobalEyeColorIndex);
        }
    }
    //更新指定的类别的显示，根据输入的内容
    void UpdatePartDisplay(string category, int typeIndex, int colorIndex)
    {

        Sprite selectedSprite = GetSpriteByIndex(category, typeIndex, colorIndex);

        if (selectedSprite != null)
        {
            // 更新显示
            switch (category)
            {
                case "Eyebrows":
                    EyebrowsDisplay.sprite = selectedSprite;
                    break;
                case "Eyes":
                    EyesDisplay.sprite = selectedSprite;
                    break;
                case "Mouth":
                    MouthDisplay.sprite = selectedSprite;
                    break;
                case "BackHair":
                    BackHairDisplay.sprite = selectedSprite;
                    break;
                case "FrontHair":
                    FrontHairDisplay.sprite = selectedSprite;
                    break;
            }

            //更新当前选择
            currentSelections[category] = selectedSprite;
            currentTypeIndex[category] = typeIndex;
            currentColorIndex[category] = colorIndex;

        }
    }


    #endregion

    #region 确认按钮控制
    public void ConfirmSelection()
    {
        Debug.Log("确认捏脸方案");
        UpdateCharacterDisplay();
        Debug.Log("确认捏脸方案");
        PopupManager.Instance.ShowConfirm("捏脸确认", "请确认是否采用这个捏脸方案", SaveFaceDate);
        Debug.Log("确认捏脸方案");
    }

    void UpdateCharacterDisplay()
    {
        // 确保所有部位都使用当前的颜色设置
        ApplyHairColorToAllParts();
        ApplyEyeColorToAllParts();
    }

    //获取当前已经渲染的组件中的各个部分的image名字并保存
    public void SaveFaceDate()
    {
        //发布捏脸结束事件
        string _Eyebrows = EyebrowsDisplay.sprite.name;
        string _Eyes = EyesDisplay.sprite.name;
        string _Mouth = MouthDisplay.sprite.name;
        string _BackHair = BackHairDisplay.sprite.name;
        string _FrontHair = FrontHairDisplay.sprite.name;
        Dictionary<string, string> _parts = new Dictionary<string, string>();
        _parts.Add("BackHair", _BackHair);
        _parts.Add("Eyes", _Eyes);
        _parts.Add("Eyebrows", _Eyebrows);
        _parts.Add("FrontHair", _FrontHair);
        _parts.Add("Mouth", _Mouth);

        EventBus.Instance.Publish(new OnAvatarCustomizationOver
        {
            _parts = _parts
        });
       
    }
    #endregion

    #endregion

    #region 工具方法
    /// <summary>
    /// 从Sprite名称提取颜色索引 (例: "FrontHair_1_3" → 3)
    /// </summary>
    private int ExtractColorIndex(string spriteName)
    {
        string[] parts = spriteName.Split('_');
        return (parts.Length >= 3 && int.TryParse(parts[2], out int color)) ? color : 0;
    }

    /// <summary>
    /// 从typeName提取样式索引 (例: "FrontHair 类型2" → 2)
    /// </summary>
    private int ExtractStyleIndex(string typeName)
    {
        // 安全提取: 查找"类型"后的数字
        int startIndex = typeName.IndexOf("类型");
        if (startIndex >= 0 && int.TryParse(typeName.Substring(startIndex + 2), out int style))
            return style;
        return 0;
    }

    //获取不同部位的数据缓存
    List<FacePartData> GetPartDataByCategory(string category)
    {
        switch (category)
        {
            case "Eyebrows": return eyebrowsData;
            case "Eyes": return eyesData;
            case "BackHair": return backHairData;
            case "FrontHair": return frontHairData;
            case "Mouth": return mouthData; // 新增底发数据
            default: return new List<FacePartData>();
        }
    }

    List<FacePartData> GetPartShowDataByCategory(string category)
    {
        switch (category)
        {
            case "Eyebrows": return eyebrowsDataToShow;
            case "Eyes": return eyesDataToShow;
            case "BackHair": return backHairDataToShow;
            case "FrontHair": return frontHairDataToShow;
            case "Mouth": return mouthDataToShow;
            default: return new List<FacePartData>();
        }
    }

    //根据部位，样式索引以及颜色索引在某个部位的数据缓存中获取对应的图片
    Sprite GetSpriteShowByIndex(string category, int typeIndex, int colorIndex)
    {
        List<FacePartData> partData = GetPartShowDataByCategory(category);

        if (partData.Count > typeIndex && partData[typeIndex].colorVariants.Count > colorIndex)
        {
            return partData[typeIndex].colorVariants[colorIndex];
        }

        return null;
    }

    //根据部位，样式索引以及颜色索引在某个部位的数据缓存中获取对应的图片
    Sprite GetSpriteByIndex(string category, int typeIndex, int colorIndex)
    {
        List<FacePartData> partData = GetPartDataByCategory(category);

        if (partData.Count > typeIndex && partData[typeIndex].colorVariants.Count > colorIndex)
        {
            return partData[typeIndex].colorVariants[colorIndex];
        }
        return null;
    }
    Button GetButtonByCategory(string category)
    {
        switch (category)
        {
            case "Eyebrows": return EyebrowsTab;
            case "Eyes": return EyesTab;
            case "Mouth": return MouthTab;
            case "BackHair": return BackHairTab;
            case "FrontHair": return FrontHairTab;
            default: return null;
        }
    }
    #endregion

    #region 日志输出
    public void Log(string message)
    {
        Debug.Log($"[UIFaceCustomizer] : {message}");
    }

    public void LogWarning(string message)
    {
        Debug.LogWarning($"[UIFaceCustomizer] : {message}");
    }

    public void LogError(string message)
    {
        Debug.LogError($"[UIFaceCustomizer] : {message}");
    }
    #endregion






}