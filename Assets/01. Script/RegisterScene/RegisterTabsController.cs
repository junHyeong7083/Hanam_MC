using UnityEngine;

public enum RegisterTab { Login, Signup }

public class RegisterTabsController : MonoBehaviour
{
    [SerializeField] GameObject loginPanel;
    [SerializeField] GameObject signupPanel;

    [Header("Initial Tab")]
    [SerializeField] RegisterTab defaultTab = RegisterTab.Login; // 시작 탭 지정

    void Awake()
    {
        // 초기 탭은 여기서 '한 번만' 결정
        if (defaultTab == RegisterTab.Login) ShowLogin();
        else ShowSignup();
    }

    public void ShowLogin()
    {
        Debug.Log("[Tabs] ShowLogin");
        if (loginPanel) loginPanel.SetActive(true);
        if (signupPanel) signupPanel.SetActive(false);
    }

    public void ShowSignup()
    {
        Debug.Log("[Tabs] ShowSignup");
        if (loginPanel) loginPanel.SetActive(false);
        if (signupPanel) signupPanel.SetActive(true);
    }
}
