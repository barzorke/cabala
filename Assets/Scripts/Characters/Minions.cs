﻿using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.Networking;

[Serializable]
public enum MinionState
{
    stop,
    moving,
    fighting,
    dead
}

[Serializable]
public enum MinionType
{
    Ghost,
    Warrior,
    Wizard
}

public class Minions : NetworkBehaviour
{
    #region parameters
    public MinionsInformations minionsInformations;
    public MinionType minionType;

    [SerializeField]
    public Dictionary<MinionType, MinionType> StongAgainst;

    public MinionState DEBUGState;

    public MinionState state
    {
        get
        {
            return _state;
        }
        set
        {
            if (value != state)
            {
                previousState = _state;
                _state = value;
            }
        }
    }
    public MinionState previousState { get; protected set; }

    public Minions opponent { get; private set; }

    private MinionState _state;
    private NavMeshAgent navAgent;
    private Transform goal;
    private bool isInit = false;


    private Destructible _destructible;
    private Unit_ID _unit_ID;

    public int PlayerIndex
    {
        
        get
        {
            return _unit_ID.GetPlayerIndex();
        }
    }

    #endregion

    #region engine methods


    void Awake()
    {
        _destructible = GetComponent<Destructible>();
        _unit_ID = GetComponent<Unit_ID>();
        navAgent = GetComponent<NavMeshAgent>();

        _destructible.maxLife = minionsInformations.baseLifePoints;

        _destructible.HandleDestroyed += OnDie;
        _destructible.HandleAlive += OnAlive;
    }

    void LateUpdate()
    {
        DEBUGState = state;

        if (_unit_ID.IsReady())
        {
            initalize();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Minion")
        {
            Minions opponent = other.GetComponent<Minions>();
            if (opponent.PlayerIndex != PlayerIndex && opponent.state != MinionState.fighting)
            {
                LaunchFight(opponent);
            }
        }
    }

    #endregion

    #region methods


    protected void initalize()
    {
        if (isInit)
        {
            return;
        }
        isInit = true;

        state = MinionState.moving;

        setMaterial();

        int numberOfPlayer = GameObject.Find("GameSharedData").GetComponent<GameSharedData>().NumberOfPlayer;

        Debug.Log((PlayerIndex % numberOfPlayer) + 1);
        GameObject player = Unit_ID.FindPlayer((PlayerIndex % numberOfPlayer) + 1);
        SetGoal(player.transform);
    }

    private void setMaterial()
    {
        GetComponent<Renderer>().material = minionsInformations.teamMaterials[PlayerIndex];
    }

    public void SetGoal(Transform goalTransform)
    {
        Debug.Log(name + " goto " + goalTransform.name); 

        goal = goalTransform;
        if (goal != null)
        {
            navAgent.destination = goal.position;
        }
        else
        {
            navAgent.Stop();
        }
    }

    public void LaunchFight(Minions otherFighter)
    {
        if (state == MinionState.fighting)
        {
            return;
        }

        opponent = otherFighter;
        opponent.opponent = this;

        setupFight();
        opponent.setupFight();

        Invoke("Attack", minionsInformations.firstAttackSpeed);
    }

    public void finishFight()
    {
        if (_destructible.GetLife() <= 0)
        {
            return; // do nothing, will go on late state and die as it should
        }

        opponent = null;
        state = previousState;

        CancelInvoke("Attack");

        if (state == MinionState.moving)
        {
            navAgent.destination = goal.position;
            navAgent.Resume();
        }
    }

    public void setupFight()
    {
        state = MinionState.fighting;
        navAgent.Stop();
    }

    public void Attack()
    {
        Destructible opponentDestructible = opponent.GetComponent<Destructible>();
        opponentDestructible.TakeDamage(computeDamages(), _destructible);
        _destructible.TakeDamage(opponent.computeDamages(), opponentDestructible);

        Invoke("Attack", minionsInformations.attackSpeed);
    }

    public int computeDamages()
    {
        if (minionsInformations.minionStrengths.amIStrongAgainst(minionType, opponent.minionType))
        {
            return (int)((float)minionsInformations.damages * minionsInformations.damageMultiplicator);
        }

        return minionsInformations.damages;
    }

    private void OnDie(GameObject whoDied, Destructible whokill)
    {
        if (opponent != null)
        {
            opponent.finishFight();
        }

        state = MinionState.dead;

        //      Destroy(gameObject);
   }
    private void OnAlive(GameObject whoAlive)
    {
        isInit = false;
        initalize();
    }

    #endregion
}
