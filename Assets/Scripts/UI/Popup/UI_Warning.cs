using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using Zenject;

public class UI_Warning : UI_Popup
{    
    // DI 주입받을 매니저들
    [Inject] private ISceneManagerEx _sceneManager;

    // 자동 바인딩할 버튼 요소를 식별하는 enum
    enum Buttons
    {
        Continue,
        Settings,
        BackToMain,
        BackToLobby   // 추가 예시
    }

    // 버튼 레퍼런스(없으면 null)
    private Button _continueBtn, _settingsBtn, _returnBtn;
    private Button _backToLobbyBtn;

    public override void Init()
    {
        base.Init();

        // 버튼들 자동 바인딩: hierarchy에 같은 이름이 있으면, 없으면 skip
        _continueBtn = TryBindButton(Buttons.Continue);
        _settingsBtn = TryBindButton(Buttons.Settings);
        _returnBtn = TryBindButton(Buttons.BackToMain);
        _backToLobbyBtn = TryBindButton(Buttons.BackToLobby);


        // 버튼 이벤트 연결
        // 버튼 이벤트
        if (_continueBtn != null)
            _continueBtn.gameObject.BindEvent(_ =>
            {
                //Managers.Sound.PlaySFX(0);
                ClosePopupUI();
            });
        if (_settingsBtn != null)
            _settingsBtn.gameObject.BindEvent(_ =>
            {
                //Managers.Sound.PlaySFX(0);
                _uiManager.ShowPopupUI<UI_Settings>("UI_Settings");
            });

        if (_returnBtn != null)
            _returnBtn.gameObject.BindEvent(_ =>
            {
                // 이전 씬으로 돌아가기
                ClosePopupUI();

                PlayerPrefs.SetString("nextScene", _sceneManager.GetSceneName(Define.Scene.Main));
                _sceneManager.LoadScene(Define.Scene.Loading);
            });

        if (_backToLobbyBtn != null)
            _backToLobbyBtn.gameObject.BindEvent(_ =>
            {
                // 이전 씬으로 돌아가기
                ClosePopupUI();

                PlayerPrefs.SetString("nextScene", _sceneManager.GetSceneName(Define.Scene.Lobby));
                _sceneManager.LoadScene(Define.Scene.Loading);
            });
    }

    // enum 이름과 같은 child GameObject를 찾아서 Button 컴포넌트 리턴
    // 못 찾거나 Button이 없으면 null 리턴
    private Button TryBindButton(Buttons btnEnum)
    {
        string name = btnEnum.ToString();
        var go = Util.FindChild(gameObject, name, true);
        if (go == null) return null;
        return go.GetComponent<Button>();
    }

    private void Start()
    {
        // Init이 ShowPopupUI 이후에 호출되지 않는 경우를 대비
        Init();
    }
}
