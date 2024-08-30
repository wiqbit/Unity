using System;
using UnityEngine;

namespace Assets.Scripts
{
	internal class PieceController : MonoBehaviour
	{
		public enum MoveCallback
		{
			None,
			Moved,
			PawnPromotionMoved,
			TookBack
		}

		private float _speed = 2F;
		private Square _moveToSquare = null;
		private float _moveDistance = 0F;
		private MoveCallback _moveCallback = MoveCallback.None;
		private Vector3 _remove = Vector3.zero;
		private float _removeDistance = 0F;
		private bool _removeIsPromotedPawn = false;

		public void Move(Square toSquare, MoveCallback moveCallback)
		{
			_moveToSquare = toSquare;
			_moveCallback = moveCallback;
		}

		public void Remove(bool removeIsPromotedPawn)
		{
			Bounds bounds = GetComponentInChildren<MeshRenderer>().bounds;
			_remove = new Vector3(transform.position.x, 0F - bounds.size.y / 2F - 1F, transform.position.z);
			_removeIsPromotedPawn = removeIsPromotedPawn;
		}

		private void Moved()
		{
			_moveToSquare = null;

			GameSystem.Instance.Moved();
		}

		private void PawnPromotionMoved()
		{
			_moveToSquare = null;
		}

		private void PromotedPawnRemoved()
		{
			_remove = Vector3.zero;
		}

		private void Removed()
		{
			_remove = Vector3.zero;

			Destroy(gameObject);
		}

		private void TookBack()
		{
			_moveToSquare = null;

			GameSystem.Instance.TookBack();
		}

		private void MoveTowards(ref float distance, Transform currentTransform, Vector3 targetPosition, Action callback)
		{
			if (distance == 0F)
			{
				distance = Vector3.Distance(currentTransform.position, targetPosition);
			}

			Vector3 moveTowards = Vector3.MoveTowards(currentTransform.position, targetPosition, distance * _speed * Time.deltaTime);

			if (currentTransform.position == moveTowards)
			{
				distance = 0F;

				if (callback != null)
				{
					callback();
				}
			}
			else
			{
				currentTransform.position = moveTowards;
			}
		}

		private void Update()
		{
			// Update is called once per frame
			if (_moveToSquare != null)
			{
				Action callback = null;

				switch (_moveCallback)
				{
					case MoveCallback.Moved:
						callback = Moved;
						break;

					case MoveCallback.PawnPromotionMoved:
						callback = PawnPromotionMoved;
						break;

					case MoveCallback.TookBack:
						callback = TookBack;
						break;
				}

				MoveTowards(ref _moveDistance, transform, _moveToSquare.Surface.transform.position, callback);
			}

			if (_remove != Vector3.zero)
			{
				MoveTowards(ref _removeDistance, transform, _remove, _removeIsPromotedPawn ? PromotedPawnRemoved : Removed);
			}
		}
	}
}