using UnityEngine;
using UnityEngine.AI;

public class StalkingState : MonsterState
{
    public override void OnEnterState(MonsterAI monsterAI)
    {
        base.OnEnterState(monsterAI);
        // activate the monster
        monster.gameObject.GetComponent<MeshRenderer>().enabled = true;
        // activate the navmesh agent
        monster.agent.isStopped = false;
        monster.agent.speed = monster.stalkingSpeed;

        StalkPlayer();
        
        Debug.Log("StalkingState Enter");
    }

    private void StalkPlayer()
    {
        Vector3 playerPos = monster.player.transform.position;
        Vector3 direction = (monster.transform.position - playerPos).normalized;
        Vector3 targetPos = playerPos + direction * monster.stalkingDistance;


        NavMeshHit hit;
        if (NavMesh.SamplePosition(targetPos, out hit, 1f, 1))
        {
            monster.agent.SetDestination(hit.position);
        }

    }

    public override void OnUpdateState()
    {
        if (Vector3.Distance(monster.transform.position, monster.player.transform.position) < monster.disappearingDistance)
        {
            // The player is too close, hide the monster
            monster.gameObject.GetComponent<MeshRenderer>().enabled = false;
        }
        // Keep checking distance to player and reposition
        else if (Vector3.Distance(monster.transform.position, monster.player.transform.position) > monster.stalkingDistance)
        {
            StalkPlayer();
        }
    }

    public override void OnExitState()
    {
        // monster fades after player gets close
    }
}
