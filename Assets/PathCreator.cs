using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PathCreator : MonoBehaviour
{
	[HideInInspector]
	public Path path;


	public void CreatePath()
	{
		path = new Path(this.transform.position);
	}

	void Reset()
	{
		CreatePath();
	}

}






