using TMPro;
using UnityEngine;

namespace Appodeal.Solitaire
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class CardView : MonoBehaviour
    {
        [SerializeField] private TextMeshPro _rankText;
        [SerializeField] private TextMeshPro _suitText;
        [SerializeField] private SpriteRenderer _background;
        [SerializeField] private Color _redColor = new(0.8f, 0.1f, 0.1f);
        [SerializeField] private Color _blackColor = new(0.1f, 0.1f, 0.1f);
        [SerializeField] private Color _faceDownColor = new(0.2f, 0.3f, 0.8f);

        public CardData Data { get; private set; }

        public void Initialize(CardData data)
        {
            Data = data;
            Refresh();
        }

        public void Refresh()
        {
            if (Data == null) return;

            if (Data.IsFaceUp)
            {
                _rankText.text = Data.DisplayRank;
                _suitText.text = Data.DisplaySuit;
                var color = Data.IsRed ? _redColor : _blackColor;
                _rankText.color = color;
                _suitText.color = color;
                _background.color = Color.white;
            }
            else
            {
                _rankText.text = string.Empty;
                _suitText.text = string.Empty;
                _background.color = _faceDownColor;
            }
        }

        public void SetSortingOrder(int order)
        {
            _background.sortingOrder = order;
            _rankText.sortingOrder = order + 1;
            _suitText.sortingOrder = order + 1;
        }

        public int GetSortingOrder() => _background.sortingOrder;
    }
}
