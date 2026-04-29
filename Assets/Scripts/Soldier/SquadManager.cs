using System.Collections.Generic;
using UnityEngine;

public class SquadManager : MonoBehaviour
{
    public Transform leader;
    public List<SoldierBrain> members = new List<SoldierBrain>();
    
    // Almacena qué soldado del escuadrón está persiguiendo actualmente al Player
    private SoldierBrain currentHunter = null;
    private bool wasInCombat = false;

    private void Update()
    {
        if (members.Count == 0) return;

        bool inCombat = IsSquadInCombat();
        if (inCombat != wasInCombat)
        {
            wasInCombat = inCombat;
            UpdateFormation(inCombat);
        }
    }

    public void InitializeSquad(List<SoldierBrain> squadMembers)
    {
        members = squadMembers;
        if (members.Count == 0) return;

        // El primero es el líder
        leader = members[0].transform;
        members[0].InitializeAsLeader();

        for (int i = 1; i < members.Count; i++)
        {
            members[i].InitializeAsFollower(Vector3.zero);
        }

        UpdateFormation(false);
    }

    private void UpdateFormation(bool inCombat)
    {
        for (int i = 1; i < members.Count; i++)
        {
            if (members[i] == null) continue;

            Vector3 offset = Vector3.zero;
            
            if (inCombat)
            {
                // Formación desplegada en combate (Línea amplia en V)
                int row = (i + 1) / 2; 
                float xSign = (i % 2 == 1) ? 1f : -1f; 
                
                float xOffset = 4f * row * xSign; // Muy separados lateralmente
                float zOffset = -1f * row; // Casi en línea horizontal
                
                offset = new Vector3(xOffset, 0f, zOffset);
            }
            else
            {
                // Formación en columna de 2x2 para patrullar
                int row = i / 2;
                int col = i % 2;
                
                float xOffset = (col == 1) ? 2.5f : 0f; // El impar se pone a la derecha del par
                float zOffset = -2.5f * row; // Distancia hacia atrás
                
                offset = new Vector3(xOffset, 0f, zOffset);
            }

            members[i].formationOffset = offset;
        }
    }

    public bool IsSquadInCombat()
    {
        foreach (SoldierBrain member in members)
        {
            if (member != null && member.gameObject.activeInHierarchy && member.CurrentState != SoldierState.Dead)
            {
                if (member.CurrentState == SoldierState.Combat || member.CurrentState == SoldierState.HuntPlayer)
                {
                    return true;
                }
            }
        }
        return false;
    }

    public bool IsPlayerHunted()
    {
        if (currentHunter != null)
        {
            if (currentHunter.gameObject.activeInHierarchy && currentHunter.CurrentState == SoldierState.HuntPlayer)
            {
                return true;
            }
            else
            {
                currentHunter = null;
                return false;
            }
        }
        return false;
    }

    public bool RequestHuntPlayer(SoldierBrain requester)
    {
        if (!IsPlayerHunted())
        {
            currentHunter = requester;
            return true;
        }
        return currentHunter == requester;
    }

    public void ClearHunter(SoldierBrain requester)
    {
        if (currentHunter == requester)
        {
            currentHunter = null;
        }
    }

    public void HandlePlayerSpotted(Transform player)
    {
        // Si no tenemos a nadie cazando al jugador o el que lo caza murió
        if (currentHunter == null || !currentHunter.gameObject.activeInHierarchy || currentHunter.CurrentState == SoldierState.Dead)
        {
            // Intentar asignar a un soldado que NO esté en combate con otros enemigos
            SoldierBrain bestHunter = null;
            foreach (SoldierBrain member in members)
            {
                if (member != null && member.gameObject.activeInHierarchy && member.CurrentState != SoldierState.Dead)
                {
                    if (bestHunter == null) 
                    {
                        bestHunter = member;
                    }
                    else if (member.CurrentState != SoldierState.Combat && bestHunter.CurrentState == SoldierState.Combat)
                    {
                        bestHunter = member; // Preferimos a alguien libre
                    }
                }
            }
            
            if (bestHunter != null)
            {
                currentHunter = bestHunter;
            }
        }
    }

    public bool IsHunter(SoldierBrain soldier)
    {
        return currentHunter == soldier;
    }

    public void AlertSquad(Transform target)
    {
        foreach (SoldierBrain member in members)
        {
            if (member != null && member.gameObject.activeInHierarchy)
            {
                member.ReceiveAlert(target);
            }
        }
    }

    public bool IsTargetEngaged(Transform target, SoldierBrain asker)
    {
        if (target == null) return false;
        
        foreach (SoldierBrain member in members)
        {
            if (member != asker && member != null && member.gameObject.activeInHierarchy && member.CurrentState != SoldierState.Dead)
            {
                // Si otro miembro del escuadrón sigue viendo al objetivo, confirmamos que sigue en combate
                if (member.CurrentTarget == target && member.HasLineOfSight)
                {
                    return true;
                }
            }
        }
        return false;
    }

    public bool IsFightingEnemies()
    {
        foreach (SoldierBrain member in members)
        {
            if (member != null && member.gameObject.activeInHierarchy && member.CurrentState != SoldierState.Dead)
            {
                if (member.CurrentState == SoldierState.Combat && member.CurrentTarget != null)
                {
                    FactionIdentity targetFaction = member.CurrentTarget.GetComponentInParent<FactionIdentity>();
                    if (targetFaction != null && targetFaction.myFaction != FactionType.Player)
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }

    public void RemoveMember(SoldierBrain soldier)
    {
        if (members.Contains(soldier))
        {
            members.Remove(soldier);
        }
        
        if (currentHunter == soldier)
        {
            currentHunter = null;
        }

        if (leader == soldier.transform)
        {
            if (members.Count > 0)
            {
                // Promover al siguiente de la lista como nuevo líder
                leader = members[0].transform;
                members[0].InitializeAsLeader();
            }
            else
            {
                leader = null;
                // El escuadrón ha sido completamente aniquilado
                Destroy(gameObject);
            }
        }
        else if (members.Count == 0)
        {
             Destroy(gameObject);
        }
    }
}