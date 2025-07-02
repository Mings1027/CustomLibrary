using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;
using System.Collections.Generic;

public class CSharpFileGenerator : EditorWindow
{
    private string _codeInput = "";
    private string _filenameInput = "";
    private string _savePath = "Assets";
    private Vector2 _scrollPosition;
    private StatusMessage _status = new StatusMessage();
    private RecentPathsManager _recentPaths;

    private const string WINDOW_TITLE = "C# File Generator";
    private const string MENU_PATH = "Tools/C# File Generator #&c";
    private static readonly Vector2 MIN_WINDOW_SIZE = new Vector2(600, 500);

    private static readonly HashSet<string> UNITY_BASE_CLASSES = new HashSet<string>
    {
        "MonoBehaviour", "MonoBehavior", "ScriptableObject", "EditorWindow",
        "Editor", "PropertyDrawer", "AssetPostprocessor", "ScriptableWizard",
        "PreprocessBuild", "PostProcessBuild"
    };

    [MenuItem(MENU_PATH)]
    public static void ShowWindow()
    {
        var window = GetWindow<CSharpFileGenerator>(WINDOW_TITLE);
        window.minSize = MIN_WINDOW_SIZE;
        window.Initialize();
    }

    private void Initialize()
    {
        _recentPaths = new RecentPathsManager();
        UpdateSavePathFromSelection();
    }

    private void OnEnable()
    {
        _recentPaths ??= new RecentPathsManager();
        Selection.selectionChanged += UpdateSavePathFromSelection;
        UpdateSavePathFromSelection();
    }

    private void OnDisable()
    {
        Selection.selectionChanged -= UpdateSavePathFromSelection;
    }

    private void OnGUI()
    {
        DrawHeader();
        DrawSaveLocationSection();
        DrawFilenameSection();
        DrawCodeInputSection();
        DrawGenerateButton();
        DrawStatusMessage();
    }

    private void DrawHeader()
    {
        EditorGUILayout.LabelField("Unity C# File Generator", EditorStyles.boldLabel);
        DrawUsageGuide();
        EditorGUILayout.Space();
    }

    private void DrawUsageGuide()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("사용방법", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("1. 저장할 위치를 지정하거나 프로젝트 브라우저에서 폴더/파일을 선택", EditorStyles.wordWrappedLabel);
        EditorGUILayout.LabelField("2. 생성할 파일명을 입력 (비워두면 클래스명으로 자동 생성)", EditorStyles.wordWrappedLabel);
        EditorGUILayout.LabelField("3. C# 코드를 입력하고 'Generate C# File' 버튼 클릭", EditorStyles.wordWrappedLabel);

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("주의점", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("• 프로젝트 브라우저에서 폴더 선택 → 해당 폴더 내부에 생성", EditorStyles.wordWrappedLabel);
        EditorGUILayout.LabelField("• 프로젝트 브라우저에서 파일 선택 → 파일과 동일한 위치에 생성", EditorStyles.wordWrappedLabel);
        EditorGUILayout.LabelField(
            "• 프로젝트 브라우저의 상단 주소 네비게이션(Assets > Scenes 이런 식으로 되어있는 곳)으로 선택 시 주소가 재설정되지 않으니 주의",
            EditorStyles.wordWrappedLabel);
        EditorGUILayout.EndVertical();
    }

    private void DrawSaveLocationSection()
    {
        EditorGUILayout.LabelField("Save Location", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        GUI.enabled = false;
        EditorGUILayout.TextField(_savePath);
        GUI.enabled = true;

        if (GUILayout.Button("Browse", GUILayout.Width(60)))
        {
            ShowFolderSelector();
        }

        EditorGUILayout.EndHorizontal();

        DrawRecentPathsDropdown();
        EditorGUILayout.Space();
    }

    private void DrawRecentPathsDropdown()
    {
        var paths = _recentPaths.GetPaths();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Recent:", GUILayout.Width(50));

        if (paths.Count > 0)
        {
            int currentIndex = paths.IndexOf(_savePath);
            if (currentIndex == -1) currentIndex = 0;
        
            EditorGUI.BeginChangeCheck();
            int selected = EditorGUILayout.Popup(currentIndex, paths.ToArray());
            if (EditorGUI.EndChangeCheck())  // 사용자가 실제로 변경했을 때만
            {
                if (selected >= 0 && selected < paths.Count)
                {
                    _savePath = paths[selected];
                }
            }
        }
        else
        {
            EditorGUILayout.Popup(0, new string[] { "No recent paths" });
        }

        EditorGUILayout.EndHorizontal();
    }

    private void DrawFilenameSection()
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Filename (optional):", GUILayout.Width(120));
        _filenameInput = EditorGUILayout.TextField(_filenameInput);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.HelpBox("Leave empty to auto-detect from main class name", MessageType.Info);
        EditorGUILayout.Space();
    }

    private void DrawCodeInputSection()
    {
        EditorGUILayout.LabelField("C# Code:");
        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.ExpandHeight(true));
        _codeInput = EditorGUILayout.TextArea(_codeInput, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();
        EditorGUILayout.Space();
    }

    private void DrawGenerateButton()
    {
        GUI.backgroundColor = new Color(0.3f, 0.7f, 0.3f);
        if (GUILayout.Button("Generate C# File", GUILayout.Height(35)))
        {
            GenerateFile();
        }

        GUI.backgroundColor = Color.white;
    }

    private void DrawStatusMessage()
    {
        if (!_status.HasMessage) return;

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(_status.Message, _status.Type);
    }

    private void ShowFolderSelector()
    {
        ProjectFolderSelector.ShowWindow(_savePath, (selectedPath) =>
        {
            _savePath = selectedPath;
            _recentPaths.Add(_savePath);
            Repaint();
        });
    }

    private void UpdateSavePathFromSelection()
    {
        var selectedPaths = Selection.assetGUIDs
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(p => !string.IsNullOrEmpty(p))
            .ToList();

        if (selectedPaths.Count == 0) return;

        string path = selectedPaths[0];
        string newPath = AssetDatabase.IsValidFolder(path) ? path : GetDirectoryFromPath(path);

        if (_savePath != newPath)
        {
            _savePath = newPath;
            Repaint();
        }
    }

    private string GetDirectoryFromPath(string path)
    {
        string directory = Path.GetDirectoryName(path);
        return string.IsNullOrEmpty(directory) ? _savePath : directory.Replace('\\', '/');
    }

    private void GenerateFile()
    {
        if (!ValidateInput()) return;

        try
        {
            string filename = DetermineFilename();
            if (string.IsNullOrEmpty(filename))
            {
                _status.Show(
                    "Could not determine filename. Please provide a filename or ensure your code contains a valid class.",
                    MessageType.Error);
                return;
            }

            CreateFile(filename);
        }
        catch (System.Exception e)
        {
            _status.Show($"Error: {e.Message}", MessageType.Error);
        }
    }

    private bool ValidateInput()
    {
        if (string.IsNullOrWhiteSpace(_codeInput))
        {
            _status.Show("Please enter some C# code", MessageType.Error);
            return false;
        }

        return true;
    }

    private string DetermineFilename()
    {
        if (!string.IsNullOrWhiteSpace(_filenameInput))
        {
            return FileUtils.SanitizeFilename(_filenameInput);
        }

        var detector = new ClassDetector(_codeInput);
        return detector.GetMainClassName();
    }

    private void CreateFile(string filename)
    {
        filename = FileUtils.EnsureCsExtension(filename);
        string fullPath = Path.Combine(_savePath, filename);

        if (!ConfirmOverwrite(fullPath, filename)) return;

        FileUtils.EnsureDirectoryExists(Path.GetDirectoryName(fullPath));
        File.WriteAllText(fullPath, _codeInput);

        _recentPaths.Add(_savePath);
        AssetDatabase.Refresh();

        _status.Show($"Successfully created: {filename} at {_savePath}", MessageType.Info);
        SelectCreatedFile(fullPath);
    }

    private bool ConfirmOverwrite(string path, string filename)
    {
        if (!File.Exists(path)) return true;

        return EditorUtility.DisplayDialog(
            "File Exists",
            $"The file '{filename}' already exists at:\n{_savePath}\n\nDo you want to overwrite it?",
            "Overwrite",
            "Cancel"
        );
    }

    private void SelectCreatedFile(string path)
    {
        var asset = AssetDatabase.LoadAssetAtPath<Object>(path);
        if (asset == null) return;

        Selection.activeObject = asset;
        EditorGUIUtility.PingObject(asset);
    }

    private class StatusMessage
    {
        public string Message { get; private set; } = "";
        public MessageType Type { get; private set; } = MessageType.None;
        public bool HasMessage => !string.IsNullOrEmpty(Message);

        public void Show(string message, MessageType type)
        {
            Message = message;
            Type = type;
        }
    }

    private class RecentPathsManager
    {
        private const string PREFS_KEY = "CSharpFileGenerator_RecentPaths";
        private const int MAX_PATHS = 3;
        private List<string> _paths;

        public RecentPathsManager()
        {
            Load();
        }

        public List<string> GetPaths() => _paths;

        public void Add(string path)
        {
            _paths.Remove(path);
            _paths.Insert(0, path);

            if (_paths.Count > MAX_PATHS)
            {
                _paths.RemoveRange(MAX_PATHS, _paths.Count - MAX_PATHS);
            }

            Save();
        }

        private void Load()
        {
            string saved = EditorPrefs.GetString(PREFS_KEY, "");
            _paths = string.IsNullOrEmpty(saved)
                ? new List<string>()
                : saved.Split('|').Where(p => !string.IsNullOrEmpty(p)).ToList();
        }

        private void Save()
        {
            EditorPrefs.SetString(PREFS_KEY, string.Join("|", _paths));
        }
    }

    private class ClassDetector
    {
        private readonly string _code;

        private const string CLASS_PATTERN =
            @"(?:public\s+|private\s+|internal\s+|protected\s+)?(?:abstract\s+|sealed\s+|static\s+)?class\s+(\w+)(?:\s*:\s*([\w\s,]+))?";

        public ClassDetector(string code)
        {
            _code = code;
        }

        public string GetMainClassName()
        {
            var classes = ParseClasses();
            if (classes.Count == 0) return null;

            var unityClass = classes.FirstOrDefault(c => c.IsUnityClass);
            return unityClass?.Name ?? classes.OrderByDescending(c => c.LineCount).FirstOrDefault()?.Name;
        }

        private List<ClassInfo> ParseClasses()
        {
            var matches = Regex.Matches(_code, CLASS_PATTERN);
            return matches.Cast<Match>().Select(ParseClassFromMatch).ToList();
        }

        private ClassInfo ParseClassFromMatch(Match match)
        {
            var info = new ClassInfo { Name = match.Groups[1].Value };

            if (match.Groups[2].Success)
            {
                info.IsUnityClass = IsUnityClass(match.Groups[2].Value);
            }

            info.LineCount = CountClassLines(match.Index);
            return info;
        }

        private bool IsUnityClass(string inheritance)
        {
            return UNITY_BASE_CLASSES.Any(inheritance.Contains);
        }

        private int CountClassLines(int classStart)
        {
            var bodyBounds = FindClassBodyBounds(classStart);
            if (!bodyBounds.HasValue) return 0;

            string body = _code.Substring(bodyBounds.Value.start, bodyBounds.Value.end - bodyBounds.Value.start);
            return body.Split('\n').Count(line => !string.IsNullOrWhiteSpace(line));
        }

        private (int start, int end)? FindClassBodyBounds(int classStart)
        {
            int braceCount = 0;
            int bodyStart = -1;

            for (int i = classStart; i < _code.Length; i++)
            {
                if (_code[i] == '{')
                {
                    if (braceCount == 0) bodyStart = i + 1;
                    braceCount++;
                }
                else if (_code[i] == '}')
                {
                    braceCount--;
                    if (braceCount == 0 && bodyStart != -1)
                    {
                        return (bodyStart, i);
                    }
                }
            }

            return null;
        }

        private class ClassInfo
        {
            public string Name { get; set; }
            public bool IsUnityClass { get; set; }
            public int LineCount { get; set; }
        }
    }

    private static class FileUtils
    {
        private static readonly char[] INVALID_CHARS = Path.GetInvalidFileNameChars();

        public static string SanitizeFilename(string filename)
        {
            string sanitized = string.Join("", filename.Split(INVALID_CHARS));
            return sanitized.EndsWith(".cs") ? sanitized.Substring(0, sanitized.Length - 3) : sanitized;
        }

        public static string EnsureCsExtension(string filename)
        {
            return filename.EndsWith(".cs") ? filename : filename + ".cs";
        }

        public static void EnsureDirectoryExists(string directory)
        {
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }
    }
}

// 새로운 프로젝트 폴더 선택기 창
public class ProjectFolderSelector : EditorWindow
{
    private System.Action<string> _onFolderSelected;
    private string _selectedPath;
    private Vector2 _scrollPosition;
    private Dictionary<string, bool> _foldoutStates = new Dictionary<string, bool>();
    private List<FolderNode> _rootFolders;

    private static readonly Vector2 WINDOW_SIZE = new Vector2(400, 500);

    public static void ShowWindow(string currentPath, System.Action<string> onFolderSelected)
    {
        var window = CreateInstance<ProjectFolderSelector>();
        window.titleContent = new GUIContent("Select Folder");
        window._onFolderSelected = onFolderSelected;
        window._selectedPath = currentPath;
        window.position = new Rect(
            (Screen.currentResolution.width - WINDOW_SIZE.x) / 2,
            (Screen.currentResolution.height - WINDOW_SIZE.y) / 2,
            WINDOW_SIZE.x, WINDOW_SIZE.y
        );

        window.BuildFolderTree();
        window.ShowModal();
    }

    private void BuildFolderTree()
    {
        _rootFolders = new List<FolderNode>();

        // Assets 폴더부터 시작
        var assetsNode = new FolderNode("Assets", "Assets");
        BuildFolderNode(assetsNode);
        _rootFolders.Add(assetsNode);

        // 초기 확장 상태 설정
        SetInitialFoldoutStates();
    }

    private void BuildFolderNode(FolderNode node)
    {
        try
        {
            var directories = Directory.GetDirectories(node.FullPath)
                .Where(d => !Path.GetFileName(d).StartsWith(".")) // 숨김 폴더 제외
                .OrderBy(d => Path.GetFileName(d));

            foreach (var dir in directories)
            {
                string folderName = Path.GetFileName(dir);
                string relativePath = dir.Replace(Application.dataPath, "Assets").Replace('\\', '/');

                var childNode = new FolderNode(folderName, relativePath);
                BuildFolderNode(childNode);
                node.Children.Add(childNode);
            }
        }
        catch (System.Exception)
        {
            // 폴더 접근 권한 없음 등의 예외 무시
        }
    }

    private void SetInitialFoldoutStates()
    {
        // 현재 선택된 경로의 상위 폴더들을 모두 확장
        string[] pathParts = _selectedPath.Split('/');
        string currentPath = "";

        foreach (string part in pathParts)
        {
            if (string.IsNullOrEmpty(part)) continue;

            currentPath = string.IsNullOrEmpty(currentPath) ? part : currentPath + "/" + part;
            _foldoutStates[currentPath] = true;
        }
    }

    private void OnGUI()
    {
        // 제목
        GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Select a folder to save the C# file:", titleStyle);

        // 현재 선택된 경로 표시 (유니티 스타일)
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        EditorGUILayout.LabelField("Selected:", EditorStyles.miniLabel, GUILayout.Width(55));
        EditorGUILayout.LabelField(_selectedPath, EditorStyles.miniLabel);
        EditorGUILayout.EndHorizontal();

        // 폴더 트리 영역 (유니티 Project 창과 유사한 배경)
        Rect treeRect = EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUI.DrawRect(treeRect,
            EditorGUIUtility.isProSkin ? new Color(0.22f, 0.22f, 0.22f, 1f) : new Color(0.76f, 0.76f, 0.76f, 1f));

        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUIStyle.none, GUI.skin.verticalScrollbar);

        foreach (var rootFolder in _rootFolders)
        {
            DrawFolderNode(rootFolder, 0);
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();

        // 하단 버튼 영역
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.FlexibleSpace();

        // 유니티 스타일 버튼
        if (GUILayout.Button("Select", EditorStyles.toolbarButton, GUILayout.Width(60)))
        {
            _onFolderSelected?.Invoke(_selectedPath);
            Close();
        }

        if (GUILayout.Button("Cancel", EditorStyles.toolbarButton, GUILayout.Width(60)))
        {
            Close();
        }

        EditorGUILayout.EndHorizontal();
    }

    private void DrawFolderNode(FolderNode node, int depth)
    {
        bool isSelected = node.RelativePath == _selectedPath;
        bool hasFoldout = node.Children.Count > 0;
        bool isFoldedOut = _foldoutStates.ContainsKey(node.RelativePath) ? _foldoutStates[node.RelativePath] : false;

        // 선택된 항목 배경색
        Rect itemRect = EditorGUILayout.BeginHorizontal(GUILayout.Height(16));
        if (isSelected)
        {
            EditorGUI.DrawRect(itemRect, new Color(0.24f, 0.49f, 0.91f, 0.5f)); // Unity 선택 색상
        }
        else if (itemRect.Contains(Event.current.mousePosition))
        {
            EditorGUI.DrawRect(itemRect, new Color(1f, 1f, 1f, 0.1f)); // 호버 효과
        }

        // 들여쓰기
        GUILayout.Space(depth * 14); // Unity 스타일 들여쓰기

        // 펼치기/접기 버튼 (유니티 스타일)
        if (hasFoldout)
        {
            GUIStyle foldoutStyle = new GUIStyle(EditorStyles.foldout);
            foldoutStyle.fixedWidth = 12;
            foldoutStyle.fixedHeight = 16;
            bool newState = EditorGUILayout.Toggle(isFoldedOut, foldoutStyle, GUILayout.Width(12));
            if (newState != isFoldedOut)
            {
                _foldoutStates[node.RelativePath] = newState;
            }
        }
        else
        {
            GUILayout.Space(12);
        }

        // 폴더 아이콘 (유니티 기본 폴더 아이콘 사용)
        Texture2D folderIcon = GetFolderIcon(node, isFoldedOut);
        if (folderIcon != null)
        {
            GUILayout.Label(folderIcon, GUILayout.Width(16), GUILayout.Height(16));
        }
        else
        {
            GUILayout.Space(16);
        }

        GUILayout.Space(2);

        // 폴더 이름
        GUIStyle labelStyle = new GUIStyle(EditorStyles.label);
        labelStyle.fontSize = 12;
        labelStyle.normal.textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black;

        if (GUILayout.Button(node.Name, labelStyle))
        {
            _selectedPath = node.RelativePath;
        }

        EditorGUILayout.EndHorizontal();

        // 자식 폴더들 표시 (펼쳐진 경우에만)
        if (hasFoldout && isFoldedOut)
        {
            foreach (var child in node.Children)
            {
                DrawFolderNode(child, depth + 1);
            }
        }
    }

    private Texture2D GetFolderIcon(FolderNode node, bool isExpanded)
    {
        // Unity 에디터의 기본 폴더 아이콘 가져오기
        string iconName = isExpanded ? "FolderOpened Icon" : "Folder Icon";

        // Unity 내장 아이콘 시도
        var icon = EditorGUIUtility.IconContent(iconName)?.image as Texture2D;
        if (icon != null) return icon;

        // 대체 아이콘들 시도
        string[] alternativeIcons = isExpanded
            ? new[] { "d_FolderOpened Icon", "FolderEmpty Icon", "d_Folder Icon" }
            : new[] { "d_Folder Icon", "FolderEmpty Icon", "d_FolderOpened Icon" };

        foreach (string altIcon in alternativeIcons)
        {
            icon = EditorGUIUtility.IconContent(altIcon)?.image as Texture2D;
            if (icon != null) return icon;
        }

        // 특별한 폴더들에 대한 아이콘
        return GetSpecialFolderIcon(node.Name);
    }

    private Texture2D GetSpecialFolderIcon(string folderName)
    {
        // 특별한 폴더들에 대한 커스텀 아이콘
        var specialFolders = new Dictionary<string, string>
        {
            { "Scripts", "cs Script Icon" },
            { "Editor", "d_UnityEditor.AnimationWindow" },
            { "Resources", "d_BuildSettings.Web" },
            { "Plugins", "d_Assembly Icon" },
            { "Materials", "d_Material Icon" },
            { "Textures", "d_Texture2D Icon" },
            { "Prefabs", "d_Prefab Icon" },
            { "Scenes", "d_SceneAsset Icon" },
            { "Audio", "d_AudioClip Icon" },
            { "Animations", "d_Animation Icon" },
            { "Fonts", "d_Font Icon" },
            { "Shaders", "d_Shader Icon" }
        };

        if (specialFolders.ContainsKey(folderName))
        {
            var icon = EditorGUIUtility.IconContent(specialFolders[folderName])?.image as Texture2D;
            if (icon != null) return icon;
        }

        // 기본 폴더 아이콘 반환
        return EditorGUIUtility.IconContent("Folder Icon")?.image as Texture2D;
    }

    private class FolderNode
    {
        public string Name { get; }
        public string RelativePath { get; }
        public string FullPath { get; }
        public List<FolderNode> Children { get; }

        public FolderNode(string name, string relativePath)
        {
            Name = name;
            RelativePath = relativePath;
            FullPath = relativePath == "Assets"
                ? Application.dataPath
                : Application.dataPath + relativePath.Substring(6); // "Assets" 제거하고 실제 경로 생성
            Children = new List<FolderNode>();
        }
    }
}