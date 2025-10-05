using UnityEngine;

public abstract class MonsterState
{
    protected MonsterAI monster;

    public virtual void OnEnterState(MonsterAI monsterAI)
    {
        this.monster = monsterAI;
    }
    
    public abstract void OnUpdateState();
    
    public virtual void OnExitState()
    {
        
    }
}
