using UnityEngine;

namespace Appodeal.Solitaire
{
    [RequireComponent(typeof(BoxCollider2D))]
    public class StackView : MonoBehaviour
    {
        [SerializeField] private string _stackId;
        [SerializeField] private float _cardOffsetY = -0.3f;

        public string StackId => _stackId;

        public Vector3 GetCardPosition(int index)
        {
            return transform.position + new Vector3(0f, index * _cardOffsetY, 0f);
        }
    }
}
