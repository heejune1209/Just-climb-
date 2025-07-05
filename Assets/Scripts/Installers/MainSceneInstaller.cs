using UnityEngine;
using Zenject;

namespace JustClimb.Installers
{
    public class MainSceneInstaller : MonoInstaller
    {

        public override void InstallBindings()
        {
            // 기본적으로 전역 바인딩만 쓰면 되니 비워 두거나,
            // 이 씬에서만 Override/추가 바인딩할 게 있으면 여기에 작성
        }
    }
}