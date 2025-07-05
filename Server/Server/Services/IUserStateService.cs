using System.Collections.Generic;
using System.Threading.Tasks;
using Server.Models;

namespace Server.Services
{
    /// <summary>
    /// 델타 이벤트 리스트를 받아 사용자 상태를 병합(UPSERT) 처리하는 서비스 인터페이스입니다.
    /// </summary>
    public interface IUserStateService
    {
        /// <summary>
        /// 지정된 사용자에 대해 델타 이벤트를 병합하고, DB 및 캐시에 UPSERT 작업을 수행합니다.
        /// </summary>
        /// <param name="userId">병합 대상 사용자 식별자</param>
        /// <param name="deltas">클라이언트에서 전송된 델타 이벤트 리스트</param>
        Task MergeDeltasAsync(string userId, IEnumerable<DeltaEventDto> deltas);

        /// <summary>
        /// 지정된 사용자의 전체 상태를 읽어 옵니다(풀 덤프).
        /// 캐시먼저 조회하며, 없으면 DB에서 로드 후 캐시에 저장합니다.
        /// </summary>
        Task<SaveData> LoadStateAsync(string userId);
    }
}
