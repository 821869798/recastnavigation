using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.AI;

public class GameMainNavMesh : MonoBehaviour
{
	public NavMeshAgent character;
	public Joystick joystick;
	public float speed = 5f;

	private void OnEnable()
	{
		character.enabled = true;
		joystick.onTouchMove += OnJoystickMove;
		joystick.onTouchUp += OnJoystickUp;
	}

	private void OnDisable()
	{
		character.enabled = false;
		joystick.onTouchMove -= OnJoystickMove;
		joystick.onTouchUp -= OnJoystickUp;
	}

	private void OnJoystickMove(JoystickData joystickData)
	{
		character.ResetPath();

		var forward = GameUtil.mainCamera.transform.forward;
		forward = Vector3.ProjectOnPlane(forward, Vector3.up);
		character.transform.rotation = Quaternion.LookRotation(forward) * Quaternion.Euler(0, -joystickData.angle + 90, 0);
		var offset = character.transform.forward * speed * joystickData.power * Time.deltaTime;
		character.Move(offset);
	}

	private void OnJoystickUp()
	{

	}

	private void Update()
	{
		if (Input.GetMouseButtonDown(0))
		{
			Ray ray = GameUtil.mainCamera.ScreenPointToRay(Input.mousePosition);

			if (Physics.Raycast(ray, out var hit))
			{
				character.SetDestination(hit.point);
			}
		}
	}

}
