using System.Collections.Generic;
using UnityEngine;

namespace FairyGUI.Dynamic
{
    public sealed class UIPackageMapping : ScriptableObject, IUIPackageHelper
    {
        public string[] PackageIds;
        public string[] PackageNames;

        public string GetPackageNameById(string id)
        {
            if (m_PackageIdToNameMap == null)
            {
                m_PackageIdToNameMap = new Dictionary<string, string>();

                if (PackageIds != null && PackageNames != null)
                {
                    var count = Mathf.Min(PackageIds.Length, PackageNames.Length);
                    for (var i = 0; i < count; i++)
                        m_PackageIdToNameMap.Add(PackageIds[i], PackageNames[i]);
                }
            }
            
            return m_PackageIdToNameMap.TryGetValue(id, out var packageName) ? packageName : null;
        }
        
        private Dictionary<string, string> m_PackageIdToNameMap;
    }
}