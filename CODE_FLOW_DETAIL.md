# Hanam_MC 전체 코드 흐름 상세 문서

> **마음 필름 감독** - 교육/치유 앱
> Unity URP 17.0.4 | LiteDB | BCrypt | DOTween | Whisper.cpp
> 10개 문제 × 3~4 스텝 | Director / Gardener 테마

---

## 처음 보는 사람을 위한 3줄 요약

**코드를 처음 받으면 이 순서대로 읽으면 된다:**
1. `Bootstrap` → `DataService` → `SessionManager` → `SceneNavigator` — 앱이 켜지고, DB가 열리고, 세션이 복원되고, 씬이 전환되는 흐름
2. `ProblemSceneController` → `StepFlowController` → `ProblemStepBase` → `DialogueSequencer` — 문제 씬에서 스텝이 순차 진행되고, 대사가 나오고, 사용자 입력을 받는 핵심 루프
3. `CommonRewardStep` → `DataService.Progress/Reward` → `GameManager.GoToHome()` — 문제 완료 → DB 저장 → 홈 복귀까지의 마무리 흐름

---

## 수정 위험 구간 (Impact Map)

아래 클래스는 **변경 시 영향 범위가 크다.** 수정 전 반드시 영향도를 확인할 것.

| 클래스 | 위험도 | 영향 범위 | 비고 |
|--------|--------|-----------|------|
| `StepFlowController` | **최상** | 전체 문제 (10개 × 3~4스텝) | 모든 문제의 스텝 진행 엔진. GoToStep/NextStep 변경 시 전체 문제 흐름 깨짐 |
| `ProblemStepBase` | **최상** | 전체 스텝 (30+개) | 모든 스텝의 추상 베이스. SaveAttempt/OnStepEnter 시그니처 변경 시 전 스텝 수정 필요 |
| `DialogueSequencer` | **최상** | 모든 대사 표시 스텝 | enterTextIds/completedTextIds 흐름 변경 시 모든 문제의 대사 타이밍 영향 |
| `CommonRewardStep` | **상** | 모든 문제의 마지막 스텝 | 보상 저장 + 문제 완료 처리. MarkProblemSolved() 변경 시 진행 데이터 깨짐 |
| `DataService` | **상** | 전체 앱 데이터 | Composition Root. 서비스/Repository 생성 순서 변경 시 NullRef 폭탄 |
| `SessionManager` | **상** | 인증 + 씬 라우팅 | SignIn/SignOut/TryRestore 변경 시 자동 로그인, 세션 유지 깨짐 |
| `SceneNavigator` | **상** | 전체 씬 전환 | IsAllowed 로직 변경 시 미인증 사용자 접근 가능 (보안 이슈) |
| `StepCompletionGate` | **중상** | 모든 게이트 사용 스텝 | MarkOneDone/Apply 변경 시 완료 조건 판정 오류 |
| `MultipleChoiceStepBase` | **중** | P1 Step3, P3 Step3 등 | 객관식 문제 공통 베이스. 변경 시 해당 문제들만 영향 |
| `InventoryDropTargetStepBase` | **중** | P2~P10 Step1 | 드롭 스텝 공통 베이스. 변경 시 10개 문제의 Step1 영향 |

---

## 최소 테스트 동선 (Smoke Test Checklist)

인수인계 후 또는 코드 수정 후, 아래 순서대로 검증하면 핵심 흐름을 확인할 수 있다.

- [ ] **1. 회원가입** — RegisterScene에서 이름/이메일/비밀번호 입력 → "회원가입 완료" 메시지 → 로그인 탭 전환
- [ ] **2. 로그인** — 가입한 계정으로 로그인 → HomeScene 진입 확인
- [ ] **3. 앱 재시작 시 자동 로그인** — 앱 종료 후 재실행 → 로그인 화면 없이 HomeScene 직행
- [ ] **4. 홈에서 문제 진입** — Director 테마 선택 → Problem 1 클릭 → ProblemScene 로드 + StartStep 표시
- [ ] **5. 스텝 순차 진행** — "시작" 클릭 → Step2 진입 → 대사(enterTextIds) 정상 표시 → 상호작용 잠금 해제
- [ ] **6. 문제 풀이 + DB 저장** — Step2/Step3에서 문제 풀이 → CompletionGate 완료 → completedTextIds 재생 → NextStepBtn 표시
- [ ] **7. 보상 스텝** — CommonRewardStep 진입 → 보상 애니메이션 → "홈으로" 버튼 표시
- [ ] **8. 문제 완료 + 홈 복귀** — "홈으로" 클릭 → MarkProblemSolved() DB 저장 → HomeScene 복귀 → 다음 문제 잠금 해제 확인
- [ ] **9. 인벤토리 확인** — 보상 아이템이 DB에 저장되었는지 확인 (다음 문제 Step1에서 드롭 가능)
- [ ] **10. 로그아웃** — 관리자 패널(F1) 또는 홈 화면에서 로그아웃 → RegisterScene 이동 + 세션 클리어

---

## Known Issues & 한계 (정직한 현황)

### 구조적 한계

1. **Binder/Logic 패턴 일관성 차이**
   - P1~P6는 Logic이 ProblemStepBase를 직접 상속하고 Binder가 프로퍼티만 구현하는 구조
   - P7~P10은 같은 패턴이지만 코드 스타일(#region 블록 등)이 뒤에 만든 것이라 더 정돈됨
   - 기능적 차이는 없으나, 코드 스타일이 앞뒤 불일치할 수 있음

2. **Awake() 금지 규칙은 관습적**
   - 언어 레벨 강제가 아니라 규칙으로만 존재. 새 개발자가 Awake()를 쓰면 스텝 활성화 순서 버그 발생 가능
   - 린터나 커스텀 Inspector 경고는 미구현

3. **SceneNavigator 인증 체크가 클라이언트 단**
   - IsAllowed()가 SessionManager.IsSignedIn만 확인 → 서버 없이 로컬 DB 기반이라 보안은 앱 수준
   - 서버 연동 시 토큰 기반 인증으로 교체 필요

### 데이터 관련

4. **CSV 데이터테이블이 100% 매핑되지 않은 부분 존재**
   - 기획 변경으로 일부 enterTextIds/completedTextIds가 placeholder이거나 미할당 상태일 수 있음
   - 기획자 변경 이력이 있어서 텍스트 ID 매핑은 최종 확인 필요

5. **문제 풀이 로그(Attempt)는 즉시 DB 저장**
   - 배치 저장이 아니라 스텝 완료 시 바로 InsertAttempt() 호출
   - 대량 데이터 시 성능 이슈 가능성 있으나, 현재 규모(10문제 × 유저 수)에서는 문제 없음

6. **InventoryRepository.Add()에 스키마 안전장치 존재**
   - InvalidCastException 발생 시 컬렉션을 드롭하고 재생성하는 방어 로직이 있음
   - DB 스키마 변경 시 기존 데이터가 날아갈 수 있으므로 주의

### STT 관련

7. **STT는 키워드 매칭 기반 (자유 발화 아님)**
   - Whisper.cpp가 음성을 텍스트로 변환한 후, MicRecordingIndicator가 사전 설정된 키워드와 비교
   - 사용자가 정확한 키워드를 말해야 매칭됨. 유사 표현이나 자유 대화는 미지원
   - `SetPromptHint()`로 Whisper에 키워드 힌트를 주어 인식률을 보정하지만, 100%는 아님

8. **Whisper 모델은 ggml-tiny (경량)**
   - 정확도보다 속도/메모리를 우선한 선택. 긴 문장이나 잡음 환경에서 인식률 저하 가능
   - 모바일 배포 시 모델 파일 크기(~75MB) 고려 필요

### 미구현/미완성

9. **Gardener 테마는 Director와 구조만 동일하고 콘텐츠 미완성 가능**
   - ProblemSceneController에 gardenerRoot가 있지만, 실제 Problem 구현은 Director 위주
   - 테마 확장 시 같은 Binder/Logic 패턴으로 추가하면 됨

10. **ResultScene은 관리자 전용**
    - 일반 사용자는 HOME으로만 라우팅됨. ADMIN/SUPERADMIN만 RESULT 씬 접근
    - 사용자용 결과 화면은 별도 구현 필요

---

## 목차

1. [앱 부트스트랩 (Bootstrap)](#1-앱-부트스트랩)
2. [데이터 계층 (DataService / Repository / DB)](#2-데이터-계층)
3. [인증 & 세션 (Auth / Session)](#3-인증--세션)
4. [씬 전환 (SceneNavigator)](#4-씬-전환)
5. [회원가입/로그인 (RegisterScene)](#5-회원가입로그인)
6. [홈 화면 (HomeScene)](#6-홈-화면)
7. [문제 씬 코어 (ProblemScene Core)](#7-문제-씬-코어)
8. [스텝 베이스 클래스 계층](#8-스텝-베이스-클래스-계층)
9. [대사 시스템 (DialogueSequencer)](#9-대사-시스템)
10. [이펙트 시스템](#10-이펙트-시스템)
11. [STT 시스템 (Whisper)](#11-stt-시스템)
12. [사운드 시스템 (SoundManager)](#12-사운드-시스템)
13. [Problem 1~10 상세](#13-problem-110-상세)
14. [전체 콜체인 예시](#14-전체-콜체인-예시)
15. [핵심 규칙 & 주의사항](#15-핵심-규칙--주의사항)

---

## 1. 앱 부트스트랩

### Bootstrap.cs (`Assets/01. Script/Bootstrap.cs`)

앱의 진입점. 첫 씬(Bootstrap Scene)에서 자동 실행된다.

#### Awake() — 앱 초기화
```
1. static s_Initialized 플래그로 중복 실행 방지
2. I = this (싱글톤)
3. DontDestroyOnLoad(gameObject)
4. DatabaseInitializer.InitializeIndexes()  ← DB 인덱스 생성
5. LoadTables()                              ← CSV 로드
6. StartCoroutine(InitRoutine())            ← 씬 라우팅
```

#### LoadTables()
- `Resources/CSV/MC_DataTable_v01.csv`를 TextAsset으로 로드
- `LocalizedTable`로 파싱 → `Bootstrap.I.Localized`에 저장
- 이후 `ProblemRuntime.L(textId)`로 접근

#### InitRoutine() 코루틴
```
yield return WaitUntil(() => SceneNavigator.Instance != null)
if (SessionManager.Instance.IsSignedIn)
    → SceneNavigator.GoTo(HOME)    // 자동 로그인
else
    → SceneNavigator.GoTo(REGISTER) // 로그인 화면
```

### DatabaseInitializer.cs

#### InitializeIndexes() — DB 인덱스 일괄 생성
- `static _initialized` 플래그로 1회만 실행
- LiteDB 7개 컬렉션에 인덱스 생성:

| 컬렉션 | 인덱스 |
|---------|--------|
| users | Id(UNIQUE), Email(UNIQUE), Role, Name, LowerName, NameChosung |
| problems | Id(UNIQUE), Theme, Index |
| results | Id(UNIQUE), UserId, Theme, ProblemIndex |
| attempts | Id(UNIQUE), UserId, UserEmail, ProblemId, Theme |
| progress | UserEmail(UNIQUE) |
| inventory | Id(UNIQUE), UserId, UserEmail, ItemId |
| sessions | Id(UNIQUE), UserId, UserEmail |

---

## 2. 데이터 계층

### DB 파일 위치 및 확인 방법

**DB 파일 경로:**
```
C:\Users\{사용자명}\AppData\LocalLow\DefaultCompany\Hanam_MC\mc.db
```
> `Application.persistentDataPath` 기반. 빌드 시 CompanyName/ProductName 변경하면 경로도 바뀜.

**DB 파일 확인 방법:**

LiteDB는 단일 파일 NoSQL DB이므로 일반 텍스트 에디터로는 열 수 없다. 아래 도구를 사용:

1. **LiteDB Studio** (공식 GUI 도구, 추천)
   - https://github.com/mbdavid/LiteDB.Studio/releases
   - 다운로드 후 실행 → `mc.db` 파일 열기 → SQL 쿼리로 데이터 조회 가능
   - 예시 쿼리:
     ```sql
     SELECT $ FROM users              -- 전체 사용자 조회
     SELECT $ FROM results            -- 문제 완료 기록
     SELECT $ FROM attempts           -- 풀이 로그
     SELECT $ FROM inventory          -- 보상 아이템
     SELECT COUNT(*) FROM attempts    -- 풀이 횟수
     ```

2. **LiteDB Shell** (CLI)
   - NuGet에서 `LiteDB` 패키지 설치 후 C# 코드로 직접 조회

> **주의:** 앱 실행 중에는 DB 파일이 잠길 수 있으므로, Play 모드를 종료한 후 열 것.

### 아키텍처 다이어그램
```
DataService (싱글톤, Composition Root)
  ├─ Auth        : AuthService       → UserRepository       → DBGateway → LiteDB
  ├─ Progress    : LocalProgressService → ProgressRepository, ResultRepository
  ├─ Reward      : LocalRewardService   → InventoryRepository, UserRepository
  ├─ Problems    : LocalProblemQueryService → ProblemRepository
  ├─ Results     : LocalResultQueryService  → ResultRepository
  └─ Admin       : LocalAdminDataService    → UserRepository, ResultRepository, FeedbackRepository
```

### DataService.cs (`Assets/01. Script/Data/DataService.cs`)

#### Awake() — Composition Root
```csharp
// Phase 1: DB 게이트웨이
Db = new DBGateway()

// Phase 2: Repository 생성 (6개)
InventoryRepository(dbCore)
UserRepository(dbCore)
ProgressRepository(dbCore)
ProblemRepository(dbCore)
ResultRepository(dbCore)
FeedbackRepository(dbCore)

// Phase 3: Service 생성 (6개)
Auth = new AuthService(UserRepository)
  └─ EnsureSuperAdmin() 호출 → 기본 관리자 계정 생성

Progress = new LocalProgressService(ProgressRepository, UserRepository, ResultRepository)
Reward = new LocalRewardService(InventoryRepository, UserRepository, Progress)
Problems = new LocalProblemQueryService(ProblemRepository)
Results = new LocalResultQueryService(ResultRepository)
Admin = new LocalAdminDataService(UserRepository, ResultRepository, FeedbackRepository)
```

### DBGateway / DBHelper — Loan 패턴

```csharp
// 모든 DB 접근은 이 패턴을 사용
_db.WithDb(db => {
    var col = db.GetCollection<Model>("collectionName");
    col.EnsureIndex(x => x.Id, true);
    return col.FindOne(query);
});
// using 블록으로 자동 Dispose → 리소스 누수 방지
```

- **DBHelper.DBPath**: `Application.persistentDataPath/mc.db`
- **DBHelper.With<T>(func)**: LiteDatabase 열기 → func 실행 → 자동 닫기

### Repository 주요 메서드

#### UserRepository
| 메서드 | 호출자 | 설명 |
|--------|--------|------|
| `ExistsEmail(email)` | AuthService.SignUp | 이메일 중복 확인 |
| `FindActiveUserByEmail(email)` | AuthService.Login, RewardService | 활성 사용자 조회 |
| `InsertUser(user)` | AuthService.SignUp | 사용자 등록 |
| `UpdateUser(user)` | AdminService | 사용자 수정 |
| `HasSuperAdmin()` | AuthService.EnsureSuperAdmin | 관리자 존재 확인 |

#### ProgressRepository
| 메서드 | 호출자 | 설명 |
|--------|--------|------|
| `InsertAttempt(attempt)` | LocalProgressService | 문제 풀이 로그 저장 |
| `GetSolvedProblemIndexes(email, theme)` | LocalProgressService | 풀었던 문제 인덱스 조회 |

#### ResultRepository
| 메서드 | 호출자 | 설명 |
|--------|--------|------|
| `InsertResult(result)` | LocalProgressService | 문제 완료 기록 저장 |
| `GetResultsByUser(email)` | LocalResultQueryService | 사용자 결과 조회 |

#### InventoryRepository
| 메서드 | 호출자 | 설명 |
|--------|--------|------|
| `Add(item)` | LocalRewardService | 인벤토리 아이템 추가 |
| `HasItem(email, itemId)` | InventoryDropTargetStepBase | 아이템 보유 확인 |
| `GetByUser(email)` | LocalRewardService | 사용자 인벤토리 전체 조회 |

### Service 주요 메서드

#### LocalProgressService
| 메서드 | 호출자 | 설명 |
|--------|--------|------|
| `SaveStepAttemptForCurrentUser(theme, index, id, payload)` | ProblemStepBase.SaveAttempt() | 풀이 로그를 attempts 컬렉션에 저장 |
| `MarkProblemSolvedForCurrentUser(theme, index)` | CommonRewardStep.MarkProblemSolved() | 문제 완료를 results 컬렉션에 저장 (중복 방지) |
| `FetchSolvedProblemIndexes(email, theme)` | ThemePanelsController | 풀었던 문제 목록 조회 |

#### LocalRewardService
| 메서드 | 호출자 | 설명 |
|--------|--------|------|
| `SaveRewardForCurrentUser(theme, index, id, payload, itemId, itemName)` | ProblemStepBase.SaveReward() | Attempt 저장 + 인벤토리 아이템 지급 |
| `GrantInventoryItem(email, item)` | SaveRewardForCurrentUser | 인벤토리에 아이템 추가 |
| `GetInventory(email)` | StepInventory, InventoryDropTargetStepBase | 보유 아이템 목록 조회 |

### Result<T> 패턴
```csharp
// 모든 서비스는 Result 또는 Result<T>를 반환 (예외를 던지지 않음)
var result = auth.Login(email, password);
if (result.Ok)
    user = result.Value;  // 성공 시 값 접근
else
    error = result.Error; // AuthError 열거형
```

---

## 3. 인증 & 세션

### AuthService.cs (`Assets/01. Script/Service/AuthService.cs`)

#### EnsureSuperAdmin() — 기본 관리자 생성
```
AuthConfig에서 DefaultAdminEmail ("admin@local") 로드
→ HasSuperAdmin() 체크
→ 없으면 BCrypt.HashPassword(password, workFactor=10)로 해시
→ InsertUser(superAdmin)
```

#### Login(email, password) → Result<User>
```
1. NormalizeEmail(email)           → 소문자 변환 + 트림
2. IsValidEmail(email)             → 정규식 검증
3. FindActiveUserByEmail(email)    → DB 조회 (IsActive=true)
4. BCrypt.Verify(password, hash)   → 비밀번호 검증
5. 성공 시 Result<User>.Success(user) 반환
```

#### SignUp(name, email, password) → Result
```
1. 입력 검증: name 빈값, email 형식, password 강도
2. ExistsEmail(email) → 중복 확인
3. BCrypt.HashPassword(password, workFactor=10)
4. InsertUser(newUser) → DB 저장
```

### AuthValidator.cs
- `IsValidEmail(email)`: 정규식 `^[A-Za-z0-9._%+\-]+@[A-Za-z0-9.\-]+\.[A-Za-z]{2,}$`
- `IsStrongPassword(pw)`: 최소 6자 + 문자 + 숫자
- `NormalizeEmail(email)`: trim + lowercase

### SessionManager.cs (`Assets/01. Script/SessionManager.cs`)

#### 싱글톤 + DontDestroyOnLoad

#### SignIn(user, sessionId)
- **호출자**: LoginController (로그인 성공 후)
```
_currentUser = user
SessionId = Guid.NewGuid()  (또는 전달받은 값)
Save() → PlayerPrefs에 저장:
  - "session.user" : JSON(UserSnapshot{Name, Email, Role, IsActive})
  - "session.id"   : SessionId 문자열
OnChanged?.Invoke()
```

#### SignOut()
- **호출자**: GameManager.Logout()
```
_currentUser = null
SessionId = null
Clear() → PlayerPrefs 키 삭제
OnChanged?.Invoke()
```

#### TryRestore() — 앱 재시작 시 세션 복원
```
PlayerPrefs에서 "session.user" 읽기
→ JSON 역직렬화 → UserSnapshot
→ User 객체 재구성 (PasswordHash 없음!)
→ SessionId 복원
→ IsSignedIn = true
```

---

## 4. 씬 전환

### SceneNavigator.cs (`Assets/01. Script/Core/SceneNavigator.cs`)

#### ScreenId 열거형
| ScreenId | 씬 이름 |
|----------|---------|
| REGISTER | RegisterScene |
| HOME | HomeScene |
| PROBLEM | ProblemScene |
| RESULT | ResultScene |

#### GoTo(ScreenId id) — 씬 이동
```
CoGoTo(id) 코루틴 시작:
  1. IsAllowed(id) → 인증 체크
     - HOME, PROBLEM, RESULT는 IsSignedIn 필수
     - 미인증 시 id = REGISTER로 강제 변경
  2. Fade(1f) → 화면 어둡게 (CanvasGroup.alpha → 1)
  3. SceneManager.LoadSceneAsync(SceneName) → 씬 로드
  4. history.Push(current) → 이전 씬 기록
  5. current = id
  6. Fade(0f) → 화면 밝게 (CanvasGroup.alpha → 0)
```

#### GoBack() — 이전 씬으로
```
if (history.Count > 0)
    GoTo(history.Pop())
```

### GameManager.cs (`Assets/01. Script/Core/GameManager.cs`)

| 메서드 | 호출자 | 동작 |
|--------|--------|------|
| `GoToHome()` | CommonRewardStep, ProblemSceneController | SceneNavigator.GoTo(HOME) |
| `GoToScene(ScreenId)` | 범용 씬 이동 | SceneNavigator.GoTo(id) |
| `Logout()` | AdminPanel, HomeSceneManager | SessionManager.SignOut() → SceneNavigator.GoTo(REGISTER) |
| `QuitApplication()` | AdminPanel | Application.Quit() (에디터: EditorApplication.isPlaying = false) |

---

## 5. 회원가입/로그인

### RegisterScene 아키텍처
```
RegisterScene
  ├─ RegisterTabsController (탭 전환: Login ↔ Signup)
  ├─ LoginController + LoginFormUI
  └─ SignupController + SignupFormUI
```

### LoginController
#### HandleLogin(email, password)
```
1. 빈값 체크
2. UI 상태: "로그인 중..."
3. auth.Login(email, password)
4. 성공:
   → SessionManager.SignIn(user)
   → 역할 분기:
     - ADMIN/SUPERADMIN → SceneNavigator.GoTo(RESULT)
     - USER → SceneNavigator.GoTo(HOME)
5. 실패:
   → UI 입력 재활성화
   → 에러 메시지 표시
```

### SignupController
#### HandleCheckEmail(email) — 실시간 이메일 중복 체크
```
1. IsValidEmail(email) → 실패 시 "이메일 형식이 올바르지 않습니다." (빨강)
2. auth.Exists(email)
   → 존재: "이미 사용 중인 이메일입니다." (빨강)
   → 가용: "사용 가능한 이메일입니다." (파랑)
```

#### HandleSignup(name, email, password)
```
1. 입력 검증 (name, email, password)
2. auth.SignUp(name, email, password)
3. 성공: "회원가입이 완료되었습니다." → tabs.ShowLogin()
4. 실패: 에러 메시지 표시
```

#### HandlePasswordChanged(pw) — 실시간 비밀번호 강도
```
빈값: 힌트 없음
강함: "안전한 비밀번호입니다." (파랑)
약함: "최소 6자, 문자+숫자를 포함해야 합니다." (빨강)
```

---

## 6. 홈 화면

### HomeScene 아키텍처
```
HomeScene
  ├─ ThemePanelsController (문제 선택, 진행률)
  ├─ HomeSceneManager (로그아웃, 종료, 옵션)
  ├─ HomeSceneUI (사용자 정보 표시)
  └─ AdminPanel (F1 관리자 패널)
```

### ProblemSession (static 클래스)
씬 간 문제 컨텍스트를 전달하는 정적 클래스.

```csharp
static class ProblemSession {
    CurrentTheme        // ProblemTheme (Director/Gardener)
    CurrentProblemIndex  // int (1-10)
    CurrentProblemId     // string (GUID)
    ReturnTarget         // HomeReturnTarget (None/LevelSelect/Ending)
    DemoMode             // bool (전체 잠금해제 + 인벤토리 우회)
}
```

### ThemePanelsController

#### Start() — 홈 씬 진입
```
1. SessionManager.CurrentUser 유효성 확인
2. ProblemSession.ReturnTarget 체크:
   - LevelSelect → ShowDirectorLevelSelect() (P1-P9 완료 후 복귀)
   - Ending → ShowDirectorEnding() (P10 완료 후 엔딩)
   - None → ShowThemeSelectPanel() (기본: 테마 선택)
3. ReturnTarget = None으로 초기화
```

#### HandleProblemClicked(theme, index)
```
1. ProblemSession.CurrentTheme = theme
2. ProblemSession.CurrentProblemIndex = index
3. SceneNavigator.GoTo(ScreenId.PROBLEM)
```

#### RefreshSinglePanel(binding) — 문제 잠금/해제 상태 갱신
```
1. Progress.FetchSolvedProblemIndexes(email, theme) → DB 조회
2. 잠금해제 로직:
   - DemoMode 또는 전부 풀었으면: 전체 해제
   - 아니면: 다음 미풀이 문제만 해제
3. panel.ApplyProblemState(unlocked[], solved[])
```

### AdminPanel — 관리자 패널
- **F1**: 패널 토글
- **ESC**: 패널 닫기
- **종료 버튼**: `GameManager.Instance.QuitApplication()`
- **로그인 버튼**: `GameManager.Instance.Logout()`

---

## 7. 문제 씬 코어

### ProblemScene 전체 구조
```
ProblemSceneController (씬 진입점)
  ↓
SetupThemeRoot() → ActivateSingleProblem()
  ↓
Problem_N GameObject (인덱스로 활성화)
  ↓
StepFlowController (스텝 순차 진행)
  ↓
stepPanels[0] → stepPanels[1] → stepPanels[2] → stepPanels[3]
  ↓               ↓               ↓               ↓
StartStep     Step2(Logic)     Step3(Logic)     CommonRewardStep
```

### ProblemSceneController.cs

#### Start()
```
1. DataService.Instance?.Problems 유효성 확인
2. ProblemSession.CurrentProblemIndex 유효성 확인
3. SetupThemeRoot()
   → Director/Gardener 루트 중 하나만 활성화
4. ActivateSingleProblem(index)
   → _activeRoot의 자식 중 index-1번째만 SetActive(true)
```

### StepFlowController.cs

스텝(패널) 순차 진행을 관리하는 핵심 컨트롤러.

#### 필드
```csharp
List<GameObject> stepPanels;     // 스텝 패널 리스트 (순서대로)
bool useSkip;                     // 건너뛰기 기능
int skipTargetStepIndex;          // 건너뛰기 대상 스텝
string bgmClipName;               // BGM 클립명
bool stopBgmOnExit;               // OnDisable 시 BGM 정지
int _currentIndex = -1;           // 현재 스텝 (-1 = 시작 전)
```

#### 메서드 호출 관계

| 메서드 | 호출자 | 동작 |
|--------|--------|------|
| `NextStep()` | StepCompletionGate, AutoNextStepButton, CommonRewardStep, DialogueSequencer(NextStepBtn), StartStep | 다음 스텝으로 이동. 마지막 넘으면 OnFlowFinished() |
| `PrevStep()` | 디버그/사용자 | 이전 스텝으로 (최소 0) |
| `JumpToStep(int)` | ThemePanelsController, 디버그 | 지정 스텝으로 직접 이동 |
| `RestartCurrentStep()` | 디버그 | 현재 스텝 재시작 (OnDisable→OnEnable) |
| `SkipFlow()` | Skip 버튼 | skipTargetStepIndex로 점프 |
| `SetAllInactive()` | OnEnable | 모든 패널 비활성화 |

#### OnEnable() 흐름
```
1. SetAllInactive()                       // 모든 패널 끄기
2. SoundManager.PlayBGM(bgmClipName)     // BGM 재생
3. GoToStep(0)                            // 첫 스텝 활성화
```

#### GoToStep(int index) — 핵심 제어
```csharp
_currentIndex = index;
for (int i = 0; i < stepPanels.Count; i++)
{
    bool active = (i == _currentIndex);
    stepPanels[i].SetActive(active);
    // active=true  → OnEnable → OnStepEnter()
    // active=false → OnDisable → OnStepExit()
}
```

### StepCompletionGate.cs

다중 조건 완료 추적기. 스텝 내 여러 작업(예: 4개 문제 중 4개 정답)의 진행을 추적한다.

#### 필드
```csharp
bool useProgressFill;              // 프로그레스 바 표시
Image progressFillImage;           // fillAmount (0→1)
bool useCompleteRoot = true;       // 버튼 모드 (false면 자동 진행)
GameObject completeRoot;           // "다음" 버튼 컨테이너
StepFlowController stepFlowController;  // 자동 진행 모드용
bool useHideRoot = true;           // 완료 시 UI 숨김
GameObject hideRoot;               // 완료 시 숨길 UI

int _totalCount;                   // 목표 수
int _currentCount;                 // 현재 수
bool _autoNextFired;               // 자동 NextStep 중복 방지
```

#### 메서드

| 메서드 | 호출자 | 동작 |
|--------|--------|------|
| `ResetGate(int total)` | OnStepEnter (각 스텝) | total 설정, current=0, Apply() |
| `MarkOneDone()` | 각 스텝 Logic (정답, 드롭 성공 등) | current++, Apply() |
| `MarkOneUndone()` | 선택 취소 시 | current--, Apply() |

#### Apply() — 상태 갱신
```
1. progress = current / total
2. progressFillImage.fillAmount = progress (선택적)
3. completed = (total > 0 && current >= total)
4. useHideRoot → hideRoot.SetActive(!completed)
5. useCompleteRoot → completeRoot.SetActive(completed)  // 버튼 모드
   또는 !useCompleteRoot → stepFlowController.NextStep()  // 자동 진행
```

---

## 8. 스텝 베이스 클래스 계층

### 상속 구조
```
MonoBehaviour
  └─ ProblemStepBase (추상: OnStepEnter, OnStepExit)
       ├─ MultipleChoiceStepBase<TQuestion>  (4지선다)
       ├─ RandomCardSequenceStepBase          (카드 순차 처리)
       ├─ InventoryDropTargetStepBase         (아이템 드롭)
       ├─ CommonRewardStep                    (보상 표시)
       └─ StartStep                           (문제 인트로)
```

### ProblemStepBase.cs

모든 스텝의 추상 베이스. 라이프사이클 훅과 DB 저장 유틸리티를 제공.

#### 라이프사이클
```
OnEnable() (Unity)
  → OnStepEnter() (추상, 자식 구현)
  → [사용자 상호작용]
OnDisable() (Unity)
  → OnStepExit() (가상, 선택적 오버라이드)
```

#### 핵심 메서드

| 메서드 | 설명 |
|--------|------|
| `OnStepEnter()` | 추상. 자식 클래스에서 UI 초기화, 이벤트 바인딩, 상태 리셋 |
| `OnStepExit()` | 가상. 코루틴 정지, 이벤트 해제, DOTween Kill |
| `BuildStepKey()` | stepKeyConfig + ProblemContext → "Director_P1_Step2" |
| `SaveAttempt(object body)` | body를 JSON 직렬화 → Progress.SaveStepAttemptForCurrentUser() |
| `SaveReward(body, itemId, itemName)` | Reward.SaveRewardForCurrentUser() → 인벤토리 아이템 지급 |
| `L(int textId)` | ProblemRuntime.L(textId) 래핑 → CSV 텍스트 조회 |

#### SaveAttempt() 구현
```csharp
protected void SaveAttempt(object body)
{
    if (!useDBSave || context == null) return;
    string stepKey = BuildStepKey();
    var payload = new { stepKey, theme, problemIndex, body };
    DataService.Instance.Progress.SaveStepAttemptForCurrentUser(
        context.Theme, context.ProblemIndex, context.ProblemId, payload);
}
```

### MultipleChoiceStepBase\<TQuestion\>

N개 객관식 문제를 순차 처리하는 제네릭 베이스.

#### 추상 멤버 (자식 구현)
```csharp
int QuestionCount { get; }                          // 총 문제 수
TQuestion GetQuestion(int index);                    // 문제 데이터
void ApplyQuestionUI(int index, TQuestion q);        // UI 표시
int GetCorrectOptionIndex(TQuestion q);              // 정답 인덱스
void OnQuestionAttempted(TQuestion q, int optionIndex, bool isCorrect);
void OnAllQuestionsCompleted();                      // 전체 완료 콜백
```

#### 흐름
```
OnStepEnter()
  → _currentQuestionIndex = 0
  → completionGate.ResetGate(QuestionCount)
  → ShowQuestion(0)
      → ApplyQuestionUI() → 버튼 리스너 등록

OnClickOption(int optionIndex)
  → GetCorrectOptionIndex(q) 비교
  → 정답: HandleCorrect() → completionGate.MarkOneDone() → GoNextQuestionOrFinish()
  → 오답: HandleWrong() → 오답 색상 표시

GoNextQuestionOrFinish()
  → 다음 문제 있으면: ShowQuestion(++index)
  → 전부 풀었으면: OnAllQuestionsCompleted()
```

### RandomCardSequenceStepBase

N개 카드를 순차적으로 표시하고 처리하는 베이스.

#### 추상 멤버
```csharp
int CardCount { get; }
void OnApplyCardToUI(int logicalIndex);    // 카드 표시
void OnClearCurrentCardUI();               // 카드 숨김
void OnCardProcessed(int logicalIndex);    // 카드 처리 완료
void OnAllCardsProcessed();                // 전체 완료
void OnSequenceReset() { }                 // 초기화 (가상)
```

#### 흐름
```
OnStepEnter()
  → _order = [0, 1, 2, ...] (순차)
  → _currentIndex = 0
  → completionGate.ResetGate(CardCount)
  → OnSequenceReset()
  → UpdateCurrentCardUI() → OnApplyCardToUI(0)

CompleteCurrentCard()
  → OnCardProcessed(logicalIndex)
  → completionGate.MarkOneDone()
  → _currentIndex++
  → 남은 카드 있으면: UpdateCurrentCardUI()
  → 없으면: OnAllCardsProcessed()
```

### InventoryDropTargetStepBase

사용자가 인벤토리에서 아이템을 보유하고 있는지 확인 후 활성화 애니메이션을 재생.

#### 추상 프로퍼티
```csharp
RectTransform TargetVisualRoot;    // 애니메이션 대상
GameObject InstructionRoot;         // 완료 시 숨김
StepCompletionGate CompletionGate; // 게이트
```

#### 가상 설정
```csharp
float ActivateScale => 1.05f;       // 최대 스케일
float ActivateDuration => 0.6f;     // 애니메이션 시간
float DelayBeforeComplete => 1.5f;  // 완료 전 대기
float AutoActivateDelay => 0.5f;    // 시작 전 대기
```

#### 흐름
```
OnStepEnter()
  → CompletionGate.ResetGate(1)
  → OnStepEnterExtra()
  → HasItemInDb() 체크 (DemoMode면 항상 true)
  → AutoActivateRoutine() 코루틴
      → WaitForSeconds(AutoActivateDelay)
      → HandleActivatedRoutine()
          → InstructionRoot.SetActive(false)
          → PlayActivateAnimation() (스케일 펄스: sin 곡선 1→1.05→1)
          → WaitForSeconds(DelayBeforeComplete)
          → OnActivateComplete() (자식 오버라이드)
          → CompletionGate.MarkOneDone()
```

### CommonRewardStep — 보상 표시 (최종 스텝)

모든 문제의 마지막 스텝. 보상 아이템 표시 + DB 저장 + 홈 이동.

#### SequenceItem — 애니메이션 설정
```csharp
[Serializable]
public class SequenceItem
{
    RectTransform root;          // 대상 UI
    CanvasGroup canvasGroup;     // 페이드
    float delay;                 // 시작 지연
    float duration;              // 애니메이션 시간
    Vector2 startOffset;         // 시작 위치 오프셋
    bool useScale;               // 스케일 애니메이션
    float startScale;            // 시작 스케일
    bool useOvershoot;           // 바운스 효과
    float overshootScale;        // 바운스 최대 스케일
}
```

#### 흐름
```
OnStepEnter()
  → SaveRewardToDbOnce()  // 1회만: SaveReward() → 인벤토리 저장
  → ApplyRewardText()     // 보상 이름/설명 표시 (CSV)
  → homeButton.SetActive(false)
  → dialogueSequencer.OnEnterComplete += OnEnterComplete
  → StartSequence()       // SequenceItem 순차 애니메이션

OnEnterComplete()
  → homeButton.SetActive(true)  // "홈으로" 버튼 표시

GoToHome() [버튼 클릭]
  → MarkProblemSolved()  // DB: 문제 완료 기록
  → ProblemSession.ReturnTarget = LevelSelect
  → GameManager.Instance.GoToHome()

MarkProblemSolved()
  → DataService.Instance.Progress.MarkProblemSolvedForCurrentUser(theme, index)
```

### StartStep — 문제 인트로

#### OnStepEnter()
```csharp
int stageIndex = ProblemSession.CurrentProblemIndex;  // 1-10
titleText.text = ProblemRuntime.L(TitleBaseId + stageIndex);
descriptionText.text = ProblemRuntime.L(DescBaseId + stageIndex);
// "시작" 버튼 → StepFlowController.NextStep() (버튼 리스너)
```

---

## 9. 대사 시스템

### DialogueSequencer.cs (`Assets/01. Script/Common/DialogueSequencer.cs`)

HanamBoxRoot 프리팹에 부착. 캐릭터 대사의 순차 표시 + TTS 재생을 관리.

#### 두 가지 시퀀스
1. **enterTextIds**: 스텝 진입 시 자동 재생 (인트로 대사)
2. **completedTextIds**: 문제 완료 후 수동 호출 (완료 대사)

#### 이벤트
```csharp
event Action OnFirstTextShown;       // 첫 번째 enterText 표시 시
event Action OnEnterComplete;        // 마지막 enterText 표시 시 (상호작용 잠금 해제용)
event Action OnEnterSequenceDone;    // enter 시퀀스 완전 종료 시
```

#### Enter 시퀀스 흐름
```
OnEnable()
  → 1프레임 대기 (Logic이 이벤트 구독할 시간 확보)
  → PlaySequence(enterTextIds, onLastShown: OnEnterComplete, onDone: OnEnterSequenceDone)

ShowCurrent()
  → dialogueText.text = ProblemRuntime.L(textId)  // CSV 텍스트
  → SoundManager.Instance.PlayTTS(textId)          // 음성 재생
  → 첫 텍스트면: OnFirstTextShown 이벤트
  → 마지막 텍스트면: onLastShown(OnEnterComplete) 이벤트
  → nextDialogueBtn 활성화

OnClickNext()
  → _currentIndex++
  → ShowCurrent()
  → 인덱스 초과 시: Complete()
      → OnEnterSequenceDone 이벤트
```

#### Completed 시퀀스 흐름
```
Logic에서 dialogueSequencer.ShowCompletedText() 호출
  → PlaySequence(completedTextIds, onLastShown: ShowNextStepBtn, onDone: null)
  → ShowCurrent() → 텍스트 표시
  → 마지막 텍스트 → NextStepBtn 활성화
  → 사용자가 NextStepBtn 클릭
  → StepFlowController.NextStep()
```

#### 페이지 표시
```
enterTextIds = [101, 102, 103]  // 3페이지
completedTextIds = [201, 202]    // 2페이지
→ 전체 "(1/5)", "(2/5)", ... "(5/5)"
```

---

## 10. 이펙트 시스템

### EffectControllerBase.cs (`Assets/01. Script/Effect/Common/EffectControllerBase.cs`)

DOTween 기반 인트로 애니메이션 관리자. introElements 배열로 데이터 드리븐 설정.

#### IntroElement 설정
```csharp
[Serializable]
public class IntroElement
{
    RectTransform target;
    IntroAnimationType animationType;  // Slide 또는 Scale
    float delay;
    float duration = 0.3f;
    // Slide: direction (Up/Down/Left/Right), distance (50f)
    // Scale: startScale (0.3f)
}
```

#### PlayIntro() — Slide 애니메이션
```
시작: basePos + GetDirectionOffset(direction, distance)
끝:   basePos
알파:  0 → 1
```

#### PlayIntro() — Scale 애니메이션
```
시작: baseScale * startScale (예: 0.3)
끝:   baseScale (1.0)
알파:  0 → 1
이징: OutBack (바운스)
```

### IntroElement.cs (`Assets/01. Script/Effect/Common/IntroElement.cs`)

개별 UI 오브젝트에 부착하는 독립 입장 애니메이션 컴포넌트.

#### 애니메이션 타입
- **Slide**: slideFrom 방향에서 미끄러져 들어옴
- **Scale**: startScale에서 1로 커짐
- **FlyIn**: Catmull-Rom 곡선으로 비행 경로
- **Fade**: 알파 0→1

#### API
```csharp
Play(Action onComplete = null)   // 애니메이션 시작
SetToEnd()                        // 최종 상태로 즉시 이동
ResetToStart()                    // 시작 상태로 복원
event Action OnArrived;           // 애니메이션 완료 시 발생
```

### GlowEffect.cs (`Assets/01. Script/Effect/Common/GlowEffect.cs`)

스케일 펄스 + 알파 페이드를 조합한 반복 발광 효과.

```
duration=1.0s 기준:
  0.0s-0.5s: Scale 1.0→1.2, Alpha 0.5→1.0
  0.5s-1.0s: Scale 1.2→1.0, Alpha 1.0→0.5
  → SetLoops(-1, Restart)  // 무한 반복
```

### ButtonHoverEffect.cs

IPointerEnter/Exit/Down/Up 기반 버튼 피드백.
```
Normal → PointerEnter → Hover (scale: 1.08, outline ON)
         → PointerDown → Pressing (sprite: pressed)
         → PointerUp → Selected (sprite: selected)
PointerExit → Normal
```

---

## 11. STT 시스템

### STTManager.cs — Whisper.cpp 래퍼 싱글톤

#### 초기화
```
Awake(): 싱글톤 + DontDestroyOnLoad
Start(): InitializeWhisper()
  → StreamingAssets/WhisperModels/ggml-tiny.bin 로드 (백그라운드 스레드)
  → 16kHz, 4 CPU 스레드, Korean, Greedy 샘플링
```

#### 녹음 흐름
```
StartRecording()
  → Microphone.Start() + RecordAudio() 코루틴 (PCM 수집)
  → enableRealtimeProcessing 시: 2초마다 OnPartialResult

StopRecording()
  → Microphone.End()
  → ProcessAudio() (최종 인식, 별도 스레드)
  → OnFinalResult 이벤트
```

#### Whisper 파라미터
| 파라미터 | 실시간 | 최종 |
|----------|--------|------|
| singleSegment | true | false |
| max_tokens | 4 | 16 |
| temperature | 0 | 0 |
| suppress_blank | true | true |
| no_speech_thold | 0.6 | 0.6 |

#### 키워드 힌트
```csharp
SetPromptHint(string[] keywords)
→ initial_prompt에 키워드 삽입
→ 인식 정확도 ~20-30% 향상
```

### MicRecordingIndicator.cs — UI + 키워드 매칭

```csharp
SetKeywords(string[] words)          // 매칭 대상 설정
event Action<int> OnKeywordMatched;   // 매칭 성공 (인덱스)
event Action<string> OnNoMatch;       // 매칭 실패 (원본 텍스트)
event Action<bool> OnRecordingChanged; // 녹음 상태 변경
```

---

## 12. 사운드 시스템

### SoundManager.cs — 싱글톤

#### Awake()
```
싱글톤 + DontDestroyOnLoad
Resources/TTS/ 하위 모든 AudioClip 로드
```

#### API
| 메서드 | 설명 |
|--------|------|
| `PlayTTS(int textId)` | textId로 TTS 클립 검색 → 재생 |
| `PlayBGM(string clipName)` | Resources/BGM/ 하위 클립 로드 → 루프 재생 |
| `StopBGM()` | BGM 정지 |
| `PlaySFX(string clipName)` | 효과음 1회 재생 |

---

## 13. Problem 1~10 상세

### Binder/Logic 패턴

모든 복잡한 스텝은 두 개의 클래스로 구성:
- **Logic** (추상): 비즈니스 로직, abstract 프로퍼티로 UI 참조 요구
- **Binder** (구체): SerializeField로 UI 바인딩, abstract 프로퍼티 구현

```csharp
// Logic (추상)
public abstract class Director_P1_Step2_Logic : ProblemStepBase
{
    protected abstract Button[] FilmButtons { get; }  // Binder가 구현
    // 게임 로직...
}

// Binder (구체, 인스펙터에서 설정)
public class Director_P1_Step2 : Director_P1_Step2_Logic
{
    [SerializeField] private Button[] _filmButtons;
    protected override Button[] FilmButtons => _filmButtons;
}
```

---

### Problem 1: "발견" (Discovery)

#### Step 1 — 먼지 입자 인트로
- **베이스**: `ProblemStepBase`
- **메카닉**: UI 먼지 파티클 효과 (DustParticleUI)
- **주요 메서드**:
  - `OnStepEnter()`: SpawnDustParticles() 또는 RestartDustParticles()
  - `SpawnDustParticles()`: DustCount개 Image 생성 + DustParticleUI 컴포넌트 부착
- **DB 저장**: 없음 (시각 효과만)

#### Step 2 — 필름 조각 발견
- **베이스**: `ProblemStepBase`
- **메카닉**: 5개 필름 카드 클릭 → 체크마크 + 플래시 + 텍스트 표시
- **데이터 구조**: `FilmFragment { id, checkMark, flashOverlay, dimTarget, buttonText, wiggle, introElement, shakeTrigger }`
- **주요 메서드**:
  - `OnFilmClicked(int id)`: 이미 체크 또는 잠금 시 리턴 → 효과음 재생 → 체크마크 표시 → dimTarget 알파 복원 → CompletionGate.MarkOneDone()
  - `BindShakeTriggers()`: introElement.OnArrived → shakeTrigger.StartShake() 바인딩
- **DB 저장**: 없음

#### Step 3 — 생각/사실 분류
- **베이스**: `RandomCardSequenceStepBase`
- **메카닉**: 6+ 카드 순차 표시 → "생각" 또는 "사실" 선택 → 맞으면 해당 슬롯으로 DOTween 이동
- **데이터**: `SortLogEntry { filmId, text, correctType, chosenType }`
- **주요 메서드**:
  - `HandleSort(bool userChoseThought)`: 정답 비교 → 오답: 2초 피드백 → 정답: PlaceCurrentFilmIntoCorrectSlot() + AdvanceAfterDelayWithAnimation()
  - `PlaceCurrentFilmIntoCorrectSlot()`: 카드를 ThoughtSlots/FactSlots 배열의 빈 슬롯에 DOTween 이동
  - `SaveSortLogToDb()`: _logs 배열 → SaveAttempt()
- **STT**: MicRecordingIndicator, 키워드 ["생각", "사실"], OnSTTKeywordMatched → HandleSort()
- **DB 저장**: SortLogPayload (분류 결과)

---

### Problem 2: "재촬영" (Refilming)

#### Step 1 — 아이템 드래그 앤 드롭 (P2-P9 공용)
- **베이스**: `ProblemStepBase`
- **메카닉**: 인트로 애니메이션 (좌/우 슬라이드) → 인벤토리 표시 → 아이템을 드롭박스로 드래그 → 완료
- **주요 메서드**:
  - `OnStepEnter()`: InitState() + 인트로 애니메이션 시작
  - `InitState()`: DropBoxArea 리셋, DB에서 보유 아이템 로드, draggable 설정
  - `OnEnterSequenceDone()`: hanamBox 숨김, stepInventory 표시, 드래그 콜백 설정
  - `OnInventoryItemDropped(item)`: 인벤토리 숨김 → MarkOneDone() → ShowCompletedText()
- **DB 저장**: 없음 (진행만)

#### Step 2 — 감정 조명 공개
- **베이스**: `ProblemStepBase`
- **메카닉**: 5개 감정 슬롯 클릭 → textRoot 숨김 + imageRoot 표시 + 스프라이트 교체
- **데이터**: `EmotionSlot { filmButton, revealedSprite, textRoot, imageRoot, revealed }`
- **주요 메서드**:
  - `OnFilmClicked(slot)`: revealed 체크 → 숨김/표시 전환 → CompletionGate.MarkOneDone() → TryHandleCompleted()
- **DB 저장**: 없음

#### Step 3 — 관점 선택 + STT 재촬영
- **베이스**: `ProblemStepBase`
- **메카닉**: 3개 관점 중 선택 → 아웃라인 표시 → 마이크 활성화 → STT 녹음 → NG→OK 씬 전환
- **데이터**: `RefilmLogPayload { ngText, selectedId, selectedText, recorded }`
- **주요 메서드**:
  - `OnSlotClicked(int)`: 아웃라인 표시, MicButton 활성화, SetKeywords([관점 텍스트])
  - `OnSTTKeywordMatched()`: PlayRefilmComplete() → StepRoot 숨김 + OkSceneCard 표시
  - `SaveRefilmLogToDb()`: SaveAttempt(RefilmLogPayload)
- **DB 저장**: RefilmLogPayload

---

### Problem 3: "생각 재작성" (Rewrite)

#### Step 1 — 아이템 드래그 (P2 Step1과 동일)

#### Step 2 — 캐러셀 재작성 + STT
- **베이스**: `ProblemStepBase`
- **메카닉**: 다중 라운드. 각 라운드: 원문 표시 → 캐러셀로 대안 탐색 → STT 녹음 → 재작성 애니메이션
- **데이터**: `IRewriteStepData { Id, OriginalText, RewrittenText, Options[], OptionKeywords[][], OptionSprites[] }`
- **주요 메서드**:
  - `EnterInnerStep(int index)`: 가이드 표시 + 원문 표시 + 캐러셀 초기화 + STT 키워드 설정
  - `RefreshCarouselUI()`: 텍스트/인덱스/이미지 갱신, Prev/Next 버튼 상태
  - `OnSTTKeywordMatched(int)`: matchedIndex == _currentOptionIndex 확인 → PlayRewriteCompleteSequence()
  - `PlayRewriteCompleteSequence()`: 원문 페이드아웃 → 재작성 텍스트로 교체 (색상 변경) → 페이드인
  - `OnClickNextDialog()`: 다음 라운드 또는 SaveRewriteLogToDb() + 완료
- **이펙트**: Problem3_Step2_EffectController (펜 애니메이션 + 텍스트 교체)
- **DB 저장**: AttemptBody { steps[]: stepId, originalText, selectedOptionIndex, selectedOption, rewrittenText, recorded }

#### Step 3 — 객관식 + STT
- **베이스**: `MultipleChoiceStepBase<Question>`
- **메카닉**: 3개 문제 순차. 선택 → 아웃라인 → STT 확인 → 정답: 나머지 페이드아웃
- **주요 메서드**:
  - `ShowQuestion()`: 버튼 리스너 + STT 키워드 설정
  - `HandleWrong()`: 아웃라인 숨김 + 힌트 2초 표시 + 버튼 잠금
  - `HandleCorrect()`: 아웃라인 표시 + FadeOutIncorrectOptions() → MarkOneDone()
- **DB 저장**: 상속된 SaveAttempt

---

### Problem 4: "편집" (Editing)

#### Step 1 — 아이템 드래그 (P2 Step1과 동일)

#### Step 2 — 필름 컷 편집 (CUT/PASS)
- **베이스**: `ProblemStepBase`
- **메카닉**: 필름 컷 순차 → CUT(생각 삭제) 또는 PASS(사실 유지) → 올바른 분류 시 애니메이션 → 전부 정리 후 컬러 복원
- **데이터**: `FilmCutData { cutID, textId, isThinking }`, `CutStatus { ACTIVE, CUTTING, PASSED, DELETED }`
- **주요 메서드**:
  - `OnClickCut()`: isThinking이면 정답 → CutAnimation() (가위 효과 + 좌우 분리)
  - `OnClickPass()`: !isThinking이면 정답 → PassAnimation() (오른쪽 슬라이드)
  - `TryCompleteStep()`: 모든 생각 삭제 + 모든 사실 통과 → ColorRestoreAnimation() → SaveFilmEditingAttempt()
- **이펙트**: Problem4_Step2_EffectController (슬라이드, 가위, 흔들림, 그레이스케일→컬러)
- **DB 저장**: CutAttemptLog[] + CutActionLog[]

#### Step 3 — 반박 Yes/No 문제
- **베이스**: `ProblemStepBase`
- **메카닉**: 3개 카드 순차 → Yes/No 답변 → 정답: 카드 좌측 퇴장 + 다음 카드 우측 등장
- **데이터**: `QuestionData { questionId, mainTextId, isYesCorrect }`, `QuestionActionLog { questionId, answer, wasCorrect }`
- **주요 메서드**:
  - `HandleAnswer(bool isYes)`: 정답 비교 → OnCorrectAnswer() → 퇴장 애니메이션 → 다음 문제 또는 CompleteStep()
- **이펙트**: Problem4_Step3_EffectController (좌우 슬라이드 + 알파)
- **DB 저장**: QuestionActionLog[]

---

### Problem 5: "표현" (Expression)

#### Step 1 — 아이템 드래그 (P2 Step1과 동일)

#### Step 2 — 씬 아이콘 탐험 (줌아웃)
- **베이스**: `ProblemStepBase`
- **메카닉**: 4-6개 씬 아이콘 배치 → 클릭하면 UnrevealedRoot 숨김 + RevealedRoot 표시 → 전체 클릭 시 완료
- **데이터**: `IZoomOutSceneData { Id, IconButton, UnrevealedRoot, RevealedRoot, textIds }`
- **DB 저장**: 없음

#### Step 3 — 시나리오 대사 + STT
- **베이스**: `ProblemStepBase`
- **메카닉**: 5+ 시나리오 순차. 각: 필름 이미지/텍스트 + 하나미 음성 → 사용자 응답 텍스트 → STT 녹음 → 다음
- **데이터**: `IScenarioCardData { Id, FilmSprite, FilmTextId, HanamTextId, ResponseTextId }`
- **주요 메서드**:
  - `ShowCurrentScenario()`: 비주얼 갱신 + DialogueSequencer.SetText(HanamTextId) + SetKeywords([ResponseText])
  - `OnSttMatched()`: AdvanceToNext() → 로그 기록 + 다음 시나리오
  - `CompleteAllScenarios()`: SaveScenarioAttempt() + 완료
- **DB 저장**: ScenarioLogEntry[] { id, hanamText, responseText, time }

---

### Problem 6: "이완 훈련" (Relaxation Training)

#### Step 1 — 의자 드롭 (InventoryDropTargetStepBase)
- Scale 1.02x, 2.5초 대기, 의자 아이콘 + 글로우 + 스파클 표시

#### Step 2 — 스트레스 반응 카드 선택 (3/8)
- **베이스**: `ProblemStepBase`
- **메카닉**: 8개 카드에서 정확히 3개 선택. 선택마다 스튜디오 조명 점등 (1→2→3)
- **데이터**: `StressCardSlot { id, labelTextId, button, labelText, backgroundImage, selectImage }`
- **색상**: 선택 시 #FF8A3D (오렌지)
- **주요 메서드**:
  - `OnClickCard(int)`: 선택 토글 → UpdateCardVisuals() → UpdateLightsVisual() → UpdateGateState()
  - `SaveSelectionToDB()`: { selectedCount: 3, selectedResponses: [{id, label}...] }
- **DB 저장**: 선택된 카드 목록

#### Step 3 — 이완 훈련 자동 재생
- **베이스**: `ProblemStepBase`
- **메카닉**: 3단계 자동 재생 (편안한 자세 → 복식 호흡 → 근육 이완). 각 단계: 제목 + 안내 + 프로그레스 바 + TTS
- **주요 메서드**:
  - `PlayRoutine()`: 메인 루프 — ApplyStepUI() → 카드 팝인 → duration 동안 프로그레스 바 채움 → 2초 대기 → 다음
  - `OnClickPause()` / `OnClickResume()`: TTS/BGM 일시정지/재개
- **DB 저장**: 없음 (훈련 데이터 미저장)

---

### Problem 7: "보여지는 나 vs 진짜 나" (Mask vs True Self)

#### Step 1 — 아이템 드래그 (P2 Step1과 동일)

#### Step 2 — 가면/감정 2단계 선택
- **베이스**: `ProblemStepBase`
- **메카닉**: Phase 1: 가면 4개 중 1개 선택 → 2초 대기 → Phase 2: 감정 4개 중 1개 선택 → DB 저장
- **데이터**: `ChoiceItem { id, labelTextId, button, clickImage }`, `MaskFeelingAttemptDto { mask, feeling }`
- **주요 메서드**:
  - `OnMaskSelected(item)`: 잠금 + 비주얼 + TransitionAfterDelay() → Phase 2로 전환
  - `OnFeelingSelected(item)`: SaveAttempt(MaskFeelingAttemptDto) → CompleteAfterDelay()
- **DB 저장**: MaskFeelingAttemptDto { mask:{id,label}, feeling:{id,label} }

#### Step 3 — STT 명대사 말하기
- **베이스**: `ProblemStepBase`
- **메카닉**: 3개 명대사 중 선택 → 마이크 → STT 매칭 → 성공/재시도
- **데이터**: `DialogueItem { id, textId, button, selectImg }`, `DialogueAttemptDto { id, text }`
- **주요 메서드**:
  - `OnDialogueClicked(int)`: selectImg 표시 + MicRoot 표시
  - `OnSTTKeywordMatched(int)`: matchedIndex == selectedIndex 확인 → 성공: SaveDialogueAttempt() + 완료
  - `ShowRetryGuide()`: 재시도 안내 + TTS
- **DB 저장**: DialogueAttemptDto

---

### Problem 8: "첫 장면 결정" (First Scene Decision)

#### Step 1 — 아이템 드래그 (P2 Step1과 동일)

#### Step 2 — 스토리보드 드래그&드롭
- **베이스**: `ProblemStepBase`
- **메카닉**: 5개 씬 카드 캐러셀 → 드래그 → 올바른 슬롯에 드롭 → 오답 시 스냅백
- **데이터**: `SceneCardItem { id, textId, cardSprite, correctSlotIndex }`, `SlotItem { slotIndex, emptyState, filledState, dropArea }`
- **드래그 시스템**:
  - `OnBeginDrag()`: 프록시 활성화, 고스트 알파 0.5
  - `OnDrag()`: 프록시를 포인터 따라 이동
  - `OnEndDrag()`: EventSystem 레이캐스트로 슬롯 감지 → 정답: PlaceCard() → 오답: SnapBackProxy() (ease-out cubic)
- **캐러셀**: _unplacedIndices 리스트로 미배치 카드 관리, 배치 시 제거
- **DB 저장**: CardPlacementDto[] { cardId, slotIndex, isCorrect, placedAtSeconds }

#### Step 3 — 액션 카드 + STT
- **베이스**: `ProblemStepBase`
- **메카닉**: N개 액션 카드 중 선택 → selectedIcon 표시 → STT 녹음 → 매칭
- **데이터**: `ActionItem { id, textId, button, label, selectedIcon }`, `ActionAttemptDto { selectedAction, recordingDuration }`
- **DB 저장**: ActionAttemptDto

---

### Problem 9: "좋은 말 전하기" (Good Words)

#### Step 1 — 아이템 드래그 (P2 Step1과 동일)

#### Step 2 — 3라운드 대사 선택
- **베이스**: `ProblemStepBase`
- **메카닉**: Round 1→2→3. 각: 상황 표시 + 3개 선택지 → 정답: 대사 버블 + 씬 이미지 교체 → 오답: 힌트
- **데이터**: `RoundData { situationTextId, choiceTextIds[3], correctChoiceIndex, resultTextId, speechBubbleTextId, sceneSprite, answerSceneSprite }`
- **주요 흐름**:
  - 정답 → 질문 UI 숨김 → 답변 UI 표시 (말풍선 + 대답 이미지) → "다음" 버튼 또는 최종 저장
  - 오답 → hanamBox에 안내 텍스트 (재시도 가능)
- **DB 저장**: ChoiceAttemptDto[] { roundIndex, choiceIndex, isCorrect }

#### Step 3 — 대사 조각 STT 말하기 (3라운드)
- **베이스**: `ProblemStepBase`
- **메카닉**: 3라운드 (상황→감정→요청). 각: 키워드 (흰색) + 안내 → STT 매칭 → 전체 문장 (검정) 표시 → 퍼즐 이미지 전환
- **데이터**: `RoundData { guideTextId, keywordTextId, fullTextId, altFullTextId, questionSprite, answerSprite }`
- **STT 키워드**: fullTextId + altFullTextId (대체 표현)
- **색상**: 키워드 = white, 전체 문장 = dark gray (0.196, 0.196, 0.196)
- **DB 저장**: SpeakAttemptDto[] { roundIndex, phase, recordedText }

---

### Problem 10: "나의 영화 포스터 만들기" (Movie Poster)

#### SharedData (ScriptableObject 런타임 브릿지)
```csharp
Problem10SharedData {
    int selectedGenreIndex;     // Step2에서 설정
    Sprite selectedSprite;       // Step2에서 설정
    string posterTitle;          // (미사용)
    string posterCommitment;     // Step3 STT 결과

    SetSelection(int, Sprite);   // Step2 호출
    SetPosterTexts(string, string); // Step3 호출
    Clear();                     // 리셋
}
```

#### Step 1 — 포스터 프레임 드롭 (InventoryDropTargetStepBase)
- Scale 1.1x, 0.5초 + 1초 대기
- DB 저장: { action: "poster_dropped", targetItem: "poster_frame" }

#### Step 2 — 영화 장르 선택
- **베이스**: `ProblemStepBase`
- **메카닉**: 4개 장르 중 1개 선택 → selectIndicator → 1초 대기 → CompleteRoot 표시 → SharedData에 저장
- **데이터**: `GenreCardData { labelTextId, cardSprite }`, `GenreSelectionDto { selectedIndex, selectedGenre }`
- **SharedData 연동**: `SharedData.SetSelection(index, cardSprite)` → Step3에서 사용
- **DB 저장**: GenreSelectionDto

#### Step 3 — 다짐 말하기 + 포스터 작성
- **베이스**: `ProblemStepBase`
- **메카닉**: SharedData.selectedGenreIndex로 장르별 가이드 로드 → DialogueSequencer 동적 설정 → STT 녹음 → 포스터에 텍스트 기록
- **데이터**: `GenreCommitmentData { guideTextId, sttKeyword, cardSprite }`, `PosterCreationDto { commitment }`
- **주요 메서드**:
  - `OnStepEnter()`: SharedData에서 장르 읽기 → dialogueSequencer.SetEnterTextIds([guideTextId])
  - STT 성공 → PosterCommitmentText에 기록 → SharedData.SetPosterTexts("", text) → DB 저장
- **DB 저장**: PosterCreationDto

---

## 14. 전체 콜체인 예시

### 예시 1: 회원가입 전체 흐름
```
RegisterScene → SignupFormUI.OnSignupRequested
  → SignupController.HandleSignup(name, email, password)
    → AuthValidator.NormalizeEmail(email)
    → AuthValidator.IsValidEmail(email)
    → AuthValidator.IsStrongPassword(password)
    → AuthService.SignUp(name, email, password)
      → UserRepository.ExistsEmail(email)
      → BCrypt.HashPassword(password, workFactor=10)
      → UserRepository.InsertUser(user)
        → DBGateway.WithDb(action)
          → DBHelper.With(action)
            → new LiteDatabase(connString)
            → db.GetCollection<User>("users")
            → col.Insert(user)
      → return Result.Success()
  → "회원가입이 완료되었습니다." 표시
  → RegisterTabsController.ShowLogin()
```

### 예시 2: 로그인 → 홈 이동
```
LoginFormUI.OnLoginRequested
  → LoginController.HandleLogin(email, password)
    → AuthService.Login(email, password)
      → AuthValidator.NormalizeEmail(email)
      → UserRepository.FindActiveUserByEmail(email)
      → BCrypt.Verify(password, user.PasswordHash)
      → return Result<User>.Success(user)
    → SessionManager.Instance.SignIn(user)
      → PlayerPrefs에 UserSnapshot + SessionId 저장
      → OnChanged 이벤트
    → SceneNavigator.Instance.GoTo(ScreenId.HOME)
      → CoGoTo(HOME)
        → IsAllowed(HOME) → IsSignedIn == true
        → Fade(1f) → 화면 어둡게
        → SceneManager.LoadSceneAsync("HomeScene")
        → history.Push(REGISTER)
        → current = HOME
        → Fade(0f) → 화면 밝게
```

### 예시 3: 문제 선택 → 풀이 → 홈 복귀
```
HomeScene → ThemePanelsController.HandleProblemClicked(Director, 3)
  → ProblemSession.CurrentTheme = Director
  → ProblemSession.CurrentProblemIndex = 3
  → SceneNavigator.GoTo(PROBLEM)

ProblemScene 로드
  → ProblemSceneController.Start()
    → SetupThemeRoot() → directorRoot 활성화
    → ActivateSingleProblem(3) → Problem_3 자식만 SetActive(true)
  → StepFlowController.OnEnable()
    → SetAllInactive()
    → PlayBGM(bgmClipName)
    → GoToStep(0) → StartStep 활성화

StartStep.OnStepEnter()
  → 제목/설명 표시 (CSV)
  → "시작" 버튼 → StepFlowController.NextStep()
    → GoToStep(1) → Step2 활성화

Step2_Logic.OnStepEnter()
  → DialogueSequencer 이벤트 바인딩
  → 1프레임 후 enterTextIds 재생
  → OnEnterComplete → 상호작용 잠금 해제
  → [사용자 문제 풀이]
  → SaveAttempt(payload) → DB에 풀이 로그 저장
  → CompletionGate.MarkOneDone()
  → dialogueSequencer.ShowCompletedText()
  → NextStepBtn → StepFlowController.NextStep()
    → GoToStep(2) → Step3 활성화

[Step3 마찬가지...]

CommonRewardStep.OnStepEnter()
  → SaveRewardToDbOnce()
    → SaveReward(body, "mind_lens", "마음 렌즈")
      → DataService.Reward.SaveRewardForCurrentUser()
        → ProgressService.SaveStepAttemptForCurrentUser() → attempts 컬렉션
        → InventoryRepository.Add(item) → inventory 컬렉션
  → SequenceItem 애니메이션 재생
  → DialogueSequencer 완료 대사
  → homeButton 표시

GoToHome() 버튼 클릭
  → MarkProblemSolved()
    → DataService.Progress.MarkProblemSolvedForCurrentUser(Director, 3)
      → ResultRepository.InsertResult(result) → results 컬렉션
  → ProblemSession.ReturnTarget = LevelSelect
  → GameManager.Instance.GoToHome()
    → SceneNavigator.GoTo(HOME)

HomeScene 재로드
  → ThemePanelsController.Start()
    → ReturnTarget == LevelSelect
    → ShowDirectorLevelSelect()
      → RefreshSinglePanel()
        → Progress.FetchSolvedProblemIndexes() → 문제 3 풀림 확인
        → 문제 4 잠금 해제
```

### 예시 4: 앱 재시작 시 자동 로그인
```
Bootstrap 씬 로드
  → Bootstrap.Awake()
    → DatabaseInitializer.InitializeIndexes()
    → LoadTables() → CSV 로드
    → InitRoutine() 코루틴 시작

  → DataService.Awake() → 모든 서비스 초기화
  → SessionManager.Awake() → DontDestroyOnLoad
  → SceneNavigator.Awake() → DontDestroyOnLoad

InitRoutine()
  → SessionManager.TryRestore()
    → PlayerPrefs에서 UserSnapshot 복원
    → IsSignedIn = true
  → SceneNavigator.GoTo(HOME) → 홈 화면 직행
```

---

## 15. 핵심 규칙 & 주의사항

### 절대 규칙

1. **Awake() 사용 금지** — OnEnable()에서 초기화. 스텝은 SetActive()로 관리되므로 OnEnable/OnDisable이 라이프사이클.

2. **텍스트 하드코딩 금지** — 반드시 `ProblemRuntime.L(textId)` 사용. CSV: `Assets/Resources/CSV/MC_DataTable_v01.csv`

3. **StepCompletionGate.OnEnable()이 _autoNextFired 리셋** — 스텝 재진입 시 중복 NextStep() 방지.

4. **DialogueSequencer.OnEnable()은 1프레임 대기** — Logic/Binder가 같은 프레임의 OnEnable()에서 이벤트 구독할 시간 확보.

5. **SaveAttempt/SaveReward는 DataService null 체크** — 미가용 시 경고 로그만 출력 (예외 미발생).

### 싱글톤 패턴
- GameManager, SessionManager, SceneNavigator, DataService, SoundManager, Bootstrap
- 공통: Awake()에서 중복 Destroy + DontDestroyOnLoad
- 접근: `ClassName.Instance` 정적 프로퍼티

### Binder/Logic 패턴
- Logic = 비즈니스 로직 (추상, 테스트 가능)
- Binder = UI 바인딩 (구체, SerializeField)
- 분리 이유: 로직 재사용 + 인스펙터 설정 분리

### 데이터 흐름 요약
```
사용자 응답 → [Serializable] 객체 → SaveAttempt(object)
  → JSON 직렬화 → ProgressService.SaveStepAttemptForCurrentUser()
  → ProgressRepository.InsertAttempt() → LiteDB attempts 컬렉션

문제 완료 → CommonRewardStep.MarkProblemSolved()
  → ProgressService.MarkProblemSolvedForCurrentUser()
  → ResultRepository.InsertResult() → LiteDB results 컬렉션

보상 지급 → SaveReward(body, itemId, itemName)
  → RewardService.SaveRewardForCurrentUser()
  → InventoryRepository.Add() → LiteDB inventory 컬렉션
```

### Problem 요약 테이블

| Problem | 테마 | Step 1 | Step 2 | Step 3 | Step 4 |
|---------|------|--------|--------|--------|--------|
| **1** | 발견 | 먼지 입자 | 필름 5개 클릭 | 생각/사실 분류 (STT) | 보상 |
| **2** | 재촬영 | 아이템 드롭 | 감정 조명 5개 | 관점 선택 + STT | 보상 |
| **3** | 재작성 | 아이템 드롭 | 캐러셀 재작성 + STT | 객관식 + STT | 보상 |
| **4** | 편집 | 아이템 드롭 | 필름 컷 CUT/PASS | Yes/No 반박 | 보상 |
| **5** | 표현 | 아이템 드롭 | 씬 아이콘 탐험 | 시나리오 대사 + STT | 보상 |
| **6** | 이완 | 의자 드롭 | 스트레스 카드 3/8 선택 | 이완 자동 재생 | 보상 |
| **7** | 가면 | 아이템 드롭 | 가면→감정 2단계 | 명대사 STT | 보상 |
| **8** | 첫장면 | 아이템 드롭 | 스토리보드 D&D | 액션 카드 + STT | 보상 |
| **9** | 좋은말 | 아이템 드롭 | 3라운드 대사 선택 | 3라운드 키워드 STT | 보상 |
| **10** | 포스터 | 포스터 드롭 | 장르 4개 선택 | 다짐 STT + 포스터 | 보상 |

---

## 파일 경로 참조

| 분류 | 경로 |
|------|------|
| 스크립트 루트 | `Assets/01. Script/` |
| 코어 매니저 | `Assets/01. Script/Core/` |
| 데이터 서비스 | `Assets/01. Script/Data/` |
| Repository | `Assets/01. Script/Data/Repository/` |
| 모델 | `Assets/01. Script/Data/Models/` |
| 인증 서비스 | `Assets/01. Script/Service/` |
| 홈 씬 | `Assets/01. Script/HomeScene/` |
| 회원가입 씬 | `Assets/01. Script/RegisterScene/` |
| 문제 씬 코어 | `Assets/01. Script/ProblemScene/` |
| 스텝 베이스 | `Assets/01. Script/ProblemScene/StepBases/` |
| Director 문제 | `Assets/01. Script/ProblemScene/Director/Problem1~10/` |
| 공용 이펙트 | `Assets/01. Script/Effect/Common/` |
| 문제별 이펙트 | `Assets/01. Script/Effect/Problem1~10/` |
| 대사 시스템 | `Assets/01. Script/Common/` |
| STT | `Assets/01. Script/STT/` |
| CSV 데이터 | `Assets/Resources/CSV/MC_DataTable_v01.csv` |
| 프리팹 | `Assets/02. Prefab/` |
| 리소스 | `Assets/03. Resource/` |
| 씬 | `Assets/04. Scene/` |
