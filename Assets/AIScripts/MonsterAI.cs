using UnityEngine;
using UnityEngine.AI;

public class MonsterAI : MonoBehaviour
{
    
    public Transform player;
    public NavMeshAgent agent;
    
    //Roaming
    public float roamingRadius = 20f;
    
    //Stalking
    public float stalkingSpeed = 2f;
    public float stalkingDistance = 7f;
    public float disappearingDistance = 5f;
    
    //current monster state
    private MonsterState currentState; 
        
        // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        ChangeState(new RoamingState());
        
    }

    // Update is called once per frame
    void Update()
    {
        if (currentState != null)
        {
            currentState.OnUpdateState();
        }
    }

    public void ChangeState(MonsterState newState)
    {
        if (currentState != null)
        {
            currentState.OnExitState();
        }
        currentState = newState;
        currentState.OnEnterState(this);
    }
    
}
