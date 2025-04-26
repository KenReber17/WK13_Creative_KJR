using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))] // Fixed typo in attribute name
public class AITarget : MonoBehaviour
{
    public Transform Target;
    public float AttackDistance;
    public float RunDistance;

    private NavMeshAgent m_Agent;
    private Animator m_Animator;
    private float m_Distance;
    //private Vector3 m_StartingPoint;
    //private bool m_PathCalculate = true;

    void Start()
    {
        m_Agent = GetComponent<NavMeshAgent>();
        m_Animator = GetComponent<Animator>();
        //m_StartingPoint = transform.position;
    }

     public void NewEvent()
    {
        Debug.Log("NewEvent triggered!"); // Test with a log
    }

    public void PlayStep()
    {
        Debug.Log("PlayStep triggered!"); // Test with a log
    }

    public void StartAttack()
    {
        Debug.Log("StartAttack triggered!"); // Test with a log
    }

    public void EndAttack()
    {
        Debug.Log("EndAttack triggered!"); // Test with a log
    }



    void Update()
    {
        m_Distance = Vector3.Distance(m_Agent.transform.position, Target.position); // Fixed method name and property
        if (m_Distance < AttackDistance)
        {
            m_Agent.isStopped = true;
            m_Animator.SetBool("Attack", true);
        }
        else
        {
            m_Agent.isStopped = false;
            // 2 lines removed to update Enemy return to starting point, if out of range
            m_Animator.SetBool("Attack", false);
            m_Agent.destination = Target.position;
            //if (!m_Agent.hasPath && m_PathCalculate)
            //{
                //m_Agent.destination = m_StartingPoint;
                //m_PathCalculate = false;
            //}
            //else
            //{
                //m_Animator.SetBool("Attack", false);
                //m_Agent.destination = Target.position;
                //m_PathCalculate = true;
            //}

        }

        

    }

    void OnAnimatorMove()
    {
        if (!m_Animator.GetBool("Attack")) // Simplified boolean check
        {
            m_Agent.speed = (m_Animator.deltaPosition / Time.deltaTime).magnitude;
        }
    }
}