StageClearSequenceController 적용 가이드

이 스크립트가 하는 일

1. 승리(Win) 엔딩 대사가 끝나면 자동 시작합니다.
2. 결과 UI 오브젝트를 즉시 꺼서 화면을 정리합니다.
3. 페이드로 화면을 덮습니다.
4. 책상 위의 "열린 책"과 그 위 오브젝트들을 숨깁니다.
5. 같은 위치에 "닫히는 연출용 책 프리팹"을 생성합니다.
6. `Close Entries` 설정대로 책 닫힘 애니메이션을 실행합니다.
7. Cinemachine 3 우선순위(Priority)로 책장 카메라로 전환합니다.
8. 닫힌 책을 책장 목표 위치(`Shelf Target`)로 이동시켜 꽂습니다.
9. 피규어들을 순차적으로 등장시킵니다.
10. 마지막 페이드 후 로비로 돌아갑니다.

정리하면서 삭제한 것

- 책장 보기 단계에서 카메라 GameObject를 켜고 끄는 방식
- 책장 보기 단계의 별도 Activate/Deactivate 배열

이유

- Cinemachine 3에서는 카메라를 끄는 것보다 Priority를 넘기는 게 `NO camera rendering` 같은 문제를 피하기 쉽습니다.
- 책장 보기 단계의 부가 연출은 한 곳(`On Look At Shelf`)으로 모으는 편이 관리가 편합니다.

인스펙터 항목 설명

Start

- `Auto Play On Win`
  일반적으로 켜 둡니다.

- `Fade Canvas Group`
  화면 전체를 덮는 검은 오버레이(이미지 + CanvasGroup)를 연결합니다.

Hide On Sequence Start

- `Objects To Hide On Sequence Start`
  클리어 루프 시작과 동시에 바로 꺼야 하는 결과 UI/오브젝트를 넣습니다.
  추천:
  - FinalDecisionUI 루트
  - win 텍스트 루트
  - lose 텍스트 루트
  - continue 텍스트 루트
  - 결과 배경 패널
  - "아무 곳 클릭" 같은 안내 오브젝트

Hide During Covered Fade

- `Desk Objects To Hide`
  책상 위의 열린 책과 그 위 소품들을 넣습니다.
  이 목록은 "화면이 덮힌 뒤" 꺼지기 때문에 책 교체가 자연스럽습니다.

Book

- `Book Spawn > Source Transform`
  열린 책이 있던 위치 기준 Transform입니다.
  보통 열린 책 루트 위치에 빈 오브젝트(SpawnPoint)를 하나 두고 그걸 연결하면 편합니다.

- `Book Spawn > Closing Book Prefab`
  클리어 연출 전용 프리팹입니다.
  닫히고, 책장으로 이동할 대상입니다.

- `Book Spawn > Parent`
  선택 항목입니다.
  비워두면 Source Transform의 부모 아래에 생성됩니다.

- `Book Spawn > Position Offset`
  생성 위치 미세 보정값입니다.

- `Book Spawn > Rotation Offset`
  생성 회전 미세 보정값입니다.

- `Book Spawn > Spawn Scale`
  `(0,0,0)`이면 프리팹의 스케일을 그대로 사용합니다.
  스케일을 강제로 맞춰야 할 때만 값을 넣습니다.

- `Book Spawn > Close Entries`
  "책이 닫히는" 애니메이션 정의입니다.
  `TitleBookAnimator`의 회전 엔트리처럼, 특정 자식(Child Path)을 Z 회전으로 닫는 방식입니다.

Close Entry(각 원소)

- `Child Path`
  프리팹 내부에서 회전시킬 자식 경로입니다.
  비워두면 프리팹 루트가 회전합니다.

- `Rotation Z`
  목표 로컬 Z 회전값입니다.

- `Rotate Mode`
  보통 `Fast`면 충분합니다.

- `Duration`
  회전 시간입니다.

- `Delay`
  시작 지연 시간입니다.

- `Ease`
  회전 Ease입니다.

닫힘 세팅 팁

- 표지(cover) 오브젝트가 있는 프리팹이라면:
  - cover 자식만 `Child Path`로 잡고 회전시키는 방식이 가장 자연스럽습니다.

- 여러 파츠를 순차로 닫고 싶다면:
  - 여러 엔트리를 만들고 `Delay`로 살짝씩 엇갈리게 주세요.

Cinemachine 3

- `Look At Shelf Camera`
  책장 장면용 `CinemachineCamera`를 연결합니다.

- `Look At Shelf Camera Priority`
  추천 시작값: `100`

- `Other Cameras`
  평소에 쓰는 모든 `CinemachineCamera`를 넣습니다(우선순위를 내릴 대상).

- `Other Camera Priority`
  추천 시작값: `0`

중요

- 메인 렌더 카메라와 `CinemachineBrain`은 항상 켜 두세요.
- Cinemachine 카메라는 "끄기"보다 Priority로 전환하는 걸 권장합니다.

Shelf Look Event

- `On Look At Shelf`
  카메라 전환 외의 연출을 여기로 연결합니다.
  추천:
  - 플레이어 몸 돌리기/고개 돌리기 애니메이션
  - 타임라인 재생
  - 조명 변경
  - SFX 재생

Book Insert

- `Shelf Target`
  책이 최종적으로 꽂힐 위치 Transform입니다.

- `Move Duration`
  추천 시작값: `1.0`

- `Move Ease`
  추천 시작값: `InOutCubic`

- `Match Rotation`
  보통 켜는 것을 추천합니다.

- `Match Scale`
  처음엔 꺼둔 뒤, 꼭 필요할 때만 켜는 것을 추천합니다.

Figures

- `Hide Existing Figures On Awake`
  씬에 미리 배치해 둔 피규어 오브젝트를 쓸 경우 켜 두는 것을 추천합니다.

- `Figure Reveal Entries`
  피규어 한 개당 한 항목입니다.

두 가지 방식 중 하나만 사용하세요

- Existing Object 방식
  - `Existing Object`만 채웁니다.
  - `Prefab`, `Spawn Point`는 비웁니다.

- Prefab 생성 방식
  - `Prefab`과 `Spawn Point`를 채웁니다.
  - `Existing Object`는 비웁니다.

Figure Entry(각 원소)

- `Delay`
  등장 전 대기 시간입니다.

- `Appear Duration`
  스케일업 애니메이션 시간입니다.

- `Start Scale Multiplier`
  추천 시작값: `0.01` ~ `0.2`

- `Appear Ease`
  추천 시작값: `OutBack`

Scene

- `Lobby Scene Name`
  보통 `LobbyScene`입니다.

- `Record Stage Clear`
  스테이지 클리어 저장 여부입니다.

- `Trigger Lobby Ending Dialogue`
  로비 복귀 후 특별 엔딩 대사를 띄울지 여부입니다.

- `Stage Id Override`
  비워두면 `NewGameConfig.StageId`를 사용합니다.
  강제로 특정 stageId를 저장하고 싶을 때만 입력합니다.

Timings

- `Initial Delay`
  대사 종료 후 약간의 텀입니다.
  추천: `0.25`

- `Cover Fade Duration`
  책상 정리 전 페이드 인(검은 화면) 시간입니다.
  추천: `0.45`

- `Reveal Fade Duration`
  새 책 생성 후 페이드 아웃(화면 다시 열기) 시간입니다.
  추천: `0.45`

- `Hide Desk Objects Delay`
  화면이 완전히 덮인 뒤 책상 오브젝트를 끄기까지의 짧은 텀입니다.
  추천: `0.1` ~ `0.2`

- `After Book Close Delay`
  책 닫힘 직후 텀입니다.

- `After Look At Shelf Delay`
  카메라 블렌드 + 플레이어 시선 연출이 자리잡을 시간을 줍니다.
  Cinemachine 블렌드가 길면 이 값을 늘리세요.

- `After Insert Delay`
  책 꽂기 후 피규어 등장 전 텀입니다.

- `Final Fade Duration`
  로비 로드 전 마지막 페이드 시간입니다.

- `Lobby Load Delay`
  마지막 페이드가 끝난 뒤 씬 로드 직전 짧은 텀입니다.

추천 적용 순서

1. `Fade Canvas Group` 연결
2. `Objects To Hide On Sequence Start` 채우기
3. `Desk Objects To Hide` 채우기
4. `Book Spawn` 설정
5. `Close Entries`로 닫힘 애니메이션 맞추기
6. `Look At Shelf Camera` / `Other Cameras` 연결
7. 필요하면 `On Look At Shelf`에 연출 연결
8. `Shelf Target` 연결
9. `Figure Reveal Entries` 추가
10. `Play Stage Clear Sequence`로 테스트

빠른 체크리스트

- 결과 UI가 안 꺼질 때
  - `Objects To Hide On Sequence Start`에 결과 루트가 들어갔는지 확인

- 책이 안 생길 때
  - `Book Spawn > Source Transform` 확인
  - `Book Spawn > Closing Book Prefab` 확인

- 책이 안 닫힐 때
  - `Book Spawn > Close Entries`가 비어 있지 않은지 확인
  - `Child Path`가 프리팹 구조와 맞는지 확인

- 카메라가 안 바뀔 때
  - `Look At Shelf Camera` / `Other Cameras` 확인
  - Priority 값 확인

- `NO camera rendering`가 뜰 때
  - 메인 렌더 카메라가 켜져 있는지 확인
  - `CinemachineBrain`이 켜져 있는지 확인
  - 다른 스크립트에서 Cinemachine 카메라를 꺼버리고 있지 않은지 확인
