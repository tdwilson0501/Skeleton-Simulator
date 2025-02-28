using System.Collections;
using UnityEngine;
using NETWORK_ENGINE;
using UnityEngine.InputSystem;

public class NetworkPlayerController : NetworkComponent
{
    [Header("Skeleton Appearance")]
    public Material[] SkeletonMatArray;
    public Renderer SkeletonRenderer;

    [Header("Physics & Animation")]
    public NetworkRigidBody NetworkPhysics;
    public Animator MyAnime;

    [Header("Movement Settings")]
    public float moveForce = 1000f;  
    public float turnSpeed = 400f;   
    public float maxSpeed = 10f;     

    [Header("Attack Settings")]
    public float attackCooldown = 0.5f; 
    private float lastAttackTime = 0f;

    private Vector2 moveInput = Vector2.zero;
    private Vector2 latestMoveInput = Vector2.zero;

    public override void NetworkedStart()
    {
        SkeletonRenderer.material = SkeletonMatArray[this.Owner % SkeletonMatArray.Length];

        if (IsServer)
        {
            int spawnIndex = (this.Owner % 3) + 1;
            GameObject spawn = GameObject.Find("SpawnPoint" + spawnIndex);
            if (spawn != null)
            {
                Vector3 spawnPos = spawn.transform.position;
                spawnPos.y = Mathf.Max(spawnPos.y, 0.5f); 
                NetworkPhysics.MyRig.position = spawnPos;
                NetworkPhysics.MyRig.rotation = spawn.transform.rotation;
            }
            NetworkPhysics.MyRig.useGravity = true;
        }

        NetworkPhysics.MyRig.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        NetworkPhysics.MyRig.drag = 0.05f;  
        NetworkPhysics.MyRig.angularDrag = 2f; 
        NetworkPhysics.MyRig.mass = 1f;    

    }

    public override IEnumerator SlowUpdate()
    {
        while (true)
        {
            if (IsLocalPlayer)
            {
                SendCommand("MOVE_INPUT", $"{moveInput.x},{moveInput.y}");
            }
            if (IsServer)
            {
                SendUpdate("RIGIDBODY_STATE", NetworkPhysics.SerializeState());
                float speed = NetworkPhysics.MyRig.velocity.magnitude;
                SendUpdate("ANIM_SPEED", speed.ToString("F2")); 
            }
            yield return new WaitForSeconds(0.033f); 
        }
    }

    public override void HandleMessage(string flag, string value)
    {
        if (IsServer)
        {
            if (flag == "MOVE_INPUT")
            {
                string[] inputs = value.Split(',');
                float turn = float.Parse(inputs[0]);
                float move = float.Parse(inputs[1]);
                latestMoveInput = new Vector2(turn, move);
            }
            else if (flag == "ANIM_ATTACK" && Time.time - lastAttackTime >= attackCooldown)
            {
                lastAttackTime = Time.time;
                SendUpdate("ANIM_ATTACK", "TRIGGER");
            }
        }

        if (!IsLocalPlayer) 
        {
            if (flag == "RIGIDBODY_STATE")
            {
                NetworkPhysics.ApplyState(value);
            }
            else if (flag == "ANIM_SPEED")
            {
                float speed;
                if (float.TryParse(value, out speed))
                {
                    MyAnime.SetFloat("speedh", speed > 0.1f ? 1f : 0f); 
                }
            }
            else if (flag == "ANIM_ATTACK")
            {
                if (MyAnime != null)
                {
                    MyAnime.SetTrigger("Attack1h1");
                }
            }
        }
    }

    void FixedUpdate()
    {
        if (IsServer)
        {
            Vector3 force = transform.forward * latestMoveInput.y * moveForce * Time.fixedDeltaTime;
            NetworkPhysics.MyRig.AddForce(force, ForceMode.Impulse);

            float turnAmount = latestMoveInput.x * turnSpeed * Time.fixedDeltaTime;
            Quaternion deltaRotation = Quaternion.Euler(0, turnAmount, 0);
            NetworkPhysics.MyRig.MoveRotation(NetworkPhysics.MyRig.rotation * deltaRotation);

            if (NetworkPhysics.MyRig.velocity.magnitude > maxSpeed)
            {
                NetworkPhysics.MyRig.velocity = NetworkPhysics.MyRig.velocity.normalized * maxSpeed;
            }

        }
    }

    public void OnDirectionChanged(InputAction.CallbackContext context)
    {
        if (!IsLocalPlayer) return;
        if (context.phase == InputActionPhase.Performed || context.phase == InputActionPhase.Started)
        {
            moveInput = context.ReadValue<Vector2>();
        }
        else if (context.phase == InputActionPhase.Canceled)
        {
            moveInput = Vector2.zero;
        }
    }

    public void OnFire(InputAction.CallbackContext context)
    {
        if (!IsLocalPlayer || context.phase != InputActionPhase.Started) return;
        if (Time.time - lastAttackTime >= attackCooldown)
        {
            MyAnime.SetTrigger("Attack1h1");
            SendCommand("ANIM_ATTACK", "TRIGGER");
        }
    }

    void Update()
    {
        if (IsLocalPlayer && Camera.main != null)
        {
            Camera.main.transform.position = Vector3.Lerp(
                Camera.main.transform.position,
                transform.position + new Vector3(0, 8, -8),
                Time.deltaTime * 5f
            );
            Camera.main.transform.LookAt(transform.position + Vector3.up);
            MyAnime.SetFloat("speedh", NetworkPhysics.MyRig.velocity.magnitude > 0.1f ? 1f : 0f); 
        }
    }
}



