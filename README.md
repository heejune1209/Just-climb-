## Just Climb!
- 기간 : 2023.09 ~ 2023.12
- 인원 : 4인 (기획 1, 아트1, 프로그래머 2)
- 역할 : 메인 프로그래머
- 도구 : Unity, C#, Github
- 장르 : 어드벤처, 클라이밍, 3인칭 백뷰 
- 플랫폼 : PC

## 프로젝트 설명
- Unity 엔진 기반 3D 백뷰 클라이밍 게임
- 홀드를 이용한 암벽 등반과 장애물을 파훼하여 산 정상에 오르는 게임
- 총 8개 Stage 구성

## 설계서
### Game Flow
![Image](https://github.com/user-attachments/assets/679a1411-5d48-4aaa-879f-68a8efc2cd31)
### Game Structure
![Image](https://github.com/user-attachments/assets/8ba99876-4b0e-4633-a4af-6959b52a5b97)
### Item Structure
![Image](https://github.com/user-attachments/assets/8e673abe-ea12-49bf-bc2b-83854262cff1)
### Obstacle Structure 
![image](https://github.com/user-attachments/assets/24ebe925-cbaf-4006-8240-513eebafee46)

[아이템 다이어그램](https://github.com/heejune1209/Just-climb-/blob/main/%EC%95%84%EC%9D%B4%ED%85%9C%20%EC%8B%9C%ED%80%80%EC%8A%A4%20%EB%8B%A4%EC%9D%B4%EC%96%B4%EA%B7%B8%EB%9E%A8.md)


## 주요 역할
- UI,UX 시스템 제작
- 캐릭터 클라이밍 시스템 분석 및 수정 
- 캐릭터 능력치 밸런싱
- ScriptableObject 기반 ItemData와 IItemUse 인터페이스를 사용해 아이템 확장성을 확보하고, 쿨타임/사용 로직을 ItemManager에 통합하여 구조화
- ObstacleBase와 ObstacleTrigger를 중심으로 장애물 감지,스폰 구조를 구축, RockDropper 등 장애물은 개별 SO 파라미터로 제어 가능

## 기술 스택 및 개발 환경
C#, Unity3D, Visual Studio 2022

## 관련 링크
### 시연 동영상
https://youtu.be/HkNdxTHhVaw?si=IY2KK07vDpRfTPzP
