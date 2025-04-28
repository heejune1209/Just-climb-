using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoginScene : BaseScene
{
    protected override void Init()  // 상속 받은 Awake() 안에서 실행됨. "LoginScene"씬 초기화
    {
        base.Init();

        SceneType = Define.Scene.Login; // 📜LoginScene의 씬 종류는 LoginScene

        //List<GameObject> list = new List<GameObject>();

        //for (int i = 0; i < 5; i++)
        //    list.Add(Managers.Resource.Instantiate("Soldier"));

        //foreach (GameObject obj in list)
        //{
        //    Managers.Resource.Destroy(obj);
        //}
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            //Managers.Scene.LoadScene(Define.Scene.Game);
        }
    }

    public override void Clear()
    {
        Debug.Log("LoginScene Clear!");
    }
}
