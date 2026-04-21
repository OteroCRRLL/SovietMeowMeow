using UnityEngine;

public enum FactionType
{
    Player,
    Soviet,
    MeowMeow
}

public class FactionIdentity : MonoBehaviour
{
    public FactionType myFaction;

    public bool IsEnemy(FactionType otherFaction)
    {
        // El Player es enemigo de todos, y Soviet es enemigo de MeowMeow.
        // Si las facciones son distintas, son enemigos.
        return myFaction != otherFaction;
    }
}
