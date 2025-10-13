// Bootstrap.cs
using UnityEngine;
public class Bootstrap : MonoBehaviour
{
    [SerializeField] SceneNavigator navigator;
    void Awake()
    {
        DontDestroyOnLoad(gameObject);
        navigator.IsAuthenticated = false;           // 초기엔 미인증
        navigator.GoTo(ScreenId.REGISTER);
    }
}
