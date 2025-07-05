using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Server.Database;

namespace Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DatabaseController : ControllerBase
    {
        private readonly JustClimbDbContext _context;

        public DatabaseController(JustClimbDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// 개발용: 모든 사용자 데이터 삭제
        /// </summary>
        [HttpDelete("reset-data")]
        public async Task<IActionResult> ResetData()
        {
            try
            {
                // 모든 사용자 데이터 삭제
                _context.Users.RemoveRange(_context.Users);
                
                // 변경사항 저장
                await _context.SaveChangesAsync();
                
                return Ok(new { message = "모든 데이터가 초기화되었습니다.", timestamp = DateTime.UtcNow });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "데이터 초기화 중 오류가 발생했습니다.", error = ex.Message });
            }
        }

        /// <summary>
        /// 개발용: 데이터베이스 완전 재생성
        /// </summary>
        [HttpPost("recreate")]
        public async Task<IActionResult> RecreateDatabase()
        {
            try
            {
                // 데이터베이스 삭제
                await _context.Database.EnsureDeletedAsync();
                
                // 데이터베이스 재생성 (마이그레이션 적용)
                await _context.Database.MigrateAsync();
                
                return Ok(new { message = "데이터베이스가 재생성되었습니다.", timestamp = DateTime.UtcNow });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "데이터베이스 재생성 중 오류가 발생했습니다.", error = ex.Message });
            }
        }

        /// <summary>
        /// 특정 사용자 데이터만 삭제
        /// </summary>
        [HttpDelete("users/{userId}")]
        public async Task<IActionResult> DeleteUserData(string userId)
        {
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (user != null)
                {
                    _context.Users.Remove(user);
                    await _context.SaveChangesAsync();
                    return Ok(new { message = $"사용자 {userId} 데이터가 삭제되었습니다." });
                }
                
                return NotFound(new { message = "사용자를 찾을 수 없습니다." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "사용자 데이터 삭제 중 오류가 발생했습니다.", error = ex.Message });
            }
        }

        /// <summary>
        /// 데이터베이스 상태 확인
        /// </summary>
        [HttpGet("status")]
        public async Task<IActionResult> GetDatabaseStatus()
        {
            try
            {
                var userCount = await _context.Users.CountAsync();
                var canConnect = await _context.Database.CanConnectAsync();
                
                return Ok(new 
                { 
                    connected = canConnect,
                    userCount = userCount,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "데이터베이스 상태 확인 중 오류가 발생했습니다.", error = ex.Message });
            }
        }
    }
}