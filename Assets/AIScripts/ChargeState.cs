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
        monster.agent.speed = 12f;
        
        monster.agent.destination = monster.player.transform.position;
        
        Debug.Log("Charge state");
    }

    public override void OnUpdateState()
    {
        //chase after player
        monster.agent.destination = monster.player.transform.position;

        if (Vector3.Distance(monster.transform.position, monster.player.transform.position) <
            monster.agent.stoppingDistance + 0.5f)
        {
            GameManager.instance.GameOver();
            monster.agent.isStopped = true;
        }
    }
}
