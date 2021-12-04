using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace U3DExtends {
public class PrefabWin : EditorWindow
{
	private static int _labelDefaultFontSize;

    [MenuItem("Window/PrefabWin", false, 9)]
    static public void OpenPrefabTool()
    {
	    _labelDefaultFontSize = EditorStyles.label.fontSize;
        PrefabWin prefabWin = (PrefabWin)EditorWindow.GetWindow<PrefabWin>(false, "Prefab Win", true);
		prefabWin.autoRepaintOnSceneChange = true;
		prefabWin.Show();
    }

    static public PrefabWin instance;

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

	const int cellPadding = 4;
    float mSizePercent = 0.5f;

    public float SizePercent
    {
        get { return mSizePercent; }
        set 
        {
            if (mSizePercent != value)
            {
                mReset = true;
                mSizePercent = value;
                mCellSize = Mathf.FloorToInt(80 * SizePercent + 10);
                EditorPrefs.SetFloat("PrefabWin_SizePercent", mSizePercent);
            }
        }
    }
    int mCellSize=50;
    int cellSize { get { return mCellSize; } }

	int mTab = 0;
	Mode mMode = Mode.CompactMode;
	Vector2 mPos = Vector2.zero;
	bool mMouseIsInside = false;
	GUIContent mContent;
	GUIStyle mStyle;

	BetterList<Item> mItems = new BetterList<Item>();

	GameObject[] draggedObjects
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

	bool draggedObjectIsOurs
	{
		get
		{
			object obj = DragAndDrop.GetGenericData("Prefab Tool");
			if (obj == null)
			{
				Debug.Log("----------------- null");
				return false;
			}
			Debug.Log("-----------------"  + obj.ToString());
			return (bool)obj;
		}
		set
		{
			DragAndDrop.SetGenericData("Prefab Tool", value);
		}
	}

	
	void OnEnable ()
	{
		instance = this;
		
		Load();

		mContent = new GUIContent();
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

	void OnDisable ()
	{
		instance = null;
		foreach (Item item in mItems) DestroyTexture(item);
		Save();
	}
 
	void OnSelectionChange () { Repaint(); }

	public void Reset ()
	{
		foreach (Item item in mItems) DestroyTexture(item);
		mItems.Clear();

		if (mTab == 0 && Configure.PrefabWinFirstSearchPath!="")
		{
			List<string> filtered = new List<string>();
			string[] allAssets = AssetDatabase.GetAllAssetPaths();
            
			foreach (string s in allAssets)
			{
                //search prefab files in folder:Configure.PrefabWinFirstSearchPath
                bool isComeFromPrefab = Regex.IsMatch(s, Configure.PrefabWinFirstSearchPath+@"/((?!/).)*\.prefab");
                if (isComeFromPrefab)
					filtered.Add(s);
			}

			filtered.Sort(string.Compare);
			foreach (string s in filtered) AddGUID(AssetDatabase.AssetPathToGUID(s), -1);
			RectivateLights();
		}
	}
	

	Item AddGUID (string guid, int index)
	{
        GameObject go = U3DExtends.UIEditorHelper.GUIDToObject<GameObject>(guid);

		if (go != null)
		{
			Item ent = new Item();
			ent.prefab = go;
			ent.guid = guid;
			GeneratePreview(ent, false);
			if (index < mItems.size) mItems.Insert(index, ent);
			else mItems.Add(ent);
			return ent;
		}
		return null;
	}
	

    string saveKey { get { return "PrefabWin " + Application.dataPath + " " + mTab; } }

	void Save ()
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

	void Load ()
	{
        mTab = EditorPrefs.GetInt("PrefabWin Prefab Tab", 0);
        SizePercent = EditorPrefs.GetFloat("PrefabWin_SizePercent", 0.5f);

		foreach (Item item in mItems) DestroyTexture(item);
		mItems.Clear();

        string data = EditorPrefs.GetString(saveKey, "");
        //data = "";//For test
        if (string.IsNullOrEmpty(data))
		{
			Reset();
		}
		else
		{
			if (string.IsNullOrEmpty(data)) return;
			string[] guids = data.Split('|');
			foreach (string s in guids) AddGUID(s, -1);
			RectivateLights();
		}
	}

	void DestroyTexture (Item item)
	{
		if (item != null && item.dynamicTex && item.tex != null)
		{
            DestroyImmediate(item.tex);
			item.dynamicTex = false;
			item.tex = null;
		}
	}

	void UpdateVisual ()
	{
		if (draggedObjects == null)
		{
			Debug.Log("进来--------------》");
			DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
		}

		else if (draggedObjectIsOurs) DragAndDrop.visualMode = DragAndDropVisualMode.Move;
		else
		{
			Debug.Log("进来+++++++++++++++》");
			DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
		}

		
	}
	

	void GeneratePreview (Item item, bool isReCreate = true)
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

	int GetCellUnderMouse (int spacingX, int spacingY)
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
			// Debug.Log($"X :{x} Y : {y} spacingX : {spacingX} spacingY : {spacingY}");
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

	bool mReset = false;
	void OnGUI ()
	{
		Event currentEvent = Event.current;
		EventType type = currentEvent.type;

		int x = cellPadding, y = cellPadding;
		int width = Screen.width - cellPadding;
		int spacingX = cellSize + cellPadding;
		int spacingY = spacingX;
        if (mMode == Mode.DetailedMode) spacingY += 32;

        GameObject[] draggeds = draggedObjects;
        // Debug.Log("-----------------undermouse " + draggeds );
        bool isDragging = (draggeds != null);
		int indexUnderMouse = GetCellUnderMouse(spacingX, spacingY);
		Debug.Log("/////////////////" + indexUnderMouse);
		bool eligibleToDrag = (currentEvent.mousePosition.y < Screen.height - 40);

		if (type == EventType.MouseDown)
		{
			Debug.Log("-------------->> MouseDown");
			mMouseIsInside = true;
		}
		else if (type == EventType.MouseDrag)
		{
			Debug.Log("-------------->> MouseDrag");
			mMouseIsInside = true;

			if (indexUnderMouse != -1 && eligibleToDrag)
			{
				if (draggedObjectIsOurs) DragAndDrop.StartDrag("Prefab Tool");
				currentEvent.Use();
			}
		}
		else if (type == EventType.MouseUp)
		{
			Debug.Log("-------------->> MouseUp");
			DragAndDrop.PrepareStartDrag();
			mMouseIsInside = false;
			Repaint();
		}
		else if (type == EventType.DragUpdated)
		{
			Debug.Log("-------------->> DragUpdated");
			mMouseIsInside = true;
			UpdateVisual();
			currentEvent.Use();
		}
		else if (type == EventType.DragPerform)
		{
			Debug.Log("-------------->> DragPerform");
			
			if (draggeds != null)
			{
				draggeds = null;
			}
			mMouseIsInside = false;
			currentEvent.Use();
		}
		else if (type == EventType.DragExited || type == EventType.Ignore)
		{
			Debug.Log("-------------->> DragExited");

			mMouseIsInside = false;
		}
		

        SizePercent = EditorGUILayout.Slider(SizePercent, 0, 1);
    }
}
}