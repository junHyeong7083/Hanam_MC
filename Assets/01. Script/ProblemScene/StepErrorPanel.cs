using UnityEngine;
using UnityEngine.UI;
using System.Collections;
public class StepErrorPanel : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private Text label;
    [SerializeField] private string defaultMessage = "다시 생각해볼까요?";
    [SerializeField] private float showDuration = 1f;

    Coroutine _routine;
    MonoBehaviour _owner;

    void Awake()
    {
        if (root != null) root.SetActive(false);
    }

    public void Show(MonoBehaviour owner, string msg = null)
    {
        _owner = owner;
        if (string.IsNullOrEmpty(msg)) msg = defaultMessage;

        if (label != null) label.text = msg;
        if (root != null) root.SetActive(true);

        if (_routine != null) _owner.StopCoroutine(_routine);
        if (showDuration > 0f && _owner != null)
            _routine = _owner.StartCoroutine(HideAfterDelay());
    }

    IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(showDuration);
        if (root != null) root.SetActive(false);
        _routine = null;
    }
}
