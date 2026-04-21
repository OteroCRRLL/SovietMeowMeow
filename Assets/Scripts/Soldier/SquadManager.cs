using System.Collections.Generic;
using UnityEngine;

public class SquadManager : MonoBehaviour
{
    public Transform leader;
    
    // Almacena qué soldado del escuadrón está persiguiendo actualmente al Player
    private SoldierBrain currentHunter = null;

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
}