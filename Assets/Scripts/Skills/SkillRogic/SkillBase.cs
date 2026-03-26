using UnityEngine;

/// <summary>
/// 모든 스킬 실행체의 공통 베이스 클래스.
/// SkillManager는 공통 계산 결과를 이 클래스에 넘기고,
/// 실제 실행은 각 자식 클래스가 담당한다.
/// </summary>
public abstract class SkillBase : MonoBehaviour
{
    protected SkillData skillData;
    protected SkillCastResult castResult;

    public virtual void Initialize(SkillData skillData, SkillCastResult castResult)
    {
        this.skillData = skillData;
        this.castResult = castResult;
    }

    public abstract void Execute();
}