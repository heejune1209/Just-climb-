using System.Globalization;

namespace Server.Utils
{
    /// <summary>
    /// 문자열 파싱 공통 헬퍼 클래스
    /// 델타 이벤트 처리에서 중복되는 파싱 로직을 통합합니다.
    /// </summary>
    public static class ParseHelper
    {
        /// <summary>
        /// 문자열을 정수로 안전하게 파싱
        /// </summary>
        public static int ParseInt(string value, int defaultValue = 0)
        {
            return int.TryParse(value, out int result) ? result : defaultValue;
        }

        /// <summary>
        /// 문자열을 실수로 안전하게 파싱
        /// </summary>
        public static float ParseFloat(string value, float defaultValue = 0f)
        {
            return float.TryParse(value, CultureInfo.InvariantCulture, out float result) ? result : defaultValue;
        }

        /// <summary>
        /// 문자열을 불린으로 안전하게 파싱
        /// </summary>
        public static bool ParseBool(string value, bool defaultValue = false)
        {
            return bool.TryParse(value, out bool result) ? result : defaultValue;
        }

        /// <summary>
        /// 문자열을 문자열로 안전하게 파싱 (null 처리)
        /// </summary>
        public static string ParseString(string value, string defaultValue = "")
        {
            return string.IsNullOrEmpty(value) ? defaultValue : value;
        }
    }
} 