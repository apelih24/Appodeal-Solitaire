using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Appodeal.Tweening
{
    public class TweenService : MonoBehaviour
    {
        private readonly Dictionary<Transform, Coroutine> _activeTweens = new();

        public void MoveTo(Transform target, Vector3 destination, float duration, Action onComplete = null)
        {
            if (target == null) return;

            CancelTween(target);

            var coroutine = StartCoroutine(MoveCoroutine(target, destination, duration, onComplete));
            _activeTweens[target] = coroutine;
        }

        public void CancelTween(Transform target)
        {
            if (_activeTweens.TryGetValue(target, out var existing))
            {
                StopCoroutine(existing);
                _activeTweens.Remove(target);
            }
        }

        public void CancelAll()
        {
            StopAllCoroutines();
            _activeTweens.Clear();
        }

        private IEnumerator MoveCoroutine(Transform target, Vector3 destination, float duration, Action onComplete)
        {
            var start = target.position;
            var elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                target.position = Vector3.Lerp(start, destination, EaseOutQuad(t));
                yield return null;
            }

            target.position = destination;
            _activeTweens.Remove(target);
            onComplete?.Invoke();
        }

        private static float EaseOutQuad(float t) => 1f - (1f - t) * (1f - t);
    }
}
