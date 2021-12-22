using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using U3DExtends;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using ContextMenu = U3DExtends.ContextMenu;

[InitializeOnLoad]
public class ArtSceneTools
{
    private static bool ShowInfo = false;
    private static Vector2 _scrollPos;
    
    private static Texture2D _bgBoxTexture2D;
    private static Texture2D _btnTexture2D;
    private static Texture2D _infoBoxTexture2D;
    private static Texture2D _selectBgTexture2D;
    private static GUIStyle _bgBoxStyle;
    private static GUIStyle _btnStyle;
    // private static GUIStyle _infoBoxStyle;
    private static int _bgBoxWidth = 320;
    
    
    static Vector2 mPos = Vector2.zero;


    private static Dictionary<int, bool> _btnStateDic;

    static BetterList<Item> mItems = new BetterList<Item>();
    static List<Item> _selections = new List<Item>();
    static Mode mMode = Mode.CompactMode;
    static GUIContent mContent;
    static 	int mTab = 0;
    static int _labelDefaultFontSize;


    class Item
    {
        public GameObject prefab;
        public string guid;
        public Texture tex;
        public bool dynamicTex = false;
    }
    
    
    enum Mode
    {
        CompactMode,
        IconMode,
        DetailedMode,
    }
    
    static GameObject[] draggedObjects
    {
        get
        {
            if (DragAndDrop.objectReferences == null || DragAndDrop.objectReferences.Length == 0) 
                return null;
			
            return DragAndDrop.objectReferences.Where(x=>x as GameObject).Cast<GameObject>().ToArray();
        }
        set
        {
            if (value != null)
            {
                DragAndDrop.PrepareStartDrag();
                DragAndDrop.objectReferences = value;
                draggedObjectIsOurs = true;
            }
            else DragAndDrop.AcceptDrag();
        }
    }
    
    static bool draggedObjectIsOurs
    {
        get
        {
            object obj = DragAndDrop.GetGenericData("Scene Tool");
            if (obj == null)
            {         
                Debug.Log("-----------------null");
                return false;
            }
            Debug.Log("-----------------"  + obj.ToString());
            return (bool)obj;
        }
        set
        {
            DragAndDrop.SetGenericData("Scene Tool", value);
        }
    }
    
    [InitializeOnLoadMethod]
    static void Init()
    {
        EditorSceneManager.activeSceneChangedInEditMode -= ChangedActiveScene;
        EditorSceneManager.activeSceneChangedInEditMode += ChangedActiveScene;
    }

    private static void ChangedActiveScene(Scene arg0, Scene arg1)
    {     
        SceneView.duringSceneGui -= OnArtSceneGUI;
        if (arg1.name == "ArtScene")
        {
            InitCollections();
            SetTexture();
            InitGuiStyle();
            SceneView.duringSceneGui += OnArtSceneGUI;
        }
    }
    
    static void InitCollections()
    {
        _btnStateDic = new Dictionary<int, bool>();
        for (int i = 0; i < 10; i++)
        {
            _btnStateDic.Add(i,false);
        }
    }

    static void SetTexture()
    {
        _bgBoxTexture2D = new Texture2D(_bgBoxWidth, Screen.height);
        for (int y = 0; y < _bgBoxTexture2D.height; y++)
        {
            for (int x = 0; x < _bgBoxTexture2D.width; x++)
            {
                _bgBoxTexture2D.SetPixel(x, y, new Color(0.18f, 0.18f, 0.18f,1));
            }
        }
        _bgBoxTexture2D.Apply();
      
        _btnTexture2D = new Texture2D(275, 30);
        for (int y = 0; y < _btnTexture2D.height; y++)
        {
            for (int x = 0; x < _btnTexture2D.width; x++)
            {
                _btnTexture2D.SetPixel(x, y, new Color(0.2196079f, 0.2196079f, 0.2196079f, 1));
            }
        }
        _btnTexture2D.Apply();

        _infoBoxTexture2D = new Texture2D(275, 300);
        for (int y = 0; y < _infoBoxTexture2D.height; y++)
        {
            for (int x = 0; x < _infoBoxTexture2D.width; x++)
            {
                _infoBoxTexture2D.SetPixel(x, y, new Color(0.31f, 0.31f, 0.31f,1));
            }
        }
        _infoBoxTexture2D.Apply();

        _selectBgTexture2D = new Texture2D(_bgBoxWidth, Screen.height);
        for (int y = 0; y < _selectBgTexture2D.height; y++)
        {
            for (int x = 0; x < _selectBgTexture2D.width; x++)
            {
                if (x == _selectBgTexture2D.width - 1 || y == _selectBgTexture2D.height - 1)
                {
                    _selectBgTexture2D.SetPixel(x, y, new Color(0.22f, 0.42f, 0.87f,1));
                }
                else
                {
                    _selectBgTexture2D.SetPixel(x, y, new Color(0.15f, 0.15f, 0.15f,1));
                }
            }
        }
        _selectBgTexture2D.Apply();
    }
    
    static GUIStyle mStyle;
    private static void InitGuiStyle()
    {
        mContent = new GUIContent();
        _labelDefaultFontSize = EditorStyles.label.fontSize;

        _bgBoxStyle = GUI.skin.box;
        _bgBoxStyle.normal.background = _bgBoxTexture2D;
        _btnStyle = GUI.skin.button;
        _btnStyle.normal.background = _btnTexture2D;
        _btnStyle.alignment = TextAnchor.MiddleLeft;
        
        mStyle = new GUIStyle();
        mStyle.alignment = TextAnchor.MiddleCenter;
        mStyle.padding = new RectOffset(2, 2, 2, 2);
        mStyle.clipping = TextClipping.Clip;
        mStyle.wordWrap = true;
        mStyle.stretchWidth = false;
        mStyle.stretchHeight = false;
        mStyle.normal.textColor = UnityEditor.EditorGUIUtility.isProSkin ? new Color(1f, 1f, 1f, 0.5f) : new Color(0f, 0f, 0f, 0.5f);
        mStyle.normal.background = null;
    }

    static void OnArtSceneGUI(SceneView sceneView)
    {
        Handles.BeginGUI();
        OnDrawGUi();
        Handles.EndGUI();
    }

    const int cellPadding = 4;
    static int mCellSize=50;
    static int cellSize { get { return mCellSize; } }
    static bool mMouseIsInside = false;


    static void OnDrawGUi()
    {
        Event currentEvent = Event.current;
        EventType eventType = currentEvent.type;
        int x = cellPadding, y = cellPadding;
        int width = 310 - cellPadding;
        int spacingX = cellSize + cellPadding;
        int spacingY = spacingX;
        GameObject[] draggeds = draggedObjects;
        bool isDragging = (draggeds != null);
        int indexUnderMouse = GetCellUnderMouse(spacingX, spacingY);
        
        if (indexUnderMouse != -1 && isDragging)
        {
            _bgBoxStyle.normal.background = _selectBgTexture2D;
        }
        else
        {
            _bgBoxStyle.normal.background = _bgBoxTexture2D;
        }
        bool eligibleToDrag = (currentEvent.mousePosition.y < Screen.height - 40);

        if (indexUnderMouse != -1)
        {
            if (eventType == EventType.MouseDown)
            {

                Debug.Log("-------------->> MouseDown");
                mMouseIsInside = true;
            }
            else if (eventType == EventType.MouseDrag)
            {
                mMouseIsInside = true;
                Debug.Log("-------------->> MouseDrag1");

                if (eligibleToDrag)
                {
                    Debug.Log("-------------->> MouseDrag2");
                    if (draggedObjectIsOurs) DragAndDrop.StartDrag("Scene Tool");
                    currentEvent.Use();
                }
            }
            else if (eventType == EventType.MouseUp)
            {
                DragAndDrop.PrepareStartDrag();
                mMouseIsInside = false;
            }
            else if (eventType == EventType.DragUpdated)
            {
                mMouseIsInside = true;
                UpdateVisual();
                currentEvent.Use();
            }
            else if (eventType == EventType.DragPerform)
            {
                if (draggeds != null)
                {
                    Debug.Log("------------");
                    // if (_selections != null)
                    // {
                    //     foreach (var selection in _selections)
                    //     {
                    //         DestroyTexture(selection);
                    //         mItems.Remove(selection);
                    //     }
                    // }
                    
                    foreach (var dragged in draggeds)
                    {
                        AddItem(dragged, indexUnderMouse);
                        ++indexUnderMouse;
                    }
				                
                    draggeds = null;
                }
                mMouseIsInside = false;
                currentEvent.Use();
            }
            else if (eventType == EventType.DragExited || eventType == EventType.Ignore)
            {
                mMouseIsInside = false;
            }
        }
       
        if (!mMouseIsInside)
        {
            draggeds = null;
        }
        
              
        GUILayout.BeginArea(new Rect(0, 0, _bgBoxWidth, Screen.height));
        if (!ShowInfo)
        {
            if (GUILayout.Button("▶",_btnStyle,GUILayout.Width(25),GUILayout.Height(Screen.height)))
            {
                ShowInfo = true;
            }
        }
        else
        {
            GUILayout.BeginHorizontal("box",_bgBoxStyle,GUILayout.Width(_bgBoxWidth),GUILayout.Height(Screen.height));
            GUILayout.BeginVertical(GUILayout.Width(283),GUILayout.Height(Screen.height));
            
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.Width(280), GUILayout.Height(Screen.height));

            BetterList<int> indices = new BetterList<int>();

            
            // for (int i = 0; i < 10; i++)
            // {
                _btnStateDic.TryGetValue(0,out var state);
                if (GUILayout.Button(state?"▼按钮模板":"▶按钮模板",_btnStyle,GUILayout.Width(275),GUILayout.Height(30)))
                {
                    _btnStateDic[0] = !state;
                }

                if (state)
                {
                    _bgBoxStyle.normal.background = _infoBoxTexture2D;
                    GUILayout.BeginVertical("box" ,GUILayout.Width(275),GUILayout.Height(300));

                    #region MyRegion
                    
                    string searchFilter = EditorPrefs.GetString("PrefabWin_SearchFilter", null);
                    
                    for (int i = 0; i < mItems.size; )
                    {
                        // if (draggeds != null && indices.size == indexUnderMouse)
                        //     indices.Add(-1);

                        var has = _selections.Exists(item => item == mItems[i]);
			
                        if (!has)
                        {
                            if (string.IsNullOrEmpty(searchFilter) ||
                                mItems[i].prefab.name.IndexOf(searchFilter, System.StringComparison.CurrentCultureIgnoreCase) != -1)
                                indices.Add(i);
                        }
			
                        ++i;
                    }

                    // if (!indices.Contains(-1)) indices.Add(-1);

                    if (eligibleToDrag && eventType == EventType.MouseDown && indexUnderMouse > -1)
                    {
                        GUIUtility.keyboardControl = 0;

                        if (currentEvent.button == 0 && indexUnderMouse < indices.size)
                        {
                            int index = indices[indexUnderMouse];

                            if (index != -1 && index < mItems.size)
                            {
                                _selections.Add(mItems[index]);
                                draggedObjects = _selections.Select(item => item.prefab).ToArray();
                                draggeds = _selections.Select(item=>item.prefab).ToArray();
                                currentEvent.Use();
                            }
                        }
                    }

                    mPos = EditorGUILayout.BeginScrollView(mPos);
                    {
                        Color normal = new Color(1f, 1f, 1f, 0.5f);
                        for (int i = 0; i < mItems.size; ++i)
                        {
                            // int index = indices[i];
                            // Item ent = (index != -1) ? mItems[i] : _selections.Count == 0 ? null : _selections[0];
                            Item ent =  mItems[i] ;

                            if (ent != null && ent.prefab == null)
                            {
                                mItems.RemoveAt(i);
                                continue;
                            }

                            Rect rect = new Rect(x, y, cellSize, cellSize);
                            Rect inner = rect;
                            inner.xMin += 2f;
                            inner.xMax -= 2f;
                            inner.yMin += 2f;
                            inner.yMax -= 2f;
                            rect.yMax -= 1f;

                            if (!isDragging && (mMode == Mode.CompactMode || (ent == null || ent.tex != null)))
                                mContent.tooltip = (ent != null) ? ent.prefab.name : "Click to add";
                            else mContent.tooltip = "";

                            //if (ent == selection)
                            {
                                GUI.color = normal;
                                UIEditorHelper.DrawTiledTexture(inner, UIEditorHelper.backdropTexture);
                            }

                            GUI.color = Color.white;
                            GUI.backgroundColor = normal;

                            if (GUI.Button(rect, mContent, "Button"))
                            {
                                // if (ent == null || currentEvent.button == 0)
                                // {
                                //     string path = EditorUtility.OpenFilePanel("Add a prefab", "", "prefab");
                                //
                                //     if (!string.IsNullOrEmpty(path))
                                //     {
                                //         Item newEnt = CreateItemByPath(path);
                                //
                                //         if (newEnt != null)
                                //         {
                                //             mItems.Add(newEnt);
                                //             Save();
                                //         }
                                //     }
                                // }
                                // else if (currentEvent.button == 1)
                                // {
                                //     // ContextMenu.AddItem("Update Preview", false, UpdatePreView, index);
                                //      ContextMenu.AddItemWithArge("Delete", false, RemoveItem, index);
                                //      ContextMenu.Show();
                                // }
                            }

                            string caption = (ent == null) ? "" : ent.prefab.name.Replace("Control - ", "");

                            // if (ent != null)
                            // {
                                if (ent.tex == null)
                                {
                                    //texture may be destroy after exit game
                                    GeneratePreview(ent, false);
                                }

                                if (ent.tex != null)
                                {
                                    GUI.DrawTexture(inner, ent.tex);
                                    var labelPos = new Rect(inner);
                                    var labelStyle = EditorStyles.label;
                                    labelPos.height = labelStyle.lineHeight;
                                    labelPos.y = inner.height - labelPos.height + 5;
                                    labelStyle.fontSize = (int)(_labelDefaultFontSize * SizePercent);
                                    labelStyle.alignment = TextAnchor.LowerCenter;
                                    {
                                        GUI.Label(labelPos, ent.prefab.name, labelStyle);
                                    }
                                    labelStyle.alignment = TextAnchor.UpperLeft;
                                    labelStyle.fontSize = _labelDefaultFontSize;
                                }
                                else if (mMode != Mode.DetailedMode)
                                {
                                    GUI.Label(inner, caption, mStyle);
                                    caption = "";
                                }
                            // }
                            // else GUI.Label(inner, "Add", mStyle);

                            if (mMode == Mode.DetailedMode)
                            {
                                GUI.backgroundColor = new Color(1f, 1f, 1f, 0.5f);
                                GUI.contentColor = new Color(1f, 1f, 1f, 0.7f);
                                GUI.Label(new Rect(rect.x, rect.y + rect.height, rect.width, 32f), caption,
                                    "ProgressBarBack");
                                GUI.contentColor = Color.white;
                                GUI.backgroundColor = Color.white;
                            }

                            x += spacingX;

                            if (x + spacingX > width)
                            {
                                y += spacingY;
                                x = cellPadding;
                            }
                        }

                        GUILayout.Space(y + spacingY);
                    }
                    EditorGUILayout.EndScrollView();
                    
                        // GUILayout.BeginHorizontal();
                        // {
                        //     string after = EditorGUILayout.TextField("", searchFilter, "SearchTextField", GUILayout.Width(250));
                        //
                        //     if (GUILayout.Button("", "SearchCancelButton", GUILayout.Width(18f)))
                        //     {
                        //         after = "";
                        //         GUIUtility.keyboardControl = 0;
                        //     }
                        //
                        //     if (searchFilter != after)
                        //     {
                        //         EditorPrefs.SetString("PrefabWin_SearchFilter", after);
                        //         searchFilter = after;
                        //     }
                        // }
                        // GUILayout.EndHorizontal();

                    

                    #endregion
                    
                    
                    
                    GUILayout.EndVertical();
                }
            // }
            
            EditorGUILayout.EndScrollView();
            GUILayout.EndVertical();
            if (GUILayout.Button("◀",_btnStyle,GUILayout.Width(25),GUILayout.Height(Screen.height)))
            {
                ShowInfo = false;
            }
            GUILayout.EndHorizontal();
         
        }
        GUILayout.EndArea();
        if (!ShowInfo)
            return;

        
  

    }
    
    private static int GetCellUnderMouse (int spacingX, int spacingY)
    {
        Vector2 pos = Event.current.mousePosition + mPos;

        int topPadding = 24;
        int x = cellPadding, y = cellPadding + topPadding;
        if (pos.x > 320) return -1;

        float width = Screen.width - cellPadding + mPos.x;
        float height = Screen.height - cellPadding + mPos.y;
        int index = 0;

        for (; ; ++index)
        {
            Rect rect = new Rect(x, y, spacingX, spacingY);
            if (rect.Contains(pos)) break;

            x += spacingX;

            if (x + spacingX > width)
            {
                if (pos.x > x) return -1;
                y += spacingY;
                x = cellPadding;
                if (y + spacingY > height) return -1;
            }
        }
        return index;
    }
    
    private static void UpdateVisual ()
    {
        if (draggedObjects == null) DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
        else if (draggedObjectIsOurs) DragAndDrop.visualMode = DragAndDropVisualMode.Move;
        else DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
    }
    
    static Item CreateItemByPath (string path)
    {
        if (!string.IsNullOrEmpty(path))
        {
            path = FileUtil.GetProjectRelativePath(path);
            string guid = AssetDatabase.AssetPathToGUID(path);

            if (!string.IsNullOrEmpty(guid))
            {
                GameObject go = AssetDatabase.LoadAssetAtPath(path, typeof(GameObject)) as GameObject;
                Item ent = new Item();
                ent.prefab = go;
                ent.guid = guid;
                GeneratePreview(ent);
                return ent;
            }
            else Debug.Log("No GUID");
        }
        return null;
    }
    
    static void GeneratePreview (Item item, bool isReCreate = true)
    {
        if (item == null || item.prefab == null) return;
        {
            string preview_path = Configure.ResAssetsPath + "/Preview/" + item.prefab.name + ".png";
            if (!isReCreate && File.Exists(preview_path))
            {
                Texture texture = UIEditorHelper.LoadTextureInLocal(preview_path);
                item.tex = texture;
            }
            else
            {
                Texture Tex = UIEditorHelper.GetAssetPreview(item.prefab);
                if (Tex != null)
                {
                    DestroyTexture(item);
                    item.tex = Tex;
                    UIEditorHelper.SaveTextureToPNG(Tex, preview_path);
                }
            }
            item.dynamicTex = false;
            return;
        }
    }
    
    static void DestroyTexture (Item item)
    {
        if (item != null && item.dynamicTex && item.tex != null)
        {
            Object.DestroyImmediate(item.tex,false);
            item.dynamicTex = false;
            item.tex = null;
        }
    }
    
    static string saveKey { get { return "PrefabWin " + Application.dataPath + " " + mTab; } }
    static void Save ()
    {
        string data = "";

        if (mItems.size > 0)
        {
            string guid = mItems[0].guid;
            StringBuilder sb = new StringBuilder();
            sb.Append(guid);

            for (int i = 1; i < mItems.size; ++i)
            {
                guid = mItems[i].guid;

                if (string.IsNullOrEmpty(guid))
                {
                    Debug.LogWarning("Unable to save " + mItems[i].prefab.name);
                }
                else
                {
                    sb.Append('|');
                    sb.Append(mItems[i].guid);
                }
            }
            data = sb.ToString();
        }
        EditorPrefs.SetString(saveKey, data);
    }
    
    static void RemoveItem (object obj)
    {
        int index = (int)obj;
        if (index < mItems.size && index > -1)
        {
            Item item = mItems[index];
            DestroyTexture(item);
            mItems.RemoveAt(index);
        }
        Save();
    }

    private static float mSizePercent = 0.5f;
     public static float SizePercent
    {
        get { return mSizePercent; }
        set 
        {
            if (mSizePercent != value)
            {
                mSizePercent = value;
                mCellSize = Mathf.FloorToInt(80 * SizePercent + 10);
                EditorPrefs.SetFloat("PrefabWin_SizePercent", mSizePercent);
            }
        }
    }
     
    static void AddItem (GameObject go, int index)
     {
         string guid = U3DExtends.UIEditorHelper.ObjectToGUID(go);

         if (string.IsNullOrEmpty(guid))
         {
             string path = EditorUtility.SaveFilePanelInProject("Save a prefab", go.name + ".prefab", "prefab", "Save prefab as...", "");

             if (string.IsNullOrEmpty(path)) return;

             go = PrefabUtility.CreatePrefab(path, go);
             if (go == null) return;

             guid = U3DExtends.UIEditorHelper.ObjectToGUID(go);
             if (string.IsNullOrEmpty(guid)) return;
         }

         Item ent = new Item();
         ent.prefab = go;
         ent.guid = guid;
         GeneratePreview(ent);
         RectivateLights();
         
         if (index < mItems.size) mItems.Insert(index, ent);
         else mItems.Add(ent);
         Save();
     }
    
    static BetterList<Light> mLights;

    static void RectivateLights ()
    {
        if (mLights != null)
        {
            for (int i = 0; i < mLights.size; ++i)
                mLights[i].enabled = true;
            mLights = null;
        }
    }

}
