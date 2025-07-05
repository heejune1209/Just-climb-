using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using JustClimb.Data;

/// <summary>
/// 랭킹 항목 단일 행 UI
/// </summary>
public class UI_RankingEntry : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image _backgroundImage;
    [SerializeField] private TextMeshProUGUI _rankText; // 랭킹 순위 숫자
    [SerializeField] private TextMeshProUGUI _nameText; // 이름
    [SerializeField] private TextMeshProUGUI _valueText; // 시간 또는 사망 횟수 (동적)
    [SerializeField] private GameObject _crownIcon; // 1등 왕관 아이콘 (선택사항)

    public void SetData(int rank, string playerName, float timeSeconds, int deathCount, RankingSortType sortType)
    {
        // 순위 표시
        if (_rankText != null)
        {
            _rankText.text = rank.ToString();
            _rankText.color = Color.black;
        }

        // 플레이어 이름
        if (_nameText != null)
        {
            _nameText.text = playerName;
            _nameText.color = Color.black;
        }

        // 정렬 타입에 따라 값 표시
        if (_valueText != null)
        {
            if (sortType == RankingSortType.ClearTime)
            {
                // 클리어 타임 포맷팅
                var ts = TimeSpan.FromSeconds(timeSeconds);
                _valueText.text = $"{ts.Minutes:00}:{ts.Seconds:00}";
            }
            else // RankingSortType.DeathCount
            {
                // 사망 횟수
                _valueText.text = deathCount.ToString();
            }
            _valueText.color = Color.black;
        }

        // 1등 왕관 표시
        if (_crownIcon != null)
        {
            _crownIcon.SetActive(rank == 1);
        }
    }

    /// <summary>
    /// 이전 버전과의 호환성을 위한 오버로드 메서드 (기본적으로 시간 표시)
    /// </summary>
    public void SetData(int rank, string playerName, float timeSeconds, int deathCount)
    {
        SetData(rank, playerName, timeSeconds, deathCount, RankingSortType.ClearTime);
    }

    public void SetBackgroundColor(Color color)
    {
        if (_backgroundImage != null)
        {
            _backgroundImage.color = color;
        }
    }

} 