using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public class ArtSceneTools
{
    private static bool ShowInfo = false;
    private static Vector2 _scrollPos;
    
    private static Texture2D _bgBoxTexture2D;
    private static Texture2D _btnTexture2D;
    private static Texture2D _infoBoxTexture2D;
    private static GUIStyle _bgBoxStyle;
    private static GUIStyle _btnStyle;
    // private static GUIStyle _infoBoxStyle;
    private static int _bgBoxWidth = 310;
    
    
    static Vector2 mPos = Vector2.zero;


    private static Dictionary<int, bool> _btnStateDic;

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
    }
    
    private static void InitGuiStyle()
    {
        _bgBoxStyle = GUI.skin.box;
        _bgBoxStyle.normal.background = _bgBoxTexture2D;
        _btnStyle = GUI.skin.button;
        _btnStyle.normal.background = _btnTexture2D;
        _btnStyle.alignment = TextAnchor.MiddleLeft;
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
        Debug.Log("/////////////////" + indexUnderMouse);
        bool eligibleToDrag = (currentEvent.mousePosition.y < Screen.height - 40);

        if (eventType == EventType.MouseDown)
        {
            mMouseIsInside = true;
        }
        else if (eventType == EventType.MouseDrag)
        {
            mMouseIsInside = true;

            if (indexUnderMouse != -1 && eligibleToDrag)
            {
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
                draggeds = null;
            }
            mMouseIsInside = false;
            currentEvent.Use();
        }
        else if (eventType == EventType.DragExited || eventType == EventType.Ignore)
        {
            mMouseIsInside = false;
        }
        
        if (!mMouseIsInside)
        {
            draggeds = null;
        }
        
        
        GUILayout.BeginArea(new Rect(0, 0, 330, Screen.height));
        _bgBoxStyle.normal.background = _bgBoxTexture2D;
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

            for (int i = 0; i < 10; i++)
            {
                _btnStateDic.TryGetValue(i,out var state);
                if (GUILayout.Button(state?"▼按钮模板":"▶按钮模板",_btnStyle,GUILayout.Width(275),GUILayout.Height(30)))
                {
                    _btnStateDic[i] = !state;
                }

                if (state)
                {
                    _bgBoxStyle.normal.background = _infoBoxTexture2D;
                    GUILayout.BeginVertical("box" ,GUILayout.Width(275),GUILayout.Height(300));
                    GUILayout.Label("-");
                    GUILayout.EndVertical();
                }
            }
            
            EditorGUILayout.EndScrollView();
            GUILayout.EndVertical();
            if (GUILayout.Button("◀",_btnStyle,GUILayout.Width(25),GUILayout.Height(Screen.height)))
            {
                ShowInfo = false;
            }
            GUILayout.EndHorizontal();
         
        }
        GUILayout.EndArea();

    }
    
    private static int GetCellUnderMouse (int spacingX, int spacingY)
    {
        Vector2 pos = Event.current.mousePosition + mPos;

        int topPadding = 24;
        int x = cellPadding, y = cellPadding + topPadding;
        if (pos.y < y) return -1;

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
}
