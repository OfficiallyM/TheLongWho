using UnityEditor;
using UnityEngine;

public static class TransformPathCopier
{
	[MenuItem("CONTEXT/Transform/Copy Full Path")]
	static void CopyFullPath(MenuCommand command)
	{
		Transform t = (Transform)command.context;
		string path = t.name;
		while (t.parent != null)
		{
			t = t.parent;
			path = t.name + "/" + path;
		}

		EditorGUIUtility.systemCopyBuffer = path;
		Debug.Log("Copied path: " + path);
	}
}