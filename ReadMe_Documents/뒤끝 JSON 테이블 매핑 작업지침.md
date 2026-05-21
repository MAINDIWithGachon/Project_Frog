# 뒤끝 JSON 테이블 매핑 작업지침

## 한 줄 요약

클라이언트는 `RuntimeData` JSON 한 덩어리만 서버에 전달한다.
GetRuntimeDataJson() 으로 덩어리 받는 함수 만들어두었음.

백엔드는 이 JSON을 파싱해서 뒤끝 DB 테이블별로 나누어 저장한다.

로드 요청이 오면 백엔드는 뒤끝 DB의 여러 테이블 값을 다시 같은 `RuntimeData` JSON 형태로 합쳐서 클라이언트에 반환한다.

## 목표

클라이언트 개발자는 뒤끝 DB의 테이블 구조를 몰라도 된다.

클라이언트는 항상 아래 형태의 JSON만 주고받는다.

```json
{
  "Currency": {
    "Gold": 10000,
    "UpgradeStone": 100,
    "Gem": 10000
  },
  "skillLevels": [
    { "skillId": 0, "level": 1 },
    { "skillId": 1, "level": 3 },
    { "skillId": 2, "level": 5 }
  ],
  "statLevels": {
    "attackLevel": 10,
    "hpLevel": 8,
    "hpRegenLevel": 3,
    "critChanceLevel": 6,
    "critDamageLevel": 4
  },
  "growth": {
    "growthLevel": 1,
    "nowExp": 0,
    "growthPoint": 0,
    "attackLevel": 1,
    "hpLevel": 1,
    "critDamageLevel": 1
  }
}
```

백엔드 개발자는 이 JSON과 뒤끝 DB 테이블 사이의 변환 계층을 담당한다.

## 역할 분리

### 클라이언트 개발자

- `RuntimeData` JSON 구조 정의
- 인게임에서 재화, 스킬, 스탯, 성장 데이터 변경
- 저장 시 전체 JSON 전달
- 로드 시 전체 JSON 수신 후 `RuntimeData`에 적용
- 뒤끝 DB 테이블 구조는 직접 알 필요 없음

### 백엔드 개발자
- GetRuntimeDataJson() 사용하여 json 받음.
- 클라이언트가 보낸 JSON 파싱
- JSON 데이터를 도메인별 테이블/컬럼으로 분리 저장
- 로드 요청 시 여러 테이블을 조회
- 조회한 값을 다시 클라이언트가 쓰는 JSON 구조로 조립
- 신규 유저 기본 데이터 생성(데이터 세부값은 클라이언트 개발자와 의논 필요 ex 레벨이 0으로 시작하는 1로 시작하는지 등)
- 일부 테이블 누락/저장 실패/로드 실패 처리
- `schemaVersion` 기반 버전 관리

## 저장 흐름

```text
유저:  Save 요청
-> 클라이언트 : RuntimeData JSON 전달
-> 백엔드가 JSON 파싱
-> Currency 값 추출
-> SkillLevels 값 추출
-> StatLevels 값 추출
-> Growth 값 추출
-> 뒤끝 DB 테이블별 저장
-> 저장 결과 반환
```

클라이언트가 보내는 저장 요청의 개념:

```text
SaveRuntimeData(runtimeDataJson)
```

백엔드는 `runtimeDataJson`을 받아 내부에서 나누어 저장한다.

클라이언트는 다음을 직접 호출하지 않는다.

```text
SaveCurrency
SaveSkillLevels
SaveStatLevels
SaveGrowth
```

필요하다면 백엔드 내부 구현으로만 분리한다.

## 로드 흐름

```text
클라이언트 Load 요청
-> 백엔드가 현재 유저의 뒤끝 DB row 조회
-> Currency 테이블 조회
-> SkillLevels 테이블 조회
-> StatLevels 테이블 조회
-> Growth 테이블 조회
-> RuntimeData JSON 형태로 재조립
-> 클라이언트에 JSON 반환
```

클라이언트가 받는 로드 응답은 저장 요청 때 보낸 JSON과 같은 구조여야 한다.


## 이름 충돌 주의

JSON에는 같은 이름의 필드가 서로 다른 영역에 존재한다.

예:

```text
statLevels.attackLevel
growth.attackLevel
```

테이블을 나누면 괜찮지만, 같은 테이블에 합치거나 로그/DTO로 펼칠 때는 반드시 prefix를 붙인다.

권장 이름:

```text
stat_attackLevel
growth_attackLevel
```

## 저장 매핑 규칙

클라이언트 JSON:

```json
{
  "Currency": {
    "Gold": 10000,
    "UpgradeStone": 100,
    "Gem": 10000
  }
}
```

뒤끝 DB 저장:

```text
PlayerCurrency.Gold = 10000
PlayerCurrency.UpgradeStone = 100
PlayerCurrency.Gem = 10000
```

클라이언트 JSON:

```json
{
  "statLevels": {
    "attackLevel": 10,
    "hpLevel": 8,
    "hpRegenLevel": 3,
    "critChanceLevel": 6,
    "critDamageLevel": 4
  }
}
```

뒤끝 DB 저장:

```text
PlayerStatLevels.attackLevel = 10
PlayerStatLevels.hpLevel = 8
PlayerStatLevels.hpRegenLevel = 3
PlayerStatLevels.critChanceLevel = 6
PlayerStatLevels.critDamageLevel = 4
```

클라이언트 JSON:

```json
{
  "growth": {
    "growthLevel": 1,
    "nowExp": 0,
    "growthPoint": 0,
    "attackLevel": 1,
    "hpLevel": 1,
    "critDamageLevel": 1
  }
}
```

뒤끝 DB 저장:

```text
PlayerGrowth.growthLevel = 1
PlayerGrowth.nowExp = 0
PlayerGrowth.growthPoint = 0
PlayerGrowth.attackLevel = 1
PlayerGrowth.hpLevel = 1
PlayerGrowth.critDamageLevel = 1
```

클라이언트 JSON:

```json
{
  "skillLevels": [
    { "skillId": 0, "level": 1 },
    { "skillId": 1, "level": 3 },
    { "skillId": 2, "level": 5 }
  ]
}
```

뒤끝 DB 저장:

```text
PlayerSkillLevels.skillLevelsJson = 위 배열을 JSON 문자열로 저장
```

또는 스킬별 row 방식을 선택했다면:

```text
PlayerSkillLevels row 1: skillId = 0, level = 1
PlayerSkillLevels row 2: skillId = 1, level = 3
PlayerSkillLevels row 3: skillId = 2, level = 5
```

## 로드 조립 규칙

로드 시에는 저장의 반대 순서로 진행한다.

```text
PlayerCurrency
-> JSON의 Currency로 조립

PlayerSkillLevels
-> JSON의 skillLevels로 조립

PlayerStatLevels
-> JSON의 statLevels로 조립

PlayerGrowth
-> JSON의 growth로 조립
```

최종 반환 JSON은 반드시 클라이언트 계약 구조와 같아야 한다.

```json
{
  "Currency": {
    "Gold": 10000,
    "UpgradeStone": 100,
    "Gem": 10000
  },
  "skillLevels": [
    { "skillId": 0, "level": 1 },
    { "skillId": 1, "level": 3 },
    { "skillId": 2, "level": 5 }
  ],
  "statLevels": {
    "attackLevel": 10,
    "hpLevel": 8,
    "hpRegenLevel": 3,
    "critChanceLevel": 6,
    "critDamageLevel": 4
  },
  "growth": {
    "growthLevel": 1,
    "nowExp": 0,
    "growthPoint": 0,
    "attackLevel": 1,
    "hpLevel": 1,
    "critDamageLevel": 1
  }
}
```

## 신규 유저 처리

로드 요청 시 어느 테이블에도 row가 없다면 신규 유저로 판단한다.

신규 유저 처리:

```text
1. 기본 RuntimeData JSON 생성
2. JSON을 파싱
3. PlayerCurrency Insert
4. PlayerSkillLevels Insert
5. PlayerStatLevels Insert
6. PlayerGrowth Insert
7. 같은 기본 JSON을 클라이언트에 반환
```

기본값은 클라이언트와 협의한 기본 JSON을 사용한다.

## 일부 테이블 누락 처리

여러 테이블로 나누면 일부 테이블만 없는 상황이 생길 수 있다.

예:

```text
PlayerCurrency 있음
PlayerSkillLevels 없음
PlayerStatLevels 있음
PlayerGrowth 있음
```

권장 정책:

- 누락된 테이블은 기본값으로 생성한다.
- 생성 후 전체 JSON을 조립해서 반환한다.
- 누락 복구 로그를 남긴다.

단, 핵심 테이블이 손상되었거나 파싱할 수 없는 경우에는 실패를 반환하고 클라이언트가 재시도할 수 있게 한다.

## 저장 실패 처리

테이블을 나누어 저장하면 일부만 성공할 수 있다.

예:

```text
PlayerCurrency 저장 성공
PlayerSkillLevels 저장 실패
```

이 경우 데이터 정합성 문제가 생길 수 있다.

백엔드 개발자는 다음 정책을 반드시 구현하거나 문서화한다.

- 모든 테이블 저장이 성공해야 Save 성공으로 반환
- 하나라도 실패하면 Save 실패로 반환
- 실패한 경우 클라이언트가 재시도할 수 있도록 실패 사유 반환
- 저장 중복 요청 방지
- 마지막 저장 성공 시각 기록
- 가능하면 저장 실패 로그 기록

## 스킬 강화 같은 복합 변경 주의

스킬 강화는 보통 두 데이터가 함께 바뀐다.

```text
Gold 감소
skillLevels 변경
```

테이블 기준으로 보면:

```text
PlayerCurrency 업데이트
PlayerSkillLevels 업데이트
```

둘 중 하나만 성공하면 문제가 된다.

따라서 복합 변경 저장에서는 다음 원칙을 지킨다.

- 두 테이블 저장이 모두 성공해야 성공 처리
- 하나라도 실패하면 실패 반환
- 클라이언트는 실패 시 dirty 상태를 유지하고 재시도
- 가능하면 저장 전후 로그를 남김

## 버전 관리

각 테이블에는 `schemaVersion`을 둔다.

예:

```text
PlayerCurrency.schemaVersion
PlayerSkillLevels.schemaVersion
PlayerStatLevels.schemaVersion
PlayerGrowth.schemaVersion
```

콘텐츠 추가나 저장 구조 변경이 있을 때 버전을 올린다.

예:

```text
v1: Gold, UpgradeStone, Gem
v2: Energy 추가
```

로드 시 낮은 버전의 row를 발견하면:

```text
1. 기존 값 로드
2. 새 필드 기본값 적용
3. 최신 JSON 구조로 조립
4. 필요하면 최신 schemaVersion으로 저장
```

## 기본값 정책

필드가 누락되었을 때 사용할 기본값을 정한다.

권장 기본값: 추후 논의 필요

```text
Gold = 0
UpgradeStone = 0
Gem = 0
skillLevels = 빈 배열 또는 클라이언트 기본 스킬 배열
statLevels.attackLevel = 0
statLevels.hpLevel = 0
statLevels.hpRegenLevel = 0
statLevels.critChanceLevel = 0
statLevels.critDamageLevel = 0
growth.growthLevel = 1
growth.nowExp = 0
growth.growthPoint = 0
growth.attackLevel = 1
growth.hpLevel = 1
growth.critDamageLevel = 1
```

최종 기본값은 클라이언트와 협의한다.

## 검증 규칙

저장 전에 최소 검증을 수행한다.

```text
Gold >= 0
UpgradeStone >= 0
Gem >= 0
skillId >= 0
skill level >= 0
stat level >= 0
growthLevel >= 1
nowExp >= 0
growthPoint >= 0
growth stat level >= 0
```

검증 실패 시 저장하지 않고 실패 결과를 반환한다.

## 응답 형태 권장

단순 `true/false`보다 실패 사유를 알 수 있는 결과를 권장한다.

예:

```json
{
  "isSuccess": false,
  "errorCode": "SAVE_SKILL_FAILED",
  "message": "PlayerSkillLevels update failed"
}
```

로드 성공 응답 예:

```json
{
  "isSuccess": true,
  "schemaVersion": 1,
  "runtimeData": {
    "Currency": {
      "Gold": 10000,
      "UpgradeStone": 100,
      "Gem": 10000
    },
    "skillLevels": [
      { "skillId": 0, "level": 1 },
      { "skillId": 1, "level": 3 },
      { "skillId": 2, "level": 5 }
    ],
    "statLevels": {
      "attackLevel": 10,
      "hpLevel": 8,
      "hpRegenLevel": 3,
      "critChanceLevel": 6,
      "critDamageLevel": 4
    },
    "growth": {
      "growthLevel": 1,
      "nowExp": 0,
      "growthPoint": 0,
      "attackLevel": 1,
      "hpLevel": 1,
      "critDamageLevel": 1
    }
  }
}
```

## 구현 체크리스트

- [ ] 클라이언트와 `RuntimeData` JSON 계약 확정
- [ ] Save 요청에서 JSON 파싱 구현
- [ ] JSON -> 테이블별 저장 매핑 구현
- [ ] Load 요청에서 테이블별 조회 구현
- [ ] 테이블 값 -> RuntimeData JSON 조립 구현
- [ ] 신규 유저 기본 row 생성 구현
- [ ] 일부 테이블 누락 복구 구현
- [ ] 저장 실패 시 부분 성공 처리 정책 구현
- [ ] `schemaVersion` 저장 및 로드 구현
- [ ] 기본값 정책 구현
- [ ] 저장 전 검증 구현
- [ ] 실패 사유를 포함한 응답 구현
- [ ] 저장/로드 로그 구현

## 백엔드 담당자에게 전달할 핵심 문장

클라이언트는 `RuntimeData` JSON 한 덩어리를 저장 요청으로 보냅니다.

백엔드는 이 JSON을 파싱해서 `Currency`, `SkillLevels`, `StatLevels`, `Growth` 테이블로 나누어 뒤끝 DB에 저장해주세요.

로드 요청이 오면 백엔드는 뒤끝 DB의 여러 테이블 값을 다시 모아서, 클라이언트가 보낸 것과 같은 `RuntimeData` JSON 형태로 반환해주세요.

클라이언트는 테이블 구조를 알 필요 없이 항상 같은 JSON 구조만 주고받는 방식으로 유지합니다.
