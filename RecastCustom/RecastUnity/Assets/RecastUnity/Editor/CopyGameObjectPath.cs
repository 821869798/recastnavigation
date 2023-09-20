using UnityEditor;
using UnityEngine;

namespace RecastUnity.ExportEditor
{
	public static class CopyGameObjectPath
	{
		[MenuItem("GameObject/Copy Full Path", false, 0)]
		static void CopyFullPath()
		{
			Transform selectedTransform = Selection.activeTransform;

			if (selectedTransform != null)
			{
				// Unity GameObject.Find支持 /xx/xx
				string fullPath = "/" + GetTransformPath(selectedTransform);
				EditorGUIUtility.systemCopyBuffer = fullPath;
				Debug.Log("Copied Full Path to Clipboard: " + fullPath);
			}
		}

		static string GetTransformPath(Transform transform)
		{
			if (transform.parent == null)
			{
				return transform.name;
			}
			else
			{
				return GetTransformPath(transform.parent) + "/" + transform.name;
			}
		}
	}

}