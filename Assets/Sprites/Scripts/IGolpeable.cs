using UnityEngine;

// Todo lo que el látigo puede golpear (enemigos, rocas, bolas de fuego) implementa esta interfaz.
public interface IGolpeable
{
    void RecibirLatigazo(Vector2 desde);
}
