using UnityEngine;
using UnityEngine.UI;

namespace Appodeal.Solitaire
{
    public class UIManager : MonoBehaviour
    {
        [SerializeField] private GameManager _gameManager;
        [SerializeField] private Button _undoButton;
        [SerializeField] private Button _newGameButton;

        private void Start()
        {
            _undoButton.onClick.AddListener(OnUndoClicked);
            _newGameButton.onClick.AddListener(OnNewGameClicked);
        }

        private void Update()
        {
            _undoButton.interactable = _gameManager.IsUndoAvailable;
        }

        private void OnUndoClicked()
        {
            _gameManager.Undo();
        }

        private void OnNewGameClicked()
        {
            _gameManager.InitializeGame();
        }

        private void OnDestroy()
        {
            _undoButton.onClick.RemoveListener(OnUndoClicked);
            _newGameButton.onClick.RemoveListener(OnNewGameClicked);
        }
    }
}
