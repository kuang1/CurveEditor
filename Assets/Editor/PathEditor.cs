using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

[CustomEditor(typeof(PathCreator))]
public class PathEditor : Editor
{
	PathCreator creator;
	Path path
	{
		get { return creator.path; }
	}

	const float segmentSelectDistanceThreshold = 0.1f;


	void OnEnable()
	{
		creator = this.target as PathCreator;
		if(creator.path == null)
		{
			creator.CreatePath();
		}
	}

	public override void OnInspectorGUI ()
	{
		base.OnInspectorGUI ();

		EditorGUI.BeginChangeCheck();

		bool isClosed = EditorGUILayout.Toggle("Close Path", path.IsClosed);
		if(isClosed != path.IsClosed)
		{
			Undo.RecordObject(creator, "close path");
			path.IsClosed = isClosed;
		}

		if(GUILayout.Button("Create New Path"))
		{
			Undo.RecordObject(creator, "create new path");
			creator.CreatePath();
		}

		if(EditorGUI.EndChangeCheck())
		{
			SceneView.RepaintAll();
		}
	}

	void OnSceneGUI()
	{
		Input();
		Draw();
	}

	void Input()
	{
		Event e = Event.current;
		Vector2 mousePosInWorld = HandleUtility.GUIPointToWorldRay(e.mousePosition).origin;
		if(e.type == EventType.MouseDown && e.button == 0 && e.shift)
		{
			Undo.RecordObject(creator, "Add segment");
			path.AddSegment(mousePosInWorld);
		}

		if(e.type == EventType.MouseDown && e.button == 1)
		{
			float minDistToAnchor = 0.05f;
			int closedAnchorIndex = -1;
			for(int i = 0; i < path.PointsCount; i += 3)
			{
				float dist = Vector2.Distance(mousePosInWorld, path[i]);
				if(dist < minDistToAnchor)
				{
					minDistToAnchor = dist;
					closedAnchorIndex = i;
				}
			}

			if(closedAnchorIndex != -1)
			{
				Undo.RecordObject(creator, "Delete segment");
				path.DeleteSegment(closedAnchorIndex);
			}
		}


	}

	void Draw()
	{
		Color oldColor = Handles.color;

		for(int i = 0; i < path.SegmentCount; i ++)
		{
			Vector2[] points = path.GetPointsInSegment(i);
			Handles.color = Color.black;
			Handles.DrawLine(points[0], points[1]);
			Handles.DrawLine(points[2], points[3]);
			Handles.DrawBezier(points[0], points[3], points[1], points[2], Color.green, null, 2);
		}
			
		for(int i = 0; i < path.PointsCount; i ++)
		{
			Handles.color = path.IsAnchorPoint(i)? Color.red : Color.white;
			Vector2 pos = path[i];
			Vector2 newPos = Handles.FreeMoveHandle(pos, Quaternion.identity, 0.15f, Vector2.zero, Handles.SphereHandleCap);
			if(newPos != pos)
			{
				Undo.RecordObject(creator, "Move Point");
				path.MovePoint(i, newPos);
			}
		}

		Handles.color = oldColor;
	}


}






