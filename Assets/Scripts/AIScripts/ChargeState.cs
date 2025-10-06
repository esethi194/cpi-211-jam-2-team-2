using Unity.VisualScripting;
using UnityEngine;

public class ChargeState : MonsterState
{
    public override void OnEnterState(MonsterAI monsterAI)
    {
        base.OnEnterState(monsterAI);
        // activate the monster
        monster.gameObject.GetComponent<SpriteRenderer>().enabled = false;
        monster.gameObject.GetComponent<CapsuleCollider>().enabled = true;
        // activate the navmesh agent
        monster.agent.isStopped = false;
        monster.agent.speed = monster.chargeSpeed;
        
        monster.agent.destination = monster.player.transform.position;
        
        Debug.Log("Charge state");
    }

    public override void OnUpdateState()
    {
        //chase after player
        monster.agent.destination = monster.player.transform.position;
    }

    
}
