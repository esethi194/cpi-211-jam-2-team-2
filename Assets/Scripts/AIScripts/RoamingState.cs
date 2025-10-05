using UnityEngine;
using UnityEngine.AI;

public class RoamingState : MonsterState
{
   public override void OnEnterState(MonsterAI monsterAI)
   {
      base.OnEnterState(monsterAI);
      // activate the monster
      monster.gameObject.GetComponent<MeshRenderer>().enabled = false;
      // activate the navmesh agent
      monster.agent.isStopped = false;
      FindNewDestination();
      Debug.Log("Roaming state");
      
   }

   public override void OnUpdateState()
   {
      // If the monster has arrived at its destination, find a new one
      if (!monster.agent.pathPending && monster.agent.remainingDistance <= monster.agent.stoppingDistance)
      {
         FindNewDestination();
      }
      
      
      
   }

   private void FindNewDestination()
   {
      // Find a random point within a sphere around the monster
      Vector3 randomDirection = Random.insideUnitSphere * monster.roamingRadius;
      randomDirection += monster.transform.position;

      NavMeshHit hit;
      Vector3 finalDestination = Vector3.zero;
      
      // Use NavMesh.SamplePosition to find the closest valid point on the NavMesh
      if (NavMesh.SamplePosition(randomDirection, out hit, monster.roamingRadius, 1))
      {
         finalDestination = hit.position;
         monster.agent.SetDestination(finalDestination);
      }


   }
   
}
