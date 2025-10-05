using UnityEngine;

public class ChargeState : MonsterState
{
    public override void OnEnterState(MonsterAI monsterAI)
    {
        base.OnEnterState(monsterAI);
        // activate the monster
        monster.gameObject.SetActive(true);
        // activate the navmesh agent
        monster.agent.isStopped = false;
        monster.agent.speed = 8f;
        
        monster.agent.destination = monster.player.transform.position;
        
        Debug.Log("Charge state");
    }

    public override void OnUpdateState()
    {
        //chase after player
        monster.agent.destination = monster.player.transform.position;

        if (Vector3.Distance(monster.transform.position, monster.player.transform.position) <
            monster.agent.stoppingDistance)
        {
            
            monster.agent.isStopped = true;
        }
    }
}
