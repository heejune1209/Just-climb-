using System;

namespace JustClimb.Data
{
    /// <summary>
    /// DataManager에서 데이터 변경이 발생할 때마다 생성되어
    /// DataSyncManager로 전달하는 델타 모델.
    /// 데이터 전송·저장을 위한 순수한 DTO(데이터 전송 객체) 역할
    /// </summary>
    [Serializable]
    public class DeltaEvent
    {
        /// <summary>
        /// 변경된 데이터의 키 (예: "gold", "items", "stage:1:bestTime" 등)
        /// </summary>
        public string Key;

        /// <summary>
        /// 변경된 값의 직렬화된 표현
        /// (단순타입은 .ToString(), 복합객체는 JsonUtility.ToJson() 결과)
        /// </summary>
        public string Value;

        /// <summary>
        /// UTC 기준 Unix 밀리초 타임스탬프
        /// </summary>
        public long Timestamp;

        /// <summary>
        /// JSON 역직렬화용 빈 생성자
        /// </summary>
        public DeltaEvent() { }

        /// <summary>
        /// 새로운 델타 이벤트를 만들 때 사용.
        /// 자동으로 현재 UTC 시간을 타임스탬프에 기록.
        /// </summary>
        public DeltaEvent(string key, string value)
        {
            Key = key;
            Value = value;
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        public override string ToString()
            => $"[Δ] {Key} = {Value} @ {Timestamp}";
    }
}
