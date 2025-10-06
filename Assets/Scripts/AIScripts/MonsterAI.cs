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
    
    //Charge
    public float chargeSpeed = 12f;
    
    //current monster state
    private MonsterState currentState; 
    
    // sound
    private SoundManager soundManager;
    
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        soundManager = GetComponent<SoundManager>();
        
        ChangeState(new NoActionState());
        
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
    public void UpdateMonsterState()
    {

        int count = GameManager.instance.monstersCount;
        
        soundManager.PlaySoundForState(count);
        
        if (count == 1)
        {
           
            ChangeState(new RoamingState());
        }
        else if (count == 2)
        {
            
            ChangeState(new StalkingState());
        }
        else if (count >= 3)
        {
            ChangeState(new ChargeState());
        }
        
    }
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.tag == "Player" && GameManager.instance.monstersCount == 3)
        {
            GameManager.instance.GameOver();
        }
        
        
    }
}
