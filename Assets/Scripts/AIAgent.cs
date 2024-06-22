using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class AIAgent : MonoBehaviour
{
    public enum State
    {
        Idle,
        Chasing,
        ReturningFlag,
        GoingToPrison,
        Captured,
        Escorting,
        BeingEscorted,
        Wandering,
        CapturingFlag,
        Rescuing,
        Evading
    }

    public Team Team;
    public bool IsControlledByPlayer;
    public Transform Prison;
    public List<AIAgent> Allies = new();
    public List<AIAgent> Enemies = new();
    public List<Flag> EnemyFlags = new();
    public float Speed = 3f;

    public float WanderMinX;
    public float WanderMaxX;
    public float WanderMinY;
    public float WanderMaxY;

    public State CurrentState = State.Idle;
    public AIAgent EscortedAgent;
    public Flag CarriedFlag;
    private Vector3 _prisonPosition;
    private AIAgent _targetAgent;
    private Vector3 _wanderTarget;
    private Flag _targetFlag;
    private AIAgent _targetAlly;
    private float _wanderTimer;
    private const float MaxWanderTime = 5f; // Maximum time to wander before re-evaluating decisions
    private const float EvadeRange = 5f; // Distance at which the AI should start evading

    private void Update()
    {
        if (!IsControlledByPlayer)
        {
            UpdateAI();
            ClampPosition();
        }

        if (CarriedFlag != null) CarriedFlag.transform.position = transform.position;

        if (EscortedAgent != null) EscortedAgent.transform.position = transform.position;
    }

    private void ClampPosition()
    {
        var clampedX = Mathf.Clamp(transform.position.x, -16f, 16f);
        var clampedY = Mathf.Clamp(transform.position.y, -9f, 9f);
        transform.position = new Vector3(clampedX, clampedY, transform.position.z);
    }

    private void UpdateAI()
    {
        switch (CurrentState)
        {
            case State.Idle:
                MakeDecision();
                break;
            case State.Chasing:
                ChaseTarget();
                break;
            case State.ReturningFlag:
                ReturnFlag();
                break;
            case State.GoingToPrison:
                GoToPrison();
                break;
            case State.Captured:
                // Do nothing
                break;
            case State.Escorting:
                EscortAlly();
                break;
            case State.BeingEscorted:
                // Do nothing
                break;
            case State.Wandering:
                Wander();
                break;
            case State.CapturingFlag:
                CaptureTargetFlag();
                break;
            case State.Rescuing:
                RescueTargetAlly();
                break;
            case State.Evading:
                Evade();
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void Evade()
    {
        var evadeDirection = Vector3.zero;
        var closestDistance = float.MaxValue;

        foreach (var agent in Enemies)
        {
            var distance = Vector3.Distance(transform.position, agent.transform.position);
            if (!(distance < closestDistance) || !(distance < EvadeRange)) continue;
            closestDistance = distance;
            evadeDirection = transform.position - agent.transform.position;
        }

        evadeDirection.Normalize();
        var evadeTarget = transform.position + evadeDirection * EvadeRange;

        MoveTowards(evadeTarget);

        // Check if the AI has successfully evaded and can continue its previous task
        if (!IsInOwnTerritory() && !(Vector3.Distance(transform.position, evadeTarget) < 0.1f)) return;
        // Determine the previous task and return to it
        if (CarriedFlag != null)
        {
            CurrentState = State.ReturningFlag;
        }
        else if (EscortedAgent != null)
        {
            CurrentState = State.Escorting;
        }
        else
        {
            CurrentState = State.Idle;
            MakeDecision();
        }
    }

    private void MakeDecision()
    {
        // Skip decision making if in critical states
        if (CurrentState is State.Captured or State.GoingToPrison or State.BeingEscorted) return;

        if (FindTarget())
            CurrentState = State.Chasing;
        else
            switch (Random.value)
            {
                // % chance to go capture a flag
                case < 0.3f when FindFlagToCapture():
                    CurrentState = State.CapturingFlag;
                    break;
                // % chance to rescue an ally
                case < 0.3f when FindCapturedAlly():
                    CurrentState = State.Rescuing;
                    break;
                default:
                    StartWandering();
                    break;
            }
    }

    private bool FindTarget()
    {
        AIAgent closestTarget = null;
        var closestDistance = float.MaxValue;

        foreach (var agent in Enemies)
        {
            if (agent.CurrentState == State.Captured || agent.CurrentState == State.GoingToPrison ||
                !IsAgentInMyTerritory(agent)) continue;
            var distance = Vector3.Distance(transform.position, agent.transform.position);

            if (!(distance < closestDistance) || GetChasersCount(agent) >= 2) continue;
            closestTarget = agent;
            closestDistance = distance;
        }

        if (closestTarget == null) return false;
        _targetAgent = closestTarget;
        return true;
    }

    private void ChaseTarget()
    {
        if (_targetAgent == null || _targetAgent.CurrentState == State.Captured ||
            _targetAgent.CurrentState == State.GoingToPrison || !IsAgentInMyTerritory(_targetAgent))
        {
            CurrentState = State.Idle;
            return;
        }

        var currentDistance = Vector3.Distance(transform.position, _targetAgent.transform.position);
        foreach (var agent in Enemies)
        {
            if (agent == _targetAgent || agent.CurrentState == State.Captured ||
                agent.CurrentState == State.GoingToPrison || !IsAgentInMyTerritory(agent)) continue;
            var distance = Vector3.Distance(transform.position, agent.transform.position);
            if (!(distance < currentDistance) || GetChasersCount(agent) >= 2) continue;
            _targetAgent = agent;
            currentDistance = distance;
        }

        MoveTowards(_targetAgent.transform.position);
    }

    private int GetChasersCount(AIAgent agent)
    {
        return Allies.Count(a => a.CurrentState == State.Chasing && a._targetAgent == agent);
    }

    private void ReturnFlag()
    {
        if (IsInOwnTerritory())
        {
            CaptureFlag();
        }
        else
        {
            var evadeTarget = CalculateEvadeTarget(new Vector3(Team == Team.Red ? 10 : -10, transform.position.y,
                transform.position.z));
            MoveTowards(evadeTarget);
        }
    }

    private void GoToPrison()
    {
        if (Vector3.Distance(transform.position, _prisonPosition) <= 0.1f)
        {
            CurrentState = State.Captured;
            transform.position = _prisonPosition;
        }
        else
        {
            MoveTowards(_prisonPosition);
        }
    }

    private void StartWandering()
    {
        _wanderTarget = new Vector3(
            Random.Range(WanderMinX, WanderMaxX),
            Random.Range(WanderMinY, WanderMaxY),
            transform.position.z
        );
        _wanderTimer = MaxWanderTime;
        CurrentState = State.Wandering;
    }

    private void Wander()
    {
        _wanderTimer -= Time.deltaTime;

        if (Vector3.Distance(transform.position, _wanderTarget) <= 0.1f || _wanderTimer <= 0)
        {
            CurrentState = State.Idle;
            MakeDecision();
        }
        else
        {
            MoveTowards(_wanderTarget);
        }
    }

    private bool FindFlagToCapture()
    {
        Flag closestFlag = null;
        var closestDistance = float.MaxValue;

        foreach (var flag in EnemyFlags)
        {
            if (flag == null) continue;

            if (flag.IsBeingCarried) continue;
            var distance = Vector3.Distance(transform.position, flag.transform.position);
            if (!(distance < closestDistance)) continue;
            closestFlag = flag;
            closestDistance = distance;
        }

        if (closestFlag == null) return false;
        _targetFlag = closestFlag;
        return true;
    }

    private void CaptureTargetFlag()
    {
        if (_targetFlag == null || _targetFlag.IsBeingCarried)
        {
            CurrentState = State.Idle;
            MakeDecision();
            return;
        }

        var evadeTarget = CalculateEvadeTarget(_targetFlag.transform.position);
        MoveTowards(evadeTarget);

        if (Vector3.Distance(transform.position, _targetFlag.transform.position) <= 0.1f) PickUpFlag(_targetFlag);
    }

    private bool FindCapturedAlly()
    {
        AIAgent closestAlly = null;
        var closestDistance = float.MaxValue;

        foreach (var ally in Allies)
        {
            if (ally == null || ally.CurrentState != State.Captured) continue;

            var distance = Vector3.Distance(transform.position, ally.transform.position);
            if (!(distance < closestDistance)) continue;
            closestAlly = ally;
            closestDistance = distance;
        }

        if (closestAlly == null) return false;
        _targetAlly = closestAlly;
        return true;
    }

    private void RescueTargetAlly()
    {
        if (_targetAlly == null || _targetAlly.CurrentState != State.Captured)
        {
            CurrentState = State.Idle;
            MakeDecision();
            return;
        }

        var evadeTarget = CalculateEvadeTarget(_targetAlly.transform.position);
        MoveTowards(evadeTarget);

        if (Vector3.Distance(transform.position, _targetAlly.transform.position) <= 0.1f) StartEscorting(_targetAlly);
    }

    private Vector3 CalculateEvadeTarget(Vector3 target)
    {
        var evadeDirection = Vector3.zero;
        var closestDistance = float.MaxValue;

        foreach (var agent in Enemies)
        {
            var distance = Vector3.Distance(transform.position, agent.transform.position);
            if (!(distance < closestDistance) || !(distance < EvadeRange)) continue;
            closestDistance = distance;
            evadeDirection = transform.position - agent.transform.position;
        }

        evadeDirection.Normalize();
        return target + evadeDirection * EvadeRange;
    }

    private void MoveTowards(Vector3 target)
    {
        var direction = (target - transform.position).normalized;
        transform.position += direction * Speed * Time.deltaTime;
    }

    public void PickUpFlag(Flag flag)
    {
        if (CurrentState == State.Captured || CurrentState == State.GoingToPrison || CarriedFlag != null ||
            EscortedAgent != null || !EnemyFlags.Contains(flag) || flag.IsBeingCarried) return;
        CarriedFlag = flag;
        flag.IsBeingCarried = true;
        flag.transform.SetParent(transform); // Make the flag a child of the agent
        CurrentState = State.ReturningFlag;
        Debug.Log($"{gameObject.name} picked up a flag");
        // Notify other agents to avoid targeting this flag
        foreach (var agent in Allies.Where(agent =>
                     agent != this && agent.CurrentState == State.CapturingFlag && agent._targetFlag == flag))
        {
            agent._targetFlag = null;
            agent.MakeDecision();
        }
    }

    public void CaptureFlag()
    {
        if (CarriedFlag == null) return;
        CarriedFlag.IsBeingCarried = false;
        CarriedFlag.transform.SetParent(null); // Detach the flag from the agent
        GameManager.Instance.CaptureFlag(Team);
        Destroy(CarriedFlag.gameObject);
        EnemyFlags.Remove(CarriedFlag);
        CarriedFlag = null;
        CurrentState = State.Idle;
        MakeDecision();
    }

    private bool IsInOwnTerritory()
    {
        return (Team == Team.Red && transform.position.x >= 0) || (Team == Team.Blue && transform.position.x <= 0);
    }

    private bool IsInEnemyTerritory()
    {
        return (Team == Team.Red && transform.position.x < 0) || (Team == Team.Blue && transform.position.x > 0);
    }

    private bool IsAgentInMyTerritory(AIAgent agent)
    {
        return (Team == Team.Red && agent.transform.position.x > 0) ||
               (Team == Team.Blue && agent.transform.position.x < 0);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        var enemyAgent = collision.GetComponent<AIAgent>();
        if (enemyAgent != null && enemyAgent.Team != Team && enemyAgent.CurrentState != State.Captured &&
            enemyAgent.CurrentState != State.GoingToPrison)
            if (IsAgentInMyTerritory(enemyAgent))
            {
                enemyAgent.CurrentState = State.GoingToPrison;
                enemyAgent._prisonPosition = GetRandomPrisonPosition();
                if (enemyAgent.CarriedFlag != null)
                {
                    enemyAgent.CarriedFlag.ResetPosition();
                    enemyAgent.CarriedFlag.IsBeingCarried = false;
                    enemyAgent.CarriedFlag = null;
                }

                // Release control if the player is controlling the tagged agent
                if (enemyAgent.IsControlledByPlayer)
                {
                    enemyAgent.IsControlledByPlayer = false;
                    FindObjectOfType<PlayerController>().ReleaseControl();
                }
            }

        var flag = collision.GetComponent<Flag>();
        if (flag != null) PickUpFlag(flag);

        var allyAgent = collision.GetComponent<AIAgent>();
        if (allyAgent != null && allyAgent.Team == Team && allyAgent.CurrentState == State.Captured)
            StartEscorting(allyAgent);
    }

    private Vector3 GetRandomPrisonPosition()
    {
        var prisonBounds = Prison.GetComponent<BoxCollider2D>();
        var minBounds = prisonBounds.bounds.min;
        var maxBounds = prisonBounds.bounds.max;

        var randomX = Random.Range(minBounds.x, maxBounds.x);
        var randomY = Random.Range(minBounds.y, maxBounds.y);

        return new Vector3(randomX, randomY, transform.position.z);
    }

    public void FreeFromPrison()
    {
        CurrentState = State.Idle;
        MakeDecision();
    }

    public void StartEscorting(AIAgent allyAgent)
    {
        if (CurrentState == State.Captured || CurrentState == State.GoingToPrison || CarriedFlag != null ||
            allyAgent == null || allyAgent.Team != Team || EscortedAgent != null) return;

        EscortedAgent = allyAgent;
        EscortedAgent.CurrentState = State.BeingEscorted;
        CurrentState = State.Escorting;
    }

    private void EscortAlly()
    {
        if (IsInOwnTerritory())
        {
            EscortedAgent.FreeFromPrison();
            EscortedAgent = null;
            CurrentState = State.Idle;
            MakeDecision();
        }
        else
        {
            var evadeTarget = CalculateEvadeTarget(new Vector3(Team == Team.Red ? 10 : -10, transform.position.y,
                transform.position.z));
            MoveTowards(evadeTarget);
        }
    }
}