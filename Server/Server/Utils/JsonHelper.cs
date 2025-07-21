using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Server.Utils
{
    /// <summary>
    /// JSON 직렬화/역직렬화 공통 헬퍼 클래스
    /// 모든 서비스에서 중복되는 JSON 처리 로직을 통합합니다.
    /// </summary>
    public static class JsonHelper
    {
        /// <summary>
        /// JSON 문자열을 객체로 역직렬화
        /// </summary>
        public static T? DeserializeObject<T>(string json, T? defaultValue = default)
        {
            if (string.IsNullOrEmpty(json))
                return defaultValue;

            try
            {
                return JsonConvert.DeserializeObject<T>(json) ?? defaultValue;
            }
            catch (JsonException)
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// JSON 문자열을 리스트로 역직렬화
        /// </summary>
        public static List<T> DeserializeList<T>(string json)
        {
            if (string.IsNullOrEmpty(json))
                return new List<T>();

            try
            {
                return JsonConvert.DeserializeObject<List<T>>(json) ?? new List<T>();
            }
            catch (JsonException)
            {
                return new List<T>();
            }
        }

        /// <summary>
        /// 문자열 리스트 전용 역직렬화
        /// </summary>
        public static List<string> DeserializeStringList(string json)
        {
            return DeserializeList<string>(json);
        }

        /// <summary>
        /// 객체를 JSON 문자열로 직렬화
        /// </summary>
        public static string SerializeObject<T>(T obj, string defaultValue = "")
        {
            if (obj == null)
                return defaultValue;

            try
            {
                return JsonConvert.SerializeObject(obj);
            }
            catch (JsonException)
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// 리스트를 JSON 문자열로 직렬화 (빈 배열 기본값)
        /// </summary>
        public static string SerializeList<T>(T obj)
        {
            return SerializeObject(obj, "[]");
        }
    }
} 