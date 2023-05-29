using System.Collections.Generic;
using System.IO;
using System.Linq;
using FairyGUI.Utils;
using UnityEditor;
using UnityEngine;

namespace FairyGUI.Dynamic.Editor
{
    public static class UIPackageUtility
    {
        /// <summary>
        /// 获取所有UIPackage二进制文件
        /// </summary>
        public static List<string> GetUIPackageFiles(string assetsRoot)
        {
            var files = Directory.GetFiles(assetsRoot, "*_fui.bytes", SearchOption.AllDirectories);

            var unityProjectPath = Path.GetFullPath(".").Replace('\\', '/');
            var unityProjectPathLength = unityProjectPath.Length + 1;

            return files.Select(file => Path.GetFullPath(file).Replace('\\', '/').Substring(unityProjectPathLength)).ToList();
        }

        public static bool ParseUIPackageIdAndName(string file, out string id, out string name)
        {
            id = string.Empty;
            name = string.Empty;
            
            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(file);
            if (asset == null)
            {
                Debug.LogError($"FairyGUI: can not load asset at {file}");
                return false;
            }
            
            var buffer = new ByteBuffer(asset.bytes);
            if (buffer.ReadUint() != 0x46475549)
            {
                Debug.LogError($"FairyGUI: old package format found in {file}");
                return false;
            }
                
            buffer.version = buffer.ReadInt();
            var ver2 = buffer.version >= 2;
            buffer.ReadBool(); //compressed
            
            id = buffer.ReadString();
            name = buffer.ReadString();
            
            return true;
        }
    }
}