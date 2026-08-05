using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public class WalkerAgent : Agent
{
    [Header("Body Parts (drag from Hierarchy)")]
    public Rigidbody2D body;
    public Rigidbody2D head;

    [Header("Joints (drag all 8 in Inspector)")]
    public HingeJoint2D[] joints;

    [Header("Reward Settings")]
    public float fallHeadYThreshold = -3.0f;
    public float maxMotorSpeed = 300f;
    public float targetSpeed = 5f;
    public float goalDistance = 30f; // เดินถึงจุดนี้ = ชนะ + reset

    private Vector2 startBodyPos;
    private Vector2[] partsStartPos;
    private float[] partsStartRot;
    private Rigidbody2D[] allParts;
    private float maxDistanceReached;

    public override void Initialize()
    {
        startBodyPos = body.position;

        var rbs = GetComponentsInChildren<Rigidbody2D>();
        allParts = rbs;
        partsStartPos = new Vector2[rbs.Length];
        partsStartRot = new float[rbs.Length];
        for (int i = 0; i < rbs.Length; i++)
        {
            partsStartPos[i] = rbs[i].position;
            partsStartRot[i] = rbs[i].rotation;
        }

        // ไม่ให้ชิ้นส่วนตัวเองชนกัน
        var cols = GetComponentsInChildren<Collider2D>();
        for (int i = 0; i < cols.Length; i++)
        {
            for (int j = i + 1; j < cols.Length; j++)
            {
                Physics2D.IgnoreCollision(cols[i], cols[j], true);
            }
        }
    }

    public override void OnEpisodeBegin()
    {
        for (int i = 0; i < allParts.Length; i++)
        {
            allParts[i].position = partsStartPos[i];
            allParts[i].rotation = partsStartRot[i];
            allParts[i].linearVelocity = Vector2.zero;
            allParts[i].angularVelocity = 0f;
        }
        maxDistanceReached = 0f;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        Vector2 relPos = body.position - startBodyPos;
        sensor.AddObservation(relPos);
        sensor.AddObservation(body.linearVelocity);
        sensor.AddObservation(body.rotation / 180f);
        sensor.AddObservation(body.angularVelocity / 360f);
        sensor.AddObservation(head.position.y - body.position.y);

        foreach (var j in joints)
        {
            sensor.AddObservation(j.jointAngle / 180f);
            sensor.AddObservation(j.jointSpeed / 360f);
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        for (int i = 0; i < joints.Length; i++)
        {
            float action = Mathf.Clamp(actions.ContinuousActions[i], -1f, 1f);
            JointMotor2D motor = joints[i].motor;
            motor.motorSpeed = action * maxMotorSpeed;
            joints[i].motor = motor;
        }

        float velX = body.linearVelocity.x;

        AddReward(velX * 0.05f);

        if (velX > targetSpeed * 0.5f)
        {
            float speedBonus = (velX / targetSpeed) * (velX / targetSpeed) * 0.02f;
            AddReward(speedBonus);
        }

        float dist = body.position.x - startBodyPos.x;
        if (dist > maxDistanceReached)
        {
            AddReward((dist - maxDistanceReached) * 0.1f);
            maxDistanceReached = dist;
        }

        if (velX < 0.1f)
        {
            AddReward(-0.005f);
        }

        AddReward(0.001f);

        // ล้ม → penalty + reset
        if (head.position.y < fallHeadYThreshold)
        {
            AddReward(-1.0f);
            EndEpisode();
            return;
        }

        // ถึงเป้า → bonus + reset
        if (dist > goalDistance)
        {
            AddReward(10f); // รางวัลใหญ่!
            EndEpisode();
            return;
        }

        // ถอยหลังไกล → reset (กันการเรียนถอย)
        if (dist < -5f)
        {
            AddReward(-1.0f);
            EndEpisode();
            return;
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var acts = actionsOut.ContinuousActions;
        float h = Input.GetAxis("Horizontal");
        for (int i = 0; i < acts.Length; i++)
        {
            acts[i] = h;
        }
    }

    public float DistanceTraveled => body.position.x - startBodyPos.x;
    public float CurrentSpeed => body.linearVelocity.x;
}