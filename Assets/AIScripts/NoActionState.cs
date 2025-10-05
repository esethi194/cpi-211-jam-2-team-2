using UnityEngine;

public class NoActionState : MonsterState
{
    public override void OnEnterState(MonsterAI monsterAI)
    {
        base.OnEnterState(monsterAI);
        monster.agent.isStopped = true;
        monster.gameObject.SetActive(false);
        Debug.Log("NoActionState");
    }

    public override void OnUpdateState()
    {
        
    }
}
