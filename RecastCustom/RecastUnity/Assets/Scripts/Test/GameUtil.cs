using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class GameUtil
{
	private static Camera _camera;
	public static Camera mainCamera
	{
		get
		{
			if (_camera == null || !_camera.isActiveAndEnabled)
			{
				_camera = Camera.main;
			}
			return _camera;
		}
	}
}
