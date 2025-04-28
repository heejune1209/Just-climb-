using UnityEngine;

public class Define
{
    public enum UIEvent
    {
        Click,
        Drag,
        Key   
    }

    public enum MouseEvent
    {
        Press,
        Click,
    }

    public enum CameraMode
    {
        QuarterView,
    }
    public enum Scene
    {
        Unknown, // 디폴트
        Login, // 로그인 화면 씬
        Lobby, // 로비 씬
        Main, // 인게임 씬
        SelectCharacter,
        Loading,     
        Stage1,
        Stage2,
        Stage3,
        Stage4,
        Stage5,
        Stage6,
        Stage7,
        Stage8,
        Stage9,
        Stage10,
    }
    public enum Sound
    {
        Bgm,
        Effect,
        MaxCount,  // 아무것도 아님. 그냥 Sound enum의 개수 세기 위해 추가. (0, 1, '2' 이렇게 2개) 
    }
}
