using JustClimb.Data;
using System;
using System.IO;
using UnityEngine;

public class DataManager
{
    // 메모리 상에 올려둔 JSON 역직렬화 결과
    public SaveData Current { get; private set; }

    // 데이터 로드/저장 완료 콜백
    public event Action<SaveData> OnLoaded;
    public event Action<SaveData> OnSaved;

    // JSON 데이터가 변경될 때마다 델타(Event)를 발행.
    public event Action<DeltaEvent> OnDeltaGenerated;

    // 내부용: 저장 파일 경로
    string _filePath;
    // 기본 템플릿을 복사해오는 경로
    string _templatePath;

    /// <summary>
    /// Managers.Init()에서 한 번 호출.
    /// </summary>
    public void Init()
    {
        _filePath = Path.Combine(Application.persistentDataPath, "save.json");
        _templatePath = Path.Combine(Application.streamingAssetsPath, "save_template.json");

        // 첫 실행 시 템플릿 복사
        if (!File.Exists(_filePath) && File.Exists(_templatePath))
            File.Copy(_templatePath, _filePath);

        Load();
    }

    /// <summary>
    /// 파일에서 JSON을 읽어 Current에 역직렬화하고 OnLoaded 호출
    /// </summary>
    public void Load()
    {
        try
        {
            if (File.Exists(_filePath))
            {
                string json = File.ReadAllText(_filePath);
                Current = JsonUtility.FromJson<SaveData>(json);
                Debug.Log($"[DataManager] JSON 로드 완료 :\n{json}");
            }
            else
            {
                Current = new SaveData();
                Save();
            }
            OnLoaded?.Invoke(Current);
        }
        catch (Exception e)
        {
            Debug.LogError($"[DataManager] Load 실패: {e}");
            Current = new SaveData();
            OnLoaded?.Invoke(Current);
        }
    }

    /// <summary>
    /// Current를 JSON으로 직렬화해 파일에 덮어쓰고 OnSaved 호출
    /// </summary>
    public void Save()
    {
        try
        {
            // 1) JSON으로 직렬화해서 파일에 덮어쓰기
            Debug.Log($"[DataManager] JSON 저장 시작: {_filePath}");
            string json = JsonUtility.ToJson(Current, true);
            File.WriteAllText(_filePath, json);

            // 3) 델타 이벤트 발행 (전체 JSON 델타)
            OnDeltaGenerated?.Invoke(new DeltaEvent("json:full", json));

            // 2) 저장 완료 사실을 알리는 콜백
            Debug.Log($"[DataManager] JSON 파일 내용:\n{json}");
            OnSaved?.Invoke(Current);
        }
        catch (Exception e)
        {
            Debug.LogError($"[DataManager] Save 실패: {e}");
        }
    }
    // 1) 외부 코드에서 DataManager.Current 조작(Current 프로퍼티에 담긴 메모리 상의 데이터 구조를 직접 변경)
    // 2) DataManager.Save() 호출되면,
    // 2-1) OnSaved 이벤트를 Invoke함으로써,
    // OnSaved += …로 구독해 둔 모든 메서드가 변경된 Current 데이터를 넘겨받고 후속 처리를 할수 있게 된다.

    /// <summary>
    /// 저장된 파일을 통째로 삭제하고, 메모리도 새 인스턴스로 리셋한 뒤 OnSaved 호출
    /// </summary>
    public void DeleteAllData()
    {
        try
        {
            if (File.Exists(_filePath))
                File.Delete(_filePath);
        }
        catch (Exception e)
        {
            Debug.LogError($"[DataManager] DeleteAllData 실패: {e}");
        }

        Current = new SaveData();
        Save(); // 새로 빈 JSON 파일 생성
    }
}
