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
                if (string.IsNullOrEmpty(userId))
                {
                    return BadRequest(new { error = "유효하지 않은 사용자 ID입니다." });
                }

                if (request.ClearTime <= 0)
                {
                    return BadRequest(new { error = "유효하지 않은 클리어 타임입니다." });
                }

                if (request.DeathCount < 0)
                {
                    return BadRequest(new { error = "유효하지 않은 사망 횟수입니다." });
                }

                _logger.LogInformation("기록 업데이트 요청: UserId={UserId}, Stage={Stage}, Time={Time}, Deaths={Deaths}", 
                    userId, request.StageNumber, request.ClearTime, request.DeathCount);

                var success = await _rankingService.UpdateUserRecordAsync(userId, request);

                if (success)
                {
                    return Ok(new { message = "기록이 성공적으로 업데이트되었습니다." });
                }
                else
                {
                    return StatusCode(500, new { error = "기록 업데이트에 실패했습니다." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "기록 업데이트 중 오류 발생: UserId={UserId}", userId);
                return StatusCode(500, new { error = "기록 업데이트 중 오류가 발생했습니다." });
            }
        }
    }
} 