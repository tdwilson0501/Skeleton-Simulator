using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NETWORK_ENGINE;
using UnityEngine.AI;

public class EnemySkeleton : NetworkComponent
{
    [Header("Navigation & Animation")]
    public NavMeshAgent MyAgent;
    public Animator MyAnime;
    public List<Vector3> Goals;
    public Vector3 CurrentGoal;

    public override void NetworkedStart()
    {
        MyAgent = GetComponent<NavMeshAgent>();
        MyAnime = GetComponent<Animator>();

        if (IsServer)
        {
            if (!MyId.IsInit)
            {
                MyCore.NetCreateObject(0, -1, transform.position, transform.rotation);
                StartCoroutine(WaitForNetworkInit());
            }
            else
            {
                InitializeNavigation();

            }
        }
        else
        {
            StartCoroutine(WaitForServerSync());
        }
    }

    private IEnumerator WaitForNetworkInit()
    {
        yield return new WaitUntil(() => MyId.IsInit);
        InitializeNavigation();
    }

    private IEnumerator WaitForServerSync()
    {
        yield return new WaitUntil(() => MyId.IsInit && transform.position != Vector3.zero);
        MyAgent.Warp(transform.position);
    }

    private void InitializeNavigation()
    {
        GameObject[] navPoints = GameObject.FindGameObjectsWithTag("NavPoint");
        Goals = new List<Vector3>();
        foreach (GameObject g in navPoints)
        {
            Goals.Add(g.transform.position);
        }
        StartCoroutine(BehaviorCycle());
    }

    public override IEnumerator SlowUpdate()
    {
        while (true)
        {
            if (IsServer && MyId.IsInit)
            {
                SendUpdate("POSITION", $"{transform.position.x},{transform.position.y},{transform.position.z}");
                SendUpdate("ROTATION", $"{transform.rotation.eulerAngles.x},{transform.rotation.eulerAngles.y},{transform.rotation.eulerAngles.z}");
                float speed = MyAgent.velocity.magnitude;
                SendUpdate("ANIM_SPEED", speed.ToString("F2"));
            }
            yield return new WaitForSeconds(0.0167f); 
        }
    }

    public override void HandleMessage(string flag, string value)
    {
        if (!IsServer && MyId.IsInit)
        {
            if (flag == "POSITION")
            {
                string[] posSplit = value.Split(',');
                Vector3 newPos = new Vector3(float.Parse(posSplit[0]), float.Parse(posSplit[1]), float.Parse(posSplit[2]));
                MyAgent.Warp(newPos);
            }
            else if (flag == "ROTATION")
            {
                string[] rotSplit = value.Split(',');
                Vector3 newRot = new Vector3(float.Parse(rotSplit[0]), float.Parse(rotSplit[1]), float.Parse(rotSplit[2]));
                transform.rotation = Quaternion.Euler(newRot);
            }
            else if (flag == "ANIM_SPEED")
            {
                float speed;
                if (float.TryParse(value, out speed))
                {
                    MyAnime.SetFloat("speedh", speed);
                }
            }
        }
    }

    void Update()
    {
        if (IsServer && MyAgent != null && MyAnime != null && MyId.IsInit)
        {
            float speed = MyAgent.velocity.magnitude;
            MyAnime.SetFloat("speedh", speed);

            if (MyAgent.velocity.sqrMagnitude > 0.1f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(MyAgent.velocity.normalized);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
            }
        }
    }

    IEnumerator BehaviorCycle()
    {
        while (true)
        {
            if (Random.value < 0.3f)
            {
                MyAgent.isStopped = true;
                if (MyAnime != null && MyId.IsInit)
                {
                    MyAnime.SetFloat("speedh", 0f);
                }
                yield return new WaitForSeconds(10f);
                MyAgent.isStopped = false;
            }
            else if (Goals != null && Goals.Count > 0)
            {
                CurrentGoal = Goals[Random.Range(0, Goals.Count)];
                MyAgent.SetDestination(CurrentGoal);
                while (!MyAgent.pathPending && MyAgent.remainingDistance > MyAgent.stoppingDistance + 0.1f)
                {
                    yield return null;
                }
                if (MyAnime != null && MyId.IsInit)
                {
                    MyAnime.SetFloat("speedh", 0f);
                }
            }
            yield return new WaitForSeconds(1f);
        }
    }
}


