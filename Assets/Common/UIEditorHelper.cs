#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace U3DExtends
{
    public static class UIEditorHelper
    {
        [MenuItem("Edit/Copy Names " + Configure.ShortCut.CopyNodesName, false, 2)]
        public static void CopySelectWidgetName()
        {
            string result = "";
            foreach (var item in Selection.gameObjects)
            {
                string item_name = item.name;
                Transform root_trans = item.transform.parent;
                while (root_trans != null && root_trans.GetComponent<LayoutInfo>() == null)
                {
                    if (root_trans.parent != null && root_trans.parent.GetComponent<LayoutInfo>() == null)
                        item_name = root_trans.name + "/" + item_name;
                    else
                        break;
                    root_trans = root_trans.parent;
                }
                result = result + "\"" + item_name + "\",";
            }

            //复制到系统全局的粘贴板上
            GUIUtility.systemCopyBuffer = result;
            Debug.Log("Copy Nodes Name Succeed！");
        }

        static public GameObject GetUITestRootNode()
        {
            GameObject testUI = GameObject.Find(Configure.UITestNodeName);
            if (!testUI)
            {
                testUI = new GameObject(Configure.UITestNodeName, typeof(RectTransform));
                RectTransform trans = testUI.GetComponent<RectTransform>();
                trans.position = Configure.UITestNodePos;
                trans.sizeDelta = Configure.UITestNodeSize;
                testUI.AddComponent<ReopenLayoutOnExitGame>();
            }
            return testUI;
        }

        public static GameObject CreatNewLayout(bool isNeedLayout = true)
        {
            GameObject testUI = UIEditorHelper.GetUITestRootNode();
            
            string file_path = Path.Combine(Configure.ResAssetsPath, "Canvas.prefab");
            file_path = FileUtil.GetProjectRelativePath(file_path);
            GameObject layout_prefab = UnityEditor.AssetDatabase.LoadAssetAtPath(file_path, typeof(UnityEngine.Object)) as GameObject;
            GameObject layout = GameObject.Instantiate(layout_prefab) as GameObject;
            layout.transform.SetParent(testUI.transform);
            Vector3 last_pos = layout.transform.localPosition;
            layout.transform.localPosition = new Vector3(last_pos.x, last_pos.y, 0);
            if (!isNeedLayout)
            {
                Transform child = layout.transform.Find("Layout");
                // layout.transform.DetachChildren();
                if (child!=null)
                    Undo.DestroyObjectImmediate(child.gameObject);
            }

            Selection.activeGameObject = layout;
            RectTransform trans = layout.transform as RectTransform;
            SceneView.lastActiveSceneView.MoveToView(trans);
            return layout;
        }
        

        public static Decorate CreateEmptyDecorate(Transform parent)
        {
            string file_path = Path.Combine(Configure.ResAssetsPath, "Decorate.prefab");
            file_path = FileUtil.GetProjectRelativePath(file_path);
            GameObject decorate_prefab = UnityEditor.AssetDatabase.LoadAssetAtPath(file_path, typeof(UnityEngine.Object)) as GameObject;
            GameObject decorate = GameObject.Instantiate(decorate_prefab) as GameObject;
            decorate.transform.SetParent(parent);
            RectTransform rectTrans = decorate.transform as RectTransform;
            rectTrans.SetAsFirstSibling();
            rectTrans.localPosition = Vector3.zero;
            rectTrans.localScale = Vector3.one;
            Decorate decor = rectTrans.GetComponent<Decorate>();
            return decor;
        }

        //从界面的Canvas里取到真实的界面prefab
        public static Transform GetRealLayout(GameObject anyObj)
        {
            LayoutInfo layoutInfo = anyObj.GetComponentInParent<LayoutInfo>();
            Transform real_layout = null;
            if (layoutInfo == null)
                return real_layout;
            if (layoutInfo.LayoutPath != string.Empty)
            {
                string just_name = System.IO.Path.GetFileNameWithoutExtension(layoutInfo.LayoutPath);
                for (int i = 0; i < layoutInfo.transform.childCount; i++)
                {
                    Transform child = layoutInfo.transform.GetChild(i);
                    if (child.name.StartsWith(just_name))
                    {
                        real_layout = child;
                        break;
                    }
                }
            }
            else
            {
                //界面是新建的,未保存过的情况下取其子节点
                Canvas layout = anyObj.GetComponentInParent<Canvas>();
                for (int i = 0; i < layout.transform.childCount; i++)
                {
                    Transform child = layout.transform.GetChild(i);
                    if (child.GetComponent<Decorate>() != null)
                        continue;

                    real_layout = child.transform;
                    break;
                }
            }
            return real_layout;
        }

        public static void DelayReLoadLayout(GameObject o, bool isQuiet)
        {
            System.Action<PlayModeStateChange> p = null;
            p = new System.Action<PlayModeStateChange>((PlayModeStateChange c) => {
                Debug.Log("reload !");
                ReLoadLayout(o, isQuiet);
                UnityEditor.EditorApplication.playModeStateChanged -= p;
            });
            UnityEditor.EditorApplication.playModeStateChanged += p;
        }

        public static void ReLoadLayout(GameObject o, bool isQuiet)
        {
            GameObject saveObj = o == null ? Selection.activeGameObject : (o as GameObject);
            if (saveObj == null)
                return;
            LayoutInfo layoutInfo = saveObj.GetComponentInParent<LayoutInfo>();
            if (layoutInfo != null && layoutInfo.LayoutPath != string.Empty)
            {
                bool is_reopen = isQuiet || EditorUtility.DisplayDialog("警告", "是否重新加载？", "来吧", "不了");
                if (is_reopen)
                {
                    string just_name = System.IO.Path.GetFileNameWithoutExtension(layoutInfo.LayoutPath);
                    Transform real_layout = GetRealLayout(layoutInfo.gameObject);

                    if (real_layout)
                    {
                        string select_path = FileUtil.GetProjectRelativePath(layoutInfo.LayoutPath);
                        Object prefab = AssetDatabase.LoadAssetAtPath(select_path, typeof(Object));
                        GameObject new_view = PrefabUtility.InstantiateAttachedAsset(prefab) as GameObject;
                        new_view.transform.SetParent(layoutInfo.transform);
                        new_view.transform.localPosition = real_layout.localPosition;
                        new_view.transform.localScale = Vector3.one;
                        new_view.name = just_name;
                        PrefabUtility.DisconnectPrefabInstance(new_view);//链接中的话删里面的子节点时会报警告，所以还是一直失联的好，保存时直接覆盖pref
                        Undo.DestroyObjectImmediate(real_layout.gameObject);
                        Debug.Log("Reload Layout Succeed!");
                        layoutInfo.ApplyConfig(select_path);
                    }
                }
            }
            else
                Debug.Log("Try to reload unsaved layout failed");
        }

        public static Transform LoadLayoutByPath(string select_path)
        {
            //Debug.Log("select_path : "+select_path);
            GameObject new_layout = CreatNewLayout(false);
            new_layout.transform.localPosition = new Vector3(new_layout.transform.localPosition.x, new_layout.transform.localPosition.y, 0);
            LayoutInfo layoutInfo = new_layout.GetComponent<LayoutInfo>();
            layoutInfo.LayoutPath = select_path;
            if (!File.Exists(select_path))
            {
                Debug.Log("UIEditorHelper:LoadLayoutByPath cannot find layout file:"+select_path);
                return null;
            }
            string asset_relate_path = select_path;
            if (!select_path.StartsWith("Assets/"))
                asset_relate_path = FileUtil.GetProjectRelativePath(select_path);

            Object prefab = AssetDatabase.LoadAssetAtPath(asset_relate_path, typeof(Object));
            GameObject new_view = PrefabUtility.InstantiateAttachedAsset(prefab) as GameObject;
            new_view.transform.SetParent(new_layout.transform);
            new_view.transform.localPosition = Vector3.zero;
            new_view.transform.localScale = Vector3.one;
            string just_name = System.IO.Path.GetFileNameWithoutExtension(asset_relate_path);
            new_view.name = just_name;
            new_layout.gameObject.name = just_name + "_Canvas";
            PrefabUtility.DisconnectPrefabInstance(new_view);//链接中的话删里面的子节点时会报警告，所以还是一直失联的好，保存时直接覆盖prefab就行了
            //打开界面时,从项目临时文件夹找到对应界面的参照图配置,然后生成参照图
            layoutInfo.ApplyConfig(asset_relate_path);
            ReopenLayoutOnExitGame.RecordOpenLayout(select_path, new_layout.transform.localPosition);
            return new_layout.transform;
        }

        public static bool SaveTextureToPNG(Texture inputTex, string save_file_name)
        {
            RenderTexture temp = RenderTexture.GetTemporary(inputTex.width, inputTex.height, 0, RenderTextureFormat.ARGB32);
            Graphics.Blit(inputTex, temp);
            bool ret = SaveRenderTextureToPNG(temp, save_file_name);
            RenderTexture.ReleaseTemporary(temp);
            return ret;

        }

        //将RenderTexture保存成一张png图片  
        public static bool SaveRenderTextureToPNG(RenderTexture rt, string save_file_name)
        {
            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = rt;
            Texture2D png = new Texture2D(rt.width, rt.height, TextureFormat.ARGB32, false);
            png.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            byte[] bytes = png.EncodeToPNG();
            string directory = Path.GetDirectoryName(save_file_name);
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);
            FileStream file = File.Open(save_file_name, FileMode.Create);
            BinaryWriter writer = new BinaryWriter(file);
            writer.Write(bytes);
            file.Close();
            Texture2D.DestroyImmediate(png);
            png = null;
            RenderTexture.active = prev;
            return true;

        }

        public static Texture2D LoadTextureInLocal(string file_path)
        {
            //创建文件读取流
            FileStream fileStream = new FileStream(file_path, FileMode.Open, FileAccess.Read);
            fileStream.Seek(0, SeekOrigin.Begin);
            //创建文件长度缓冲区
            byte[] bytes = new byte[fileStream.Length];
            //读取文件
            fileStream.Read(bytes, 0, (int)fileStream.Length);
            //释放文件读取流
            fileStream.Close();
            fileStream.Dispose();
            fileStream = null;

            //创建Texture
            int width = 300;
            int height = 372;
            Texture2D texture = new Texture2D(width, height);
            texture.LoadImage(bytes);
            return texture;
        }

        private static Vector2 HalfVec = new Vector2(0.5f, 0.5f);
        //加载外部资源为Sprite
        public static Sprite LoadSpriteInLocal(string file_path)
        {
            if (!File.Exists(file_path))
            {
                Debug.Log("LoadSpriteInLocal() cannot find sprite file : " + file_path);
                return null;
            }
            Texture2D texture = LoadTextureInLocal(file_path);
            //创建Sprite
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), UIEditorHelper.HalfVec);
            return sprite;
        }

        public static Texture GetAssetPreview(GameObject obj)
        {
            GameObject canvas_obj = null;
            GameObject clone = GameObject.Instantiate(obj);
            Transform cloneTransform = clone.transform;
            bool isUINode = false;
            if (cloneTransform is RectTransform)
            {
                //如果是UGUI节点的话就要把它们放在Canvas下了
                canvas_obj = new GameObject("render canvas", typeof(Canvas));
                Canvas canvas = canvas_obj.GetComponent<Canvas>();
                cloneTransform.SetParent(canvas_obj.transform);
                cloneTransform.localPosition = Vector3.zero;

                canvas_obj.transform.position = new Vector3(-1000, -1000, -1000);
                canvas_obj.layer = 21;//放在21层，摄像机也只渲染此层的，避免混入了奇怪的东西
                isUINode = true;
            }
            else
                cloneTransform.position = new Vector3(-1000, -1000, -1000);

            Transform[] all = clone.GetComponentsInChildren<Transform>();
            foreach (Transform trans in all)
            {
                trans.gameObject.layer = 21;
            }

            Bounds bounds = GetBounds(clone);
            Vector3 Min = bounds.min;
            Vector3 Max = bounds.max;
            GameObject cameraObj = new GameObject("render camera");

            Camera renderCamera = cameraObj.AddComponent<Camera>();
            renderCamera.backgroundColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            renderCamera.clearFlags = CameraClearFlags.Color;
            renderCamera.cameraType = CameraType.Preview;
            renderCamera.cullingMask = 1 << 21;
            if (isUINode)
            {
                cameraObj.transform.position = new Vector3((Max.x + Min.x) / 2f, (Max.y + Min.y) / 2f, cloneTransform.position.z-100);
                Vector3 center = new Vector3(cloneTransform.position.x+0.01f, (Max.y + Min.y) / 2f, cloneTransform.position.z);//+0.01f是为了去掉Unity自带的摄像机旋转角度为0的打印，太烦人了
                cameraObj.transform.LookAt(center);

                renderCamera.orthographic = true;
                float width = Max.x - Min.x;
                float height = Max.y - Min.y;
                float max_camera_size = width > height ? width : height;
                renderCamera.orthographicSize = max_camera_size / 2;//预览图要尽量少点空白
            }
            else
            {
                cameraObj.transform.position = new Vector3((Max.x + Min.x) / 2f, (Max.y + Min.y) / 2f, Max.z + (Max.z - Min.z));
                Vector3 center = new Vector3(cloneTransform.position.x+0.01f, (Max.y + Min.y) / 2f, cloneTransform.position.z);
                cameraObj.transform.LookAt(center);

                int angle = (int)(Mathf.Atan2((Max.y - Min.y) / 2, (Max.z - Min.z)) * 180 / 3.1415f * 2);
                renderCamera.fieldOfView = angle;
            }
            RenderTexture texture = new RenderTexture(128, 128, 0, RenderTextureFormat.Default);
            renderCamera.targetTexture = texture;

            Undo.DestroyObjectImmediate(cameraObj);
            Undo.PerformUndo();//不知道为什么要删掉再Undo回来后才Render得出来UI的节点，3D节点是没这个问题的，估计是Canvas创建后没那么快有效？
            renderCamera.RenderDontRestore();
            RenderTexture tex = new RenderTexture(128, 128, 0, RenderTextureFormat.Default);
            Graphics.Blit(texture, tex);

            Object.DestroyImmediate(canvas_obj);
            Object.DestroyImmediate(cameraObj);
            return tex;
        }

        public static Bounds GetBounds(GameObject obj)
        {
            Vector3 Min = new Vector3(99999, 99999, 99999);
            Vector3 Max = new Vector3(-99999, -99999, -99999);
            MeshRenderer[] renders = obj.GetComponentsInChildren<MeshRenderer>();
            if (renders.Length > 0)
            {
                for (int i = 0; i < renders.Length; i++)
                {
                    if (renders[i].bounds.min.x < Min.x)
                        Min.x = renders[i].bounds.min.x;
                    if (renders[i].bounds.min.y < Min.y)
                        Min.y = renders[i].bounds.min.y;
                    if (renders[i].bounds.min.z < Min.z)
                        Min.z = renders[i].bounds.min.z;

                    if (renders[i].bounds.max.x > Max.x)
                        Max.x = renders[i].bounds.max.x;
                    if (renders[i].bounds.max.y > Max.y)
                        Max.y = renders[i].bounds.max.y;
                    if (renders[i].bounds.max.z > Max.z)
                        Max.z = renders[i].bounds.max.z;
                }
            }
            else
            {
                RectTransform[] rectTrans = obj.GetComponentsInChildren<RectTransform>();
                Vector3[] corner = new Vector3[4];
                for (int i = 0; i < rectTrans.Length; i++)
                {
                    //获取节点的四个角的世界坐标，分别按顺序为左下左上，右上右下
                    rectTrans[i].GetWorldCorners(corner);
                    if (corner[0].x < Min.x)
                        Min.x = corner[0].x;
                    if (corner[0].y < Min.y)
                        Min.y = corner[0].y;
                    if (corner[0].z < Min.z)
                        Min.z = corner[0].z;

                    if (corner[2].x > Max.x)
                        Max.x = corner[2].x;
                    if (corner[2].y > Max.y)
                        Max.y = corner[2].y;
                    if (corner[2].z > Max.z)
                        Max.z = corner[2].z;
                }
            }

            Vector3 center = (Min + Max) / 2;
            Vector3 size = new Vector3(Max.x - Min.x, Max.y - Min.y, Max.z - Min.z);
            return new Bounds(center, size);
        }

        static public string ObjectToGUID(UnityEngine.Object obj)
        {
            string path = AssetDatabase.GetAssetPath(obj);
            return (!string.IsNullOrEmpty(path)) ? AssetDatabase.AssetPathToGUID(path) : null;
        }

        static MethodInfo s_GetInstanceIDFromGUID;
        static public UnityEngine.Object GUIDToObject(string guid)
        {
            if (string.IsNullOrEmpty(guid)) return null;

            if (s_GetInstanceIDFromGUID == null)
                s_GetInstanceIDFromGUID = typeof(AssetDatabase).GetMethod("GetInstanceIDFromGUID", BindingFlags.Static | BindingFlags.NonPublic);

            int id = (int)s_GetInstanceIDFromGUID.Invoke(null, new object[] { guid });
            if (id != 0) return EditorUtility.InstanceIDToObject(id);
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path)) return null;
            return AssetDatabase.LoadAssetAtPath(path, typeof(UnityEngine.Object));
        }

        static public T GUIDToObject<T>(string guid) where T : UnityEngine.Object
        {
            UnityEngine.Object obj = GUIDToObject(guid);
            if (obj == null) return null;

            System.Type objType = obj.GetType();
            if (objType == typeof(T) || objType.IsSubclassOf(typeof(T))) return obj as T;

            if (objType == typeof(GameObject) && typeof(T).IsSubclassOf(typeof(Component)))
            {
                GameObject go = obj as GameObject;
                return go.GetComponent(typeof(T)) as T;
            }
            return null;
        }
        
        static public void DrawTiledTexture(Rect rect, Texture tex)
        {
            GUI.BeginGroup(rect);
            {
                int width = Mathf.RoundToInt(rect.width);
                int height = Mathf.RoundToInt(rect.height);

                for (int y = 0; y < height; y += tex.height)
                {
                    for (int x = 0; x < width; x += tex.width)
                    {
                        GUI.DrawTexture(new Rect(x, y, tex.width, tex.height), tex);
                    }
                }
            }
            GUI.EndGroup();
        }

        static Texture2D CreateCheckerTex(Color c0, Color c1)
        {
            Texture2D tex = new Texture2D(16, 16);
            tex.name = "[Generated] Checker Texture";
            tex.hideFlags = HideFlags.DontSave;

            for (int y = 0; y < 8; ++y) for (int x = 0; x < 8; ++x) tex.SetPixel(x, y, c1);
            for (int y = 8; y < 16; ++y) for (int x = 0; x < 8; ++x) tex.SetPixel(x, y, c0);
            for (int y = 0; y < 8; ++y) for (int x = 8; x < 16; ++x) tex.SetPixel(x, y, c0);
            for (int y = 8; y < 16; ++y) for (int x = 8; x < 16; ++x) tex.SetPixel(x, y, c1);

            tex.Apply();
            tex.filterMode = FilterMode.Point;
            return tex;
        }

        static Texture2D mBackdropTex;
        static public Texture2D backdropTexture
        {
            get
            {
                if (mBackdropTex == null) mBackdropTex = CreateCheckerTex(
                    new Color(0.1f, 0.1f, 0.1f, 0.5f),
                    new Color(0.2f, 0.2f, 0.2f, 0.5f));
                return mBackdropTex;
            }
        }


        static public string GenMD5String(string str)
        {
            System.Security.Cryptography.MD5CryptoServiceProvider md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
            str = System.BitConverter.ToString(md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(str)), 4, 8);
            return str.Replace("-", "");
        }


    }
}
#endif