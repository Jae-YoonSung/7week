StageClearSequenceController 적용 가이드

이 스크립트가 하는 일

1. 승리 대사가 끝나면 클리어 연출을 자동 시작합니다.
2. 결과 패널과 continue 안내 같은 UI를 즉시 끕니다.
3. 책상 위에 남아 있던 열린 책과 오브젝트를 숨깁니다.
4. 같은 위치에 닫히는 연출용 책을 생성하고 책 닫기 애니메이션을 실행합니다.
5. Stand Up 카메라로 전환합니다.
6. Bridge To Shelf 카메라로 전환합니다.
7. Bridge To Shelf 카메라에서 Look At Shelf 카메라로 넘어가는 동안, 카메라에 아주 단순한 상하 헤드무빙만 넣습니다.
8. Look At Shelf 카메라가 자리 잡은 뒤 책이 대기 위치를 거쳐 책장 위치로 이동합니다.
9. 책이 책장에 꽂히면 책의 색상이 서서히 지정된 색상으로 바뀝니다.
10. 피규어를 순차적으로 등장시킵니다.
11. 화면이 점점 어두워지며 로비 씬으로 돌아갑니다.

중요한 정리

- 시작할 때 화면 페이드는 없습니다.
- 끝날 때만 화면이 점점 어두워집니다.
- 책이 꽂히고 난 후 머티리얼의 색상을 직접 부드럽게 바꿉니다.
- 헤드무빙은 Bridge To Shelf 구간에서 위아래 이동만 합니다.
- 좌우 흔들림, 롤, 회전 보정 같은 건 없습니다.

권장 연결 순서

1. 빈 GameObject를 만들고 `StageClearSequenceController`를 붙입니다.
2. `Fade Canvas Group`에 화면 전체를 덮는 검은 이미지의 CanvasGroup을 연결합니다.
3. `Objects To Hide On Sequence Start`에 결과 패널, 결과 텍스트, continue 텍스트 같은 UI를 넣습니다.
4. `Desk Objects To Hide`에 책상 위 열린 책과 그 위 오브젝트를 넣습니다.
5. `Book` 항목들을 채웁니다.
6. `Cinemachine 3` 항목들을 채웁니다.
7. 필요하면 `On Stand Up`, `On Bridge To Shelf Camera`, `On Look At Shelf` 이벤트를 연결합니다.
8. `Figures` 항목들을 채웁니다.
9. `Scene` 항목을 확인합니다.
10. 인스펙터 우상단 메뉴나 컴포넌트 우클릭 메뉴의 `Play Stage Clear Sequence`로 테스트합니다.

각 인스펙터 설명

Start

- `Auto Play On Win`
  승리 후 대사가 끝났을 때 자동 시작할지 여부입니다.
  보통 켜 두면 됩니다.

- `Fade Canvas Group`
  마지막 어두워지는 연출에 사용할 CanvasGroup입니다.
  전체 화면을 덮는 검은 이미지 오브젝트에 붙은 CanvasGroup을 연결하면 됩니다.

Hide On Sequence Start

- `Objects To Hide On Sequence Start`
  클리어 루프가 시작되자마자 바로 꺼야 하는 것들을 넣습니다.
  예시:
  `FinalDecisionUI` 루트, 결과 텍스트, continue 텍스트, 결과 배경 패널

- `Hide Runtime Character Objects`
  씬 안에서 이름이 `Character`로 시작하는 런타임 말 오브젝트를 자동으로 꺼 줍니다.
  책 위에 생성되는 말들을 따로 배열에 넣지 않아도 되게 하려고 만든 옵션입니다.
  보통 켜 두면 됩니다.

Hide During Covered Fade

- `Desk Objects To Hide`
  책상 위에서 사라져야 하는 열린 책과 그 위 오브젝트들을 넣습니다.
  연출용으로 새 책을 생성할 때 기존 오브젝트가 겹쳐 보이지 않게 하기 위한 목록입니다.

Book

- `Book Spawn > Source Transform`
  새 닫히는 책을 생성할 기준 위치입니다.
  기존 열린 책 자리의 기준 Transform을 넣으면 됩니다.

- `Book Spawn > Closing Book Prefab`
  연출 시작 후 생성될 책 프리팹입니다.
  책 닫기, 대기 위치 이동, 책장 이동까지 이 프리팹이 담당합니다.
  보통 색 없는 책 프리팹을 여기에 넣습니다.

- `Book Spawn > Parent`
  생성된 책의 부모입니다.
  비워 두면 `Source Transform`의 부모 아래에 생성됩니다.

- `Book Spawn > Position Offset`
  생성 위치를 조금 보정하고 싶을 때 씁니다.

- `Book Spawn > Rotation Offset`
  생성 회전을 조금 보정하고 싶을 때 씁니다.

- `Book Spawn > Spawn Scale`
  생성 시 강제로 적용할 스케일입니다.
  `(0, 0, 0)`이면 프리팹 원래 스케일을 그대로 씁니다.

- `Book Spawn > Close Entries`
  책 닫기 애니메이션 정의입니다.
  `TitleBookAnimator`처럼 특정 자식의 로컬 Z 회전을 바꾸는 방식입니다.

Close Entry

- `Child Path`
  회전시킬 자식 Transform 경로입니다.
  비워 두면 생성된 책 루트가 회전합니다.

- `Rotation Z`
  닫힌 상태로 가기 위한 목표 로컬 Z 회전값입니다.

- `Rotate Mode`
  보통 `Fast`면 충분합니다.

- `Duration`
  회전 시간입니다.

- `Delay`
  시작 전 대기 시간입니다.

- `Ease`
  회전 이징입니다.

Cinemachine 3

- `Stand Up Camera`
  플레이어가 자리에서 일어나는 연출용 카메라입니다.

- `Stand Up Camera Priority`
  위 카메라가 활성화될 때 줄 Priority입니다.

- `Bridge To Shelf Camera`
  Stand Up 카메라와 책장 카메라 사이를 이어 주는 중간 카메라입니다.

- `Bridge To Shelf Camera Priority`
  위 카메라가 활성화될 때 줄 Priority입니다.

- `Look At Shelf Camera`
  최종적으로 책장을 바라보는 카메라입니다.

- `Look At Shelf Camera Priority`
  위 카메라가 활성화될 때 줄 Priority입니다.

- `Other Cameras`
  평소 씬에서 사용하는 다른 `CinemachineCamera`들을 넣습니다.
  클리어 연출 중에는 이 카메라들의 Priority를 낮춥니다.

- `Other Camera Priority`
  다른 카메라들에 줄 낮은 Priority 값입니다.
  보통 `0`이면 충분합니다.

주의

- 메인 렌더 카메라는 끄지 말고, `CinemachineBrain`도 켠 상태로 두세요.
- 카메라 GameObject를 켜고 끄기보다 Priority 전환으로 처리하는 것이 안전합니다.

Stand Up Event

- `On Stand Up`
  자리에서 일어나는 애니메이션, 의자 사운드, 플레이어 반응 등을 연결하면 됩니다.

Bridge Camera Event

- `On Bridge To Shelf Camera`
  중간 카메라로 넘어갔을 때 실행할 이벤트입니다.
  필요 없으면 비워 둬도 됩니다.

Bridge To Shelf Head Bob

- `Target`
  상하 헤드무빙을 적용할 Transform입니다.
  비워 두면 `Bridge To Shelf Camera`의 Transform을 사용합니다.
  가능하면 카메라 자체보다 하위 pivot을 하나 두고 그 pivot을 넣는 편이 더 안전합니다.

- `Step Duration`
  위로 올라가고 내려오는 반 박자 시간입니다.

- `Vertical Offset`
  위아래 이동 폭입니다.
  멀미가 나면 이 값을 더 줄이면 됩니다.

- `Ease`
  상하 이동의 부드러움입니다.
  보통 `InOutSine`이 무난합니다.

- `Reset Duration`
  헤드무빙이 끝날 때 원래 자리로 돌아가는 시간입니다.

중요

- 이 헤드무빙은 위아래 이동만 합니다.
- 회전은 전혀 넣지 않습니다.
- Look At Shelf 카메라 전환이 끝나면 자동으로 멈춥니다.

Shelf Look Event

- `On Look At Shelf`
  최종 책장 카메라로 전환될 때 실행할 이벤트입니다.
  플레이어가 책장을 바라보는 애니메이션이나 사운드를 넣으면 됩니다.

Shelf Camera Rotation

- `Enable`
  Look At Shelf 카메라에 도착한 후 카메라를 추가로 회전시킬지 여부입니다.

- `Rotation Offset`
  현재 카메라 회전값을 기준으로 얼마나 더 회전할지(상대값)를 정합니다.

- `Duration`
  회전에 걸리는 시간입니다.

- `Ease`
  회전의 부드러움(Ease)입니다.

- `Wait For Completion`
  카메라 회전이 완전히 끝난 뒤에야 다음 연출(책 꽂기 등)을 시작할지 여부입니다.
  끄면 카메라가 회전하는 동시에 책이 날아갑니다.

Figures

- `Hide Existing Figures On Awake`
  미리 씬에 배치된 피규어 오브젝트를 시작 시 숨길지 여부입니다.
  기존 오브젝트를 켰다가 등장시키는 방식이면 보통 켜 둡니다.

- `Figure Reveal Entries`
  피규어 등장 목록입니다.

Figure Reveal Entry 사용법

- 기존 오브젝트를 켜는 방식:
  `Existing Object`만 넣고, `Prefab`과 `Spawn Point`는 비워 둡니다.

- 프리팹을 생성하는 방식:
  `Prefab`과 `Spawn Point`를 넣고, `Existing Object`는 비워 둡니다.

- 공통 항목:
  `Delay`, `Appear Duration`, `Start Scale Multiplier`, `Appear Ease`

Scene

- `Lobby Scene Name`
  마지막에 돌아갈 로비 씬 이름입니다.

- `Record Stage Clear`
  스테이지 클리어 기록을 저장할지 여부입니다.

- `Trigger Lobby Ending Dialogue`
  로비 복귀 후 엔딩 대사를 이어서 실행할지 여부입니다.

- `Stage Id Override`
  비워 두면 현재 `NewGameConfig.StageId`를 사용합니다.
  특정 스테이지만 강제로 기록하고 싶을 때만 넣습니다.

Timings

- `Initial Delay`
  승리 대사 종료 후 연출 시작 전 잠깐 쉬는 시간입니다.

- `Hide Desk Objects Delay`
  결과 UI가 꺼진 뒤 책상 위 오브젝트를 숨기기 전까지의 짧은 대기입니다.

- `After Book Close Delay`
  책 닫기 후 다음 단계로 넘어가기 전 대기입니다.

- `After Stand Up Delay`
  Stand Up 카메라 전환 후 기다리는 시간입니다.

- `After Bridge Camera Delay`
  Bridge To Shelf 카메라 전환 후 기다리는 시간입니다.

- `Wait For Shelf Camera Transition`
  Look At Shelf 카메라로 전환된 뒤, 카메라가 안정될 때까지 기다리는 시간입니다.
  이 시간이 끝난 뒤에야 책이 대기 위치로 이동하기 시작합니다.

- `After Insert Delay`
  책이 책장에 꽂히고 색 있는 책으로 교체된 뒤 다음 단계로 넘어가기 전 대기입니다.

- `Final Fade Duration`
  마지막 어두워지는 시간입니다.

- `Lobby Load Delay`
  마지막 페이드가 끝난 뒤 씬을 바꾸기 전 짧은 대기입니다.

Book Insert 설정

- `Staging Target`
  책이 책장으로 가기 전에 먼저 잠깐 머무는 대기 위치입니다.

- `Staging Move Duration`
  시작 위치에서 대기 위치까지 가는 시간입니다.

- `Staging Move Ease`
  위 이동의 Ease입니다.

- `Match Staging Rotation`
  대기 위치에 도착할 때 회전도 맞출지 여부입니다.

- `Match Staging Scale`
  대기 위치에 도착할 때 스케일까지 맞출지 여부입니다.

- `Staging Swap Prefab`
  대기 위치(Staging Target)에 도착했을 때 중간에 교체할 책 프리팹입니다.
  비워두면 교체하지 않고 기존 책이 그대로 책장까지 이동합니다.

- `Shelf Target`
  최종적으로 책이 꽂힐 책장 위치입니다.

- `Move Duration`
  대기 위치에서 책장 위치까지 가는 시간입니다.

- `Move Ease`
  위 이동의 Ease입니다.

- `Match Rotation`
  책장 위치 도착 시 회전을 맞출지 여부입니다.

- `Match Scale`
  책장 위치 도착 시 스케일까지 맞출지 여부입니다.

파티클을 붙이고 싶을 때

- 현재 코드는 책이 책장에 꽂힌 뒤 색상이 서서히 바뀌는 구조입니다.
- 파티클을 추가하고 싶다면, 코드를 수정하여 색 변환 시작 직전에 파티클 프리팹을 Instantiate 하는 방식을 추천합니다.

빠른 체크리스트

- 결과 UI가 바로 꺼지지 않으면:
  `Objects To Hide On Sequence Start`에 해당 루트가 들어갔는지 확인

- 책상 위 말이 남아 있으면:
  이름이 `Character`로 시작하는지 확인
  `Hide Runtime Character Objects`가 켜져 있는지 확인

- 책이 생성되지 않으면:
  `Book Spawn > Source Transform`
  `Book Spawn > Closing Book Prefab`
  위 두 칸을 확인

- 책이 닫히지 않으면:
  `Close Entries`가 비어 있지 않은지 확인
  `Child Path`가 실제 프리팹 구조와 맞는지 확인

- 카메라가 이상하게 튀면:
  `Bridge To Shelf Head Bob > Target`에 별도 pivot을 넣어 보기
  `Vertical Offset`을 더 줄이기
  `Wait For Shelf Camera Transition` 값을 조금 늘리기

- `NO camera rendering` 오류가 나면:
  메인 렌더 카메라가 꺼지지 않았는지 확인
  `CinemachineBrain`이 켜져 있는지 확인
  다른 스크립트가 카메라 GameObject를 직접 끄지 않는지 확인
