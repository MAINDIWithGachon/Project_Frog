using UnityEngine;

public interface ISkillExecutable
{
    int SkillId { get; }
    void Execute(SkillData skillData, SkillCastResult castResult);
}
