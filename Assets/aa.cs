using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.U2D;

namespace DefaultNamespace
{
    public class aa
    {
        private const string kPath = "Assets/GameMain/Atlas/";
        private static Dictionary<string, string> m_SpriteNameMapSpriteAtlasName = new Dictionary<string, string>();
        private static void InitSpriteInfo()
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(kPath);
            FileInfo[] files = directoryInfo.GetFiles("*", SearchOption.AllDirectories);
            for (int i = 0; i < files.Length; ++ i)
            {
                if (files[i].Name.EndsWith(".spriteatlas"))
                {
                    SpriteAtlas spriteAtlas = UnityEditor.AssetDatabase.LoadAssetAtPath(Path.Combine(kPath, files[i].Name), typeof(SpriteAtlas)) as SpriteAtlas;
                    Sprite[] sprites = new Sprite[spriteAtlas.spriteCount];
                    spriteAtlas.GetSprites(sprites);
                    for (int j = 0; j < sprites.Length; ++ j)
                    {
                        string spriteName = sprites[j].name.Replace("(Clone)", "");
                        string spriteAtlasName = Path.GetFileNameWithoutExtension(files[i].Name);
                        if (m_SpriteNameMapSpriteAtlasName.ContainsKey(spriteName))
                        {
                            UnityEngine.Debug.LogErrorFormat("Repeat SpriteName SpriteAtlasName：  {0}    {1}      spriteName ： {2}",
                                m_SpriteNameMapSpriteAtlasName[spriteName],  spriteAtlasName, spriteName);
                            continue;
                        }

                        m_SpriteNameMapSpriteAtlasName.Add(spriteName, spriteAtlasName);
                    }
                }
            }
        }

    }
}