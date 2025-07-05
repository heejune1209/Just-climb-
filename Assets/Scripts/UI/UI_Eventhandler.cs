using System;
using UnityEngine.EventSystems;
using UnityEngine;

// GameObject에 붙여서 클릭·드래그 이벤트를 델리게이트로 외부에 노출
public class UI_EventHandler : MonoBehaviour, IPointerClickHandler, IDragHandler
{
    public Action<PointerEventData> OnClickHandler = null;
    public Action<PointerEventData> OnDragHandler = null;

    public void OnPointerClick(PointerEventData eventData) // 클릭 이벤트 오버라이딩
    {
        if (OnClickHandler != null)
            OnClickHandler.Invoke(eventData); // 클릭와 관련된 액션 실행
    }

    public void OnDrag(PointerEventData eventData) // 드래그 이벤트 오버라이딩
    {
        if (OnDragHandler != null)
            OnDragHandler.Invoke(eventData); // 드래그와 관련된 액션 실행
    }

    // BindEvent에서 액션을 할당하면, 클릭·드래그 시 해당 액션이 실행됩니다 ​

    // 메모리 누수 방지
    private void OnDestroy()
    {
        // 이벤트 핸들러 정리
        OnClickHandler = null;
        OnDragHandler = null;
    }
}