using System.Collections;
using UnityEngine;
using NETWORK_ENGINE;

public class NetworkRigidBody : NetworkComponent
{
    public Rigidbody MyRig;

    public override void NetworkedStart()
    {
        MyRig = GetComponent<Rigidbody>();
        if (MyRig == null)
        {
            throw new System.Exception("No Rigidbody found on this GameObject!");
        }
    }

    public override IEnumerator SlowUpdate()
    {
        while (true)
        {
            if (IsServer)
            {
                SendUpdate("RIGIDBODY_STATE", SerializeState());
            }
            yield return new WaitForSeconds(0.033f); 
        }
    }

    public override void HandleMessage(string flag, string value)
    {
        if (flag == "RIGIDBODY_STATE" && !IsServer)
        {
            ApplyState(value);
        }
    }

    public string SerializeState()
    {
        Vector3 pos = MyRig.position;
        Quaternion rot = MyRig.rotation;
        Vector3 vel = MyRig.velocity;
        Vector3 angVel = MyRig.angularVelocity;
        return $"{pos.x},{pos.y},{pos.z};{rot.x},{rot.y},{rot.z},{rot.w};{vel.x},{vel.y},{vel.z};{angVel.x},{angVel.y},{angVel.z}";
    }

    public void ApplyState(string state)
    {
        string[] parts = state.Split(';');
        if (parts.Length < 4) return;

        string[] posSplit = parts[0].Split(',');
        Vector3 newPos = new Vector3(float.Parse(posSplit[0]), float.Parse(posSplit[1]), float.Parse(posSplit[2]));

        string[] rotSplit = parts[1].Split(',');
        Quaternion newRot = new Quaternion(float.Parse(rotSplit[0]), float.Parse(rotSplit[1]), float.Parse(rotSplit[2]), float.Parse(rotSplit[3]));

        string[] velSplit = parts[2].Split(',');
        Vector3 newVel = new Vector3(float.Parse(velSplit[0]), float.Parse(velSplit[1]), float.Parse(velSplit[2]));

        string[] angVelSplit = parts[3].Split(',');
        Vector3 newAngVel = new Vector3(float.Parse(angVelSplit[0]), float.Parse(angVelSplit[1]), float.Parse(angVelSplit[2]));

        MyRig.position = newPos;
        MyRig.rotation = newRot;
        MyRig.velocity = newVel;
        MyRig.angularVelocity = newAngVel;

        Debug.Log($"[NetworkRigidBody] Applied State - Position: {newPos}, Velocity: {newVel}");
    }
}

