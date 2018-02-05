using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class Path
{
	[SerializeField, HideInInspector]
	List<Vector2> _points;

	[SerializeField, HideInInspector]
	bool _isClosed;
	[SerializeField, HideInInspector]
	bool _autoSetControlPoints;


	public Path(Vector2 center)
	{
		_points = new List<Vector2>()
		{
			center + Vector2.left,
			center + (Vector2.left + Vector2.up) * 0.5f,
			center + (Vector2.right + Vector2.down) * 0.5f,
			center + Vector2.right,
		};
	}

	public Vector2 this[int i]
	{
		get
		{
			return _points[i];
		}
	}

	public int PointsCount
	{
		get
		{
			return _points.Count;
		}
	}

	public int SegmentCount
	{
		get
		{
			int count = (_points.Count - 4) / 3 + 1;
			return _isClosed? count + 1 : count;
		}
	}

	public bool IsAnchorPoint(int pointIndex)
	{
		return pointIndex % 3 == 0;
	}

	public void AddSegment(Vector2 anchorPos)
	{
		Vector2 last = _points[_points.Count - 1];
		Vector2 last2 = _points[_points.Count - 2];

		Vector2 ctrl1 = last + (last - last2);
		Vector2 ctrl2 = (ctrl1 + anchorPos) * 0.5f;

		_points.Add(ctrl1);
		_points.Add(ctrl2);
		_points.Add(anchorPos);
	}

	public void SplitSegment(Vector2 anchorPos, int segmentIndex)
	{
		_points.InsertRange(segmentIndex * 3 + 2, new Vector2[]{ Vector2.zero, anchorPos, Vector2.zero });
		AutoSetAllAffectedControlPoints(segmentIndex * 3 + 3);
	}

	public void DeleteSegment(int anchorIndex)
	{
		if(_isClosed && SegmentCount <= 2)
			return;
		if(!_isClosed && SegmentCount <= 1)
			return;
			
		if(anchorIndex == 0)
		{
			if(_isClosed)
			{
				_points[_points.Count - 1] = _points[2];
			}
			_points.RemoveRange(0, 3);
		}
		else if(anchorIndex == _points.Count - 1 && !_isClosed)
		{
			_points.RemoveRange(anchorIndex - 2, 3);
		}
		else
		{
			_points.RemoveRange(anchorIndex - 1, 3);
		}
	}

	public Vector2[] GetPointsInSegment(int i)
	{
		return new Vector2[]
		{
			_points[i * 3],
			_points[i * 3 + 1],
			_points[i * 3 + 2],
			_points[LoopIndex(i * 3 + 3)],
		};
	}

	public void MovePoint(int i, Vector2 newPos)
	{
		Vector2 deltaMove = newPos - _points[i];
		_points[i] = newPos;

		if(i % 3 == 0) //is anchor point
		{
			if(i - 1 >= 0 || _isClosed)
			{
				_points[LoopIndex(i - 1)] += deltaMove;
			}
			if(i + 1 < _points.Count || _isClosed)
			{
				_points[LoopIndex(i + 1)] += deltaMove;
			}
		}
		else //is control point
		{
			bool nextIsAnchorPoint = ((i + 1) % 3 == 0)? true : false;

			int otherControlPointIndex = nextIsAnchorPoint? i + 2 : i - 2;
			int anchorIndex = nextIsAnchorPoint? i + 1 : i - 1;

			if((otherControlPointIndex >= 0 && otherControlPointIndex < _points.Count)
				|| _isClosed)
			{
				anchorIndex = LoopIndex(anchorIndex);
				otherControlPointIndex = LoopIndex(otherControlPointIndex);

				float dist = Vector3.Distance(_points[anchorIndex], _points[otherControlPointIndex]);
				Vector2 dir = (_points[anchorIndex] - newPos).normalized;

				_points[otherControlPointIndex] = _points[anchorIndex] + dir * dist;
			}
		}
	}


	public void ToggleClosedPath()
	{
		_isClosed = !_isClosed;

		Vector2 lastAnchor = _points[_points.Count - 1];
		Vector2 lastCtrl = _points[_points.Count - 2];

		Vector2 firstAnchor = _points[0];
		Vector2 firstCtrl = _points[1];

		if(_isClosed)
		{
			Vector2 ctrl1 = lastAnchor + (lastAnchor - lastCtrl);
			Vector2 ctrl2 = firstAnchor + (firstAnchor - firstCtrl);
			_points.Add(ctrl1);
			_points.Add(ctrl2);
		}
		else
		{
			_points.RemoveRange(_points.Count - 2, 2);
		}
	}

	public bool IsClosed
	{
		get { return _isClosed; }
		set
		{
			if(value != _isClosed)
			{
				ToggleClosedPath();
			}
		}
	}

	public bool IsAutoSetControlPoints
	{
		get { return _autoSetControlPoints; }
		set
		{
			if(value != _autoSetControlPoints)
			{
				_autoSetControlPoints = value;
				if(_autoSetControlPoints)
				{
					AutoSetAllControlPoints();
				}
			}
		}
	}


	int LoopIndex(int i)
	{
		// i + points.Count 是为了防止i为负
		return (i + _points.Count) % _points.Count;
	}


	void AutoSetAllAffectedControlPoints(int updatedAnchorIndex)
	{
		for(int i = updatedAnchorIndex - 3; i < updatedAnchorIndex + 3; i ++)
		{
			if((i >= 0 && i < _points.Count)
				|| _isClosed)
			{
				_AutoSetControlPointsWithAnchor(LoopIndex(i));
			}
		}
		_AutoSetFirstAndLastControlPoints();
	}

	void AutoSetAllControlPoints()
	{
		for(int i = 0; i < _points.Count; i += 3)
		{
			_AutoSetControlPointsWithAnchor(i);
		}
		_AutoSetFirstAndLastControlPoints();
	}

	void _AutoSetControlPointsWithAnchor(int anchorIndex)
	{
		Vector2 anchorPos = _points[anchorIndex];
		Vector2 dir = Vector2.zero;
		float[] neighourDistances = new float[2];

		if(anchorIndex - 3 >= 0 || _isClosed)
		{
			Vector2 offset = _points[LoopIndex(anchorIndex - 3)] - anchorPos;
			dir += offset.normalized;
			neighourDistances[0] = offset.magnitude;
		}
		if(anchorIndex + 3 >= 0 || _isClosed)
		{
			Vector2 offset = _points[LoopIndex(anchorIndex + 3)] - anchorPos;
			dir -= offset.normalized;
			neighourDistances[1] = -offset.magnitude;
		}

		dir.Normalize();

		for(int i = 0; i < 2; i ++)
		{
			int controlIndex = anchorIndex + i * 2 - 1;
			if((controlIndex >= 0 && controlIndex < _points.Count)
				|| _isClosed)
			{
				controlIndex = LoopIndex(controlIndex);
				_points[controlIndex] = anchorPos + dir * neighourDistances[i] * 0.5f;
			}
		}
	}

	void _AutoSetFirstAndLastControlPoints()
	{
		if(!_isClosed) // is open path
		{
			_points[1] = (_points[0] + _points[2]) * 0.5f;
			_points[_points.Count - 2] = (_points[_points.Count - 1] + _points[_points.Count - 3]) * 0.5f;
		}
	}

}






