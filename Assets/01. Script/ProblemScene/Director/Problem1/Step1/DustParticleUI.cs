using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(Image))]
public class DustParticleUI : MonoBehaviour
{
    private RectTransform _rect;
    private Image _image;

    private float _duration = 4f;
    private float _delay = 0f;

    private float _startY;
    private float _endY;

    private Coroutine _loopRoutine;

    // Director_Problem1_Step1에서 생성 직후 값 세팅
    public void Initialize(float duration, float delay)
    {
        _duration = duration;
        _delay = delay;
    }

    private void Awake()
    {
        _rect = GetComponent<RectTransform>();
        _image = GetComponent<Image>();
    }

    private void OnEnable()
    {
        SetupRandomPosition();

        _startY = _rect.anchoredPosition.y;
        _endY = _startY + 100f;

        if (_loopRoutine != null)
            StopCoroutine(_loopRoutine);

        _loopRoutine = StartCoroutine(PlayLoop());
    }

    private void OnDisable()
    {
        if (_loopRoutine != null)
        {
            StopCoroutine(_loopRoutine);
            _loopRoutine = null;
        }
    }

    private void SetupRandomPosition()
    {
        var parentRect = _rect.parent as RectTransform;
        if (parentRect == null) return;

        float x = Random.Range(0f, parentRect.rect.width);
        float y = Random.Range(0f, parentRect.rect.height);
        _rect.anchoredPosition = new Vector2(x, y);
    }

    private IEnumerator PlayLoop()
    {
        yield return new WaitForSeconds(_delay);

        while (true)
        {
            yield return MoveWithFade(_startY, _endY, 0f, 0.6f, _duration * 0.5f);
            yield return MoveWithFade(_endY, _startY, 0.6f, 0f, _duration * 0.5f);
        }
    }

    private IEnumerator MoveWithFade(float fromY, float toY, float fromA, float toA, float time)
    {
        float t = 0f;
        var color = _image.color;

        while (t < time)
        {
            t += Time.deltaTime;
            float lerp = t / time;

            float y = Mathf.Lerp(fromY, toY, lerp);
            float a = Mathf.Lerp(fromA, toA, lerp);

            var pos = _rect.anchoredPosition;
            pos.y = y;
            _rect.anchoredPosition = pos;

            color.a = a;
            _image.color = color;

            yield return null;
        }
    }
}
