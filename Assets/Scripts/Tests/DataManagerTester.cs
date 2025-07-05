using JustClimb.Manager;
using System;
using System.Linq;
using UnityEngine;
using Zenject;

public class DataManagerTester : MonoBehaviour
{
    //// Zenject로부터 주입받을 매니저들
    //[Inject] private IDataManager _dataManager;
    //[Inject] private ICurrencyManager _currencyManager;

    //void Start()
    //{
    //    // 1) JSON 템플릿 복사 및 비동기 로드 시작
    //    _dataManager.Init();   // public async void Init() { … } :contentReference[oaicite:0]{index=0}

    //    // 2) 로드 완료 시점에 호출될 이벤트 핸들러 등록
    //    _dataManager.OnLoaded += HandleOnDataLoaded;  // event Action<SaveData> OnLoaded :contentReference[oaicite:1]{index=1}
    //}

    //private void HandleOnDataLoaded(SaveData data)
    //{
    //    _dataManager.DeleteAllData();  // save.json/.bak 삭제 → 빈 SaveData로 재초기화 :contentReference[oaicite:2]{index=2}
    //    // 3) 데이터가 메모리에 올라온 직후, 젬 100개 충전
    //    _currencyManager.AddGems(100);  // void AddGems(int) :contentReference[oaicite:2]{index=2}

    //    Debug.Log($"[DataManagerTester] Data initialized. Gems now: {_currencyManager.GetGems()}");

    //    // 4) 이후 중복 호출 방지
    //    _dataManager.OnLoaded -= HandleOnDataLoaded;
    //}


}
