using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;

namespace JustClimb.Utils
{
    /// <summary>
    /// Unity 클라이언트용 JSON 직렬화/역직렬화 공통 헬퍼 클래스
    /// 모든 매니저에서 중복되는 JSON 처리 로직을 통합합니다.
    /// </summary>
    public static class JsonHelper
    {
        /// <summary>
        /// JSON 문자열을 객체로 역직렬화 (Unity 통합 버전)
        /// </summary>
        public static T DeserializeObject<T>(string json, T defaultValue = default)
        {
            if (string.IsNullOrEmpty(json))
                return defaultValue;

            try
            {
                return JsonConvert.DeserializeObject<T>(json) ?? defaultValue;
            }
            catch (JsonException ex)
            {
                Debug.LogError($"[JsonHelper] JSON 역직렬화 실패: {ex.Message}\nJSON: {json}");
                return defaultValue;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[JsonHelper] 예상치 못한 오류: {ex.Message}");
                return defaultValue;
            }
        }

        /// <summary>
        /// JSON 문자열을 리스트로 역직렬화
        /// </summary>
        public static List<T> DeserializeList<T>(string json, List<T> defaultValue = null)
        {
            defaultValue ??= new List<T>();
            return DeserializeObject(json, defaultValue);
        }

        /// <summary>
        /// 객체를 JSON 문자열로 직렬화 (Unity 통합 버전)
        /// </summary>
        public static string SerializeObject<T>(T obj, Formatting formatting = Formatting.None)
        {
            if (obj == null)
                return "null";

            try
            {
                return JsonConvert.SerializeObject(obj, formatting);
            }
            catch (JsonException ex)
            {
                Debug.LogError($"[JsonHelper] JSON 직렬화 실패: {ex.Message}\n객체: {obj}");
                return "null";
            }
            catch (Exception ex)
            {
                Debug.LogError($"[JsonHelper] 예상치 못한 오류: {ex.Message}");
                return "null";
            }
        }

        /// <summary>
        /// SaveData 전용 직렬화 (Indented 포맷팅)
        /// </summary>
        public static string SerializeSaveData(SaveData saveData)
        {
            return SerializeObject(saveData, Formatting.Indented);
        }

        /// <summary>
        /// 델타 값 직렬화 (기본 타입과 복합 타입 구분)
        /// </summary>
        public static string SerializeDeltaValue(object value)
        {
            if (value == null) return "null";

            return value switch
            {
                int intVal => intVal.ToString(),
                float floatVal => floatVal.ToString(System.Globalization.CultureInfo.InvariantCulture),
                bool boolVal => boolVal ? "true" : "false",
                string stringVal => stringVal,
                _ => SerializeObject(value) // 복합 타입은 JSON으로 직렬화
            };
        }

        /// <summary>
        /// 배열/리스트 타입 확인 (Unity에서 JsonUtility 대신 Newtonsoft 사용해야 하는 경우)
        /// </summary>
        public static bool IsListType(object obj)
        {
            if (obj == null) return false;

            var type = obj.GetType();
            return type.IsArray ||
                   (type.IsGenericType &&
                    typeof(IEnumerable).IsAssignableFrom(type) &&
                    !typeof(string).IsAssignableFrom(type));
        }

        /// <summary>
        /// Unity JsonUtility 호환 직렬화 (루트 배열 지원 안 함)
        /// </summary>
        public static string SerializeForUnity<T>(T obj)
        {
            if (obj == null) return "null";
            
            try
            {
                // 리스트/배열은 Newtonsoft.Json 사용 (JsonUtility는 루트 레벨 배열 지원 안 함)
                if (IsListType(obj))
                {
                    return JsonConvert.SerializeObject(obj);
                }
                else
                {
                    return JsonUtility.ToJson(obj, true);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[JsonHelper] Unity 직렬화 실패: {ex.Message}");
                return SerializeObject(obj); // 폴백으로 Newtonsoft 사용
            }
        }

        /// <summary>
        /// Unity JsonUtility 호환 역직렬화
        /// </summary>
        public static T DeserializeFromUnity<T>(string json, T defaultValue = default)
        {
            if (string.IsNullOrEmpty(json))
                return defaultValue;

            try
            {
                // 리스트/배열은 Newtonsoft.Json 사용
                if (typeof(IEnumerable).IsAssignableFrom(typeof(T)) && typeof(T) != typeof(string))
                {
                    return JsonConvert.DeserializeObject<T>(json) ?? defaultValue;
                }
                else
                {
                    return JsonUtility.FromJson<T>(json);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[JsonHelper] Unity 역직렬화 실패: {ex.Message}");
                return DeserializeObject(json, defaultValue); // 폴백으로 Newtonsoft 사용
            }
        }
    }
} 