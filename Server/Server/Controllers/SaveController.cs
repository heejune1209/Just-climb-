using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Server.Services;    // IUserStateService 참조
using Server.Models;      // DeltaEventDto 모델 참조

namespace Server.Controllers
{
    /// <summary>
    /// 델타 수신 후 UserStateService로 위임하여
    /// 사용자 상태를 DB 및 캐시에 병합하는 API 컨트롤러입니다.
    /// </summary>
    [ApiController]
    [Route("api/users/{userId}/state")]
    public class SaveController : ControllerBase
    {
        private readonly IUserStateService _userStateService;
        private readonly ILogger<SaveController> _logger;

        // 기능 추가: DI를 통해 IUserStateService 주입
        public SaveController(IUserStateService userStateService, ILogger<SaveController> logger)
        {
            _userStateService = userStateService;
            _logger = logger;
        }

        // 전체 상태 조회 (풀 덤프)
        // GET https://.../api/users/{uid}/state (전체 상태)
        [HttpGet]
        public async Task<IActionResult> GetState([FromRoute] string userId)
        {
            _logger.LogInformation("[SaveController] GET 요청 - UserId: {UserId}", userId);
            
            if (string.IsNullOrWhiteSpace(userId))
            {
                _logger.LogWarning("[SaveController] GET 요청 실패 - UserId가 비어있음");
                return BadRequest("userId is required.");
            }

            try
            {
                var state = await _userStateService.LoadStateAsync(userId);
                if (state == null)
                {
                    _logger.LogWarning("[SaveController] GET 요청 실패 - 사용자를 찾을 수 없음: {UserId}", userId);
                    return NotFound($"User '{userId}' not found.");
                }

                _logger.LogInformation("[SaveController] GET 요청 성공 - UserId: {UserId}, Gold: {Gold}, Gems: {Gems}", 
                    userId, state.gold, state.gems);
                return Ok(state);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "[SaveController] GET 요청 중 오류 발생 - UserId: {UserId}", userId);
                return StatusCode(500, "Internal server error");
            }
        }

        // 델타(증분) 병합
        // POST https://.../api/users/{uid}/state/delta (델타 업로드)
        [HttpPost("delta")]
        public async Task<IActionResult> PostDelta(
            [FromRoute] string userId,
            [FromBody] SaveRequest request)
        {
            _logger.LogInformation("[SaveController] POST 델타 요청 - UserId: {UserId}", userId);
            
            if (string.IsNullOrWhiteSpace(userId)
             || request?.Deltas == null
             || !request.Deltas.Any())
            {
                _logger.LogWarning("[SaveController] POST 델타 요청 실패 - 잘못된 요청: UserId={UserId}, RequestNull={RequestNull}, DeltasCount={DeltasCount}", 
                    userId, request == null, request?.Deltas?.Count() ?? 0);
                return BadRequest("Invalid request: userId and deltas are required.");
            }

            _logger.LogInformation("[SaveController] 받은 델타 개수: {Count}", request.Deltas.Count());
            
            // 받은 델타들을 로그로 출력
            foreach (var delta in request.Deltas.Take(10)) // 처음 10개만 로그 출력
            {
                _logger.LogInformation("[SaveController] 델타: Key={Key}, Value={Value}, Timestamp={Timestamp}", 
                    delta.Key, delta.Value?.Length > 100 ? delta.Value[..100] + "..." : delta.Value, delta.Timestamp);
            }

            try
            {
                // 경로(userId) + 바디(request.Deltas) 만으로 처리
                await _userStateService.MergeDeltasAsync(userId, request.Deltas);
                
                _logger.LogInformation("[SaveController] POST 델타 요청 성공 - UserId: {UserId}, 처리된 델타 개수: {Count}", 
                    userId, request.Deltas.Count());
                return Ok();
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "[SaveController] POST 델타 요청 중 오류 발생 - UserId: {UserId}", userId);
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
