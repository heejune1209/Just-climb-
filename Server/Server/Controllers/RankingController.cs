using Microsoft.AspNetCore.Mvc;
using Server.Models;
using Server.Services;

namespace Server.Controllers
{
    /// <summary>
    /// 랭킹 시스템 API 컨트롤러
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class RankingController : ControllerBase
    {
        private readonly IRankingService _rankingService;
        private readonly ILogger<RankingController> _logger;

        public RankingController(IRankingService rankingService, ILogger<RankingController> logger)
        {
            _rankingService = rankingService;
            _logger = logger;
        }

        /// <summary>
        /// 스테이지별 랭킹 조회 (정렬되어 제공)
        /// GET /api/ranking?stageNumber=1&sortType=0&page=1&pageSize=20&userId=123
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<RankingResponseDto>> GetRanking([FromQuery] RankingRequestDto request, [FromQuery] string? userId = null)
        {
            try
            {
                _logger.LogInformation("랭킹 조회 요청: Stage={Stage}, SortType={SortType}, UserId={UserId}", 
                    request.StageNumber, request.SortType, userId);

                var result = await _rankingService.GetRankingAsync(userId, request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "랭킹 조회 중 오류 발생");
                return StatusCode(500, new { error = "랭킹 조회 중 오류가 발생했습니다." });
            }
        }

        /// <summary>
        /// 사용자 기록 업데이트
        /// POST /api/ranking/{userId}/record
        /// </summary>
        [HttpPost("{userId}/record")]
        public async Task<ActionResult> UpdateUserRecord(string userId, [FromBody] UpdateRecordRequestDto request)
        {
            try
            {
                // 입력 데이터 로깅
                _logger.LogInformation("기록 업데이트 요청: UserId={UserId}, Stage={Stage}, Time={Time}, Deaths={Deaths}, DisplayName={DisplayName}", 
                    userId, request.StageNumber, request.ClearTime, request.DeathCount, request.DisplayName);

                // 유효성 검사
                if (string.IsNullOrEmpty(userId) || userId.Length > 100)
                {
                    _logger.LogWarning("유효하지 않은 사용자 ID: {UserId}", userId);
                    return BadRequest(new { error = "유효하지 않은 사용자 ID입니다." });
                }

                if (request.StageNumber <= 0 || request.StageNumber > 10)  // 실제 게임 스테이지는 1~10
                {
                    _logger.LogWarning("유효하지 않은 스테이지 번호: {Stage}", request.StageNumber);
                    return BadRequest(new { error = "유효하지 않은 스테이지 번호입니다." });
                }

                if (request.ClearTime <= 0 || request.ClearTime >= float.MaxValue)
                {
                    _logger.LogWarning("유효하지 않은 클리어 타임: {Time}", request.ClearTime);
                    return BadRequest(new { error = "유효하지 않은 클리어 타임입니다." });
                }

                if (request.DeathCount < 0 || request.DeathCount >= int.MaxValue)
                {
                    _logger.LogWarning("유효하지 않은 사망 횟수: {Deaths}", request.DeathCount);
                    return BadRequest(new { error = "유효하지 않은 사망 횟수입니다." });
                }

                if (string.IsNullOrEmpty(request.DisplayName))
                {
                    request.DisplayName = "Player";
                }

                var success = await _rankingService.UpdateUserRecordAsync(userId, request);

                if (success)
                {
                    _logger.LogInformation("기록 업데이트 성공: UserId={UserId}, Stage={Stage}", userId, request.StageNumber);
                    return Ok(new { message = "기록이 성공적으로 업데이트되었습니다." });
                }
                else
                {
                    _logger.LogError("기록 업데이트 실패: UserId={UserId}, Stage={Stage}", userId, request.StageNumber);
                    return StatusCode(500, new { error = "기록 업데이트에 실패했습니다." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "🚨 [RankingController] 기록 업데이트 중 예외 발생\n" +
                    "UserId: {UserId}\n" +
                    "Request: {@Request}\n" +
                    "Exception: {Exception}\n" +
                    "InnerException: {InnerException}\n" +
                    "StackTrace: {StackTrace}", 
                    userId, request, ex.Message, ex.InnerException?.Message, ex.StackTrace);
                return StatusCode(500, new { error = "기록 업데이트 중 오류가 발생했습니다.", details = ex.Message });
            }
        }
    }
} 