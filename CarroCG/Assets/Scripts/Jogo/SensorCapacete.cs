using UnityEngine;

// Arquivo próprio: a Unity só serializa na cena um MonoBehaviour cujo arquivo tem o mesmo nome da classe.
public class SensorCapacete : MonoBehaviour
{
    public F1 f1;

    void OnTriggerEnter2D(Collider2D outro)
    {
        if (outro is EdgeCollider2D) f1.NotificarCapotou();
    }
}
