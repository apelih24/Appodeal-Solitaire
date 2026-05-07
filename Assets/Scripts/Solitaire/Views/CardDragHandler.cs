using Appodeal.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Appodeal.Solitaire
{
    [RequireComponent(typeof(CardView))]
    [RequireComponent(typeof(BoxCollider2D))]
    public class CardDragHandler : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        private CardView _cardView;
        private GameManager _gameManager;
        private TweenService _tweenService;
        private Camera _mainCamera;
        private Vector3 _originalPosition;
        private int _originalSortingOrder;

        public bool IsDragging { get; private set; }

        private const int DragSortingOrder = 1000;
        private const float SnapBackDuration = 0.2f;

        public void Initialize(GameManager gameManager, TweenService tweenService)
        {
            _gameManager = gameManager;
            _tweenService = tweenService;
            _cardView = GetComponent<CardView>();
            _mainCamera = Camera.main;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_cardView.Data == null || !_cardView.Data.IsFaceUp) return;
            if (!_gameManager.IsTopCard(_cardView.Data)) return;

            IsDragging = true;
            _originalPosition = transform.position;
            _originalSortingOrder = _cardView.GetSortingOrder();
            _cardView.SetSortingOrder(DragSortingOrder);
            _tweenService.CancelTween(transform);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!IsDragging) return;

            var worldPos = _mainCamera.ScreenToWorldPoint(eventData.position);
            worldPos.z = transform.position.z;
            transform.position = worldPos;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!IsDragging) return;
            IsDragging = false;

            var targetStack = FindStackUnderPointer(eventData.position);
            if (targetStack != null)
            {
                var sourceStackId = _gameManager.FindStackContaining(_cardView.Data);
                if (sourceStackId != null && sourceStackId != targetStack.StackId)
                {
                    _gameManager.ExecuteMove(sourceStackId, targetStack.StackId,
                        _cardView.Data.Suit, _cardView.Data.Rank);
                    return;
                }
            }

            _cardView.SetSortingOrder(_originalSortingOrder);
            _tweenService.MoveTo(transform, _originalPosition, SnapBackDuration);
        }

        private StackView FindStackUnderPointer(Vector2 screenPosition)
        {
            var worldPos = _mainCamera.ScreenToWorldPoint(screenPosition);
            var hits = Physics2D.OverlapPointAll(worldPos);
            foreach (var hit in hits)
            {
                var stackView = hit.GetComponent<StackView>();
                if (stackView != null) return stackView;
            }
            return null;
        }
    }
}
