#if UNITY_EDITOR
using UnityEngine;
using System.Diagnostics;
using System;

namespace U3DExtends {
    public class CommonHelper {

        //获取文件名
        public static string GetFileNameByPath(string path)
        {
            path = path.Replace("\\", "/");
            int last_gang_index = path.LastIndexOf("/");
            if (last_gang_index == -1)
                return "";
            last_gang_index++;
            string name = path.Substring(last_gang_index, path.Length - last_gang_index);
            int last_dot_index = name.LastIndexOf('.');
            if (last_dot_index == -1)
                return "";
            name = name.Substring(0, last_dot_index);
            return name;
        }
        
    }
}
#endif