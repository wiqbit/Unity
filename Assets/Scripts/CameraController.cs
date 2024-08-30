using System;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts
{
	internal class CameraController : MonoBehaviour
	{
		public GameObject _board = null;
		public Button _reset = null;
		private Vector3 _position;
		private Quaternion _rotation;
		private Vector3 _lastMousePosition = Vector3.zero;
		private float _speed = 20F;

		private void Reset()
		{
			transform.position = _position;
			transform.rotation = _rotation;
			_lastMousePosition = Vector3.zero;
			_reset.interactable = false;
		}
		
		private void Start()
		{
			// Start is called before the first frame update
			transform.LookAt(_board.transform);
			_position = transform.position;
			_rotation = transform.rotation;
			_reset.onClick.AddListener(Reset);
		}
		
		private void Update()
		{
			// Update is called once per frame
			bool leftArrow = Input.GetKey(KeyCode.LeftArrow);
			bool rightArrow = Input.GetKey(KeyCode.RightArrow);
			bool downArrow = Input.GetKey(KeyCode.DownArrow);
			bool upArrow = Input.GetKey(KeyCode.UpArrow);

			if (Input.GetMouseButtonDown(0))
			{
				_lastMousePosition = Input.mousePosition;
			}

			Vector3 translation = Vector3.zero;

			if (leftArrow
				|| rightArrow
				|| downArrow
				|| upArrow)
			{
				if (leftArrow
					|| rightArrow)
				{
					translation = leftArrow ? Vector3.right : Vector3.left;
				}
				else if (downArrow
					|| upArrow)
				{
					translation = downArrow ? Vector3.down : Vector3.up;
				}
			}
			else if (_lastMousePosition != Vector3.zero)
			{
				float x = _lastMousePosition.x - Input.mousePosition.x;
				float y = _lastMousePosition.y - Input.mousePosition.y;

				if (Mathf.Abs(x) > Mathf.Abs(y))
				{
					if ((Input.mousePosition.x < _lastMousePosition.x - 10F)
						|| (Input.mousePosition.x > _lastMousePosition.x + 10F))
					{
						translation = Input.mousePosition.x < _lastMousePosition.x ? Vector3.right : Vector3.left;
					}
				}
				else if (Math.Abs(y) > Math.Abs(x))
				{
					if ((Input.mousePosition.y < _lastMousePosition.y - 10F)
						|| (Input.mousePosition.y > _lastMousePosition.y + 10F))
					{
						translation = Input.mousePosition.y < _lastMousePosition.y ? Vector3.down : Vector3.up;
					}
				}
			}

			if (leftArrow
				|| rightArrow
				|| downArrow
				|| upArrow
				|| _lastMousePosition != Vector3.zero)
			{
				if (translation != Vector3.zero)
				{
					_reset.interactable = true;

					Vector3 currentPosition = transform.position;
					Quaternion currentRotation = transform.rotation;
					float bias = translation == Vector3.right || translation == Vector3.left ? transform.up.y : 1F;

					transform.LookAt(_board.transform);
					transform.Translate(translation * Time.deltaTime * _speed * bias);

					if ((translation == Vector3.down && transform.up.y > 0.99F)
						|| (translation == Vector3.up && transform.up.y < 0.01F))
					{
						transform.position = currentPosition;
						transform.rotation = currentRotation;
					}

					transform.LookAt(_board.transform);
				}

				if (!leftArrow
					&& !rightArrow
					&& !downArrow
					&& !upArrow)
				{
					_lastMousePosition = Input.mousePosition;
				}
			}

			if (Input.GetMouseButtonUp(0))
			{
				_lastMousePosition = Vector3.zero;
			}
		}
	}
}