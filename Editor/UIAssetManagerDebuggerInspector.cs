using UnityEditor;

namespace FairyGUI.Dynamic.Editor
{
    [CustomEditor(typeof(UIAssetManager.Debugger))]
    public class UIAssetManagerDebuggerInspector : UnityEditor.Editor
    {
        private UIAssetManager.Debugger m_Debugger;
    
        private void OnEnable()
        {
            m_Debugger = (UIAssetManager.Debugger)target;
        }

        public override void OnInspectorGUI()
        {
            var dict = m_Debugger.GetUIPackageInfoDict();

            foreach (var info in dict.Values)
            {
                EditorGUILayout.BeginVertical("box");
                
                EditorGUILayout.LabelField("packageName", info.PackageName);
                EditorGUILayout.LabelField("referenceCount", info.ReferenceCount.ToString());
                EditorGUILayout.LabelField("isLoadDone", info.IsLoadDone.ToString());
                
                EditorGUILayout.EndVertical();
            }
            
            EditorUtility.SetDirty(target);
        }
    }
}
