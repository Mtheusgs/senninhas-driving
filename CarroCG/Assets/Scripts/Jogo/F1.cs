using UnityEngine;

// F1 com tração traseira: motor na roda traseira, torque no chassi quando está no ar, sensor no capacete.
[RequireComponent(typeof(Rigidbody2D))]
public class F1 : MonoBehaviour
{
    public WheelJoint2D eixoTraseiro, eixoDianteiro;
    public float velocidadeMotor = 2400f;      // graus/s na roda
    public float torqueMotor = 40f;
    public float torqueNoAr = 16f;                 // gira > 90° em ~1 s no ar (ConfigurarPartida usa o do modo)
    public Vector2 centroDeMassa = new(0, 0.05f);  // menos baixo: dá para empinar e capotar
    public bool controlePeloTeclado = true;
    [Range(-1, 1)] public float acelerador;
    public bool motorLigado = true;

    public event System.Action Capotou;
    public bool NoChao { get; private set; }
    public float VelocidadeX => rb.linearVelocity.x;
    public float Velocidade => rb.linearVelocity.magnitude;

    Rigidbody2D rb;
    Collider2D rodaT, rodaD;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.centerOfMass = centroDeMassa;
        rodaT = eixoTraseiro.connectedBody.GetComponent<Collider2D>();
        rodaD = eixoDianteiro.connectedBody.GetComponent<Collider2D>();
    }

    void Update()
    {
        if (controlePeloTeclado)
            acelerador = (Input.GetKey(KeyCode.RightArrow) ? 1 : 0) - (Input.GetKey(KeyCode.LeftArrow) ? 1 : 0);
    }

    void FixedUpdate()
    {
        NoChao = rodaT.IsTouchingLayers() || rodaD.IsTouchingLayers();
        float a = motorLigado ? acelerador : 0;
        eixoTraseiro.useMotor = a != 0;                                    // sem acelerar: roda livre
        eixoTraseiro.motor = new JointMotor2D { motorSpeed = -a * velocidadeMotor, maxMotorTorque = torqueMotor };
        if (!NoChao) rb.AddTorque(acelerador * torqueNoAr);                // → levanta o bico, ← abaixa (rotação)
    }

    public void NotificarCapotou() => Capotou?.Invoke();
}
