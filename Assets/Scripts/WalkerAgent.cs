using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public class WalkerAgent : Agent
{
    [Header("Body Parts")]
    public Rigidbody2D body;
    public Rigidbody2D head;

    [Header("Joints")]
    public HingeJoint2D[] joints;

    [Header("Settings")]
    public float fallHeadYThreshold = -3.0f;
    public float maxMotorSpeed = 300f;
    public float targetSpeed = 5f;
    public float goalDistance = 10000f;

    [Header("Height")]
    public float startBodyY = 0f;
    public float bodyHeightBonusScale = 0.05f;

    [Header("Fall")]
    public float bodyMinY = -2.5f;
    public float maxBodyTilt = 60f;

    [Header("Laser Pursuer")]
    public Transform pursuerVisual;
    public float pursuerSpeed = 1.0f;
    public float pursuerStartOffset = -3.0f;
    public bool pursuerEnabled = true;

    [Header("Demo Mode (tick for inference)")]
    public bool demoMode = false;

    private Vector2 startBodyPos;
    private Vector2[] partsStartPos;
    private float[] partsStartRot;
    private Rigidbody2D[] allParts;
    private float maxDistanceReached;
    private int stepsWithoutProgress;
    private float pursuerX;

    public override void Initialize()
    {
        startBodyPos = body.position;
        startBodyY = body.position.y;

        // Auto-hide laser in demo mode
        if (demoMode && pursuerVisual != null)
        {
            pursuerVisual.gameObject.SetActive(false);
        }

        var rbs = GetComponentsInChildren<Rigidbody2D>();
        allParts = rbs;
        partsStartPos = new Vector2[rbs.Length];
        partsStartRot = new float[rbs.Length];
        for (int i = 0; i < rbs.Length; i++)
        {
            partsStartPos[i] = rbs[i].position;
            partsStartRot[i] = rbs[i].rotation;
        }

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
        stepsWithoutProgress = 0;

        pursuerX = startBodyPos.x + pursuerStartOffset;
        if (pursuerVisual != null)
        {
            var p = pursuerVisual.position;
            p.x = pursuerX;
            pursuerVisual.position = p;
        }
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
        float bodyTiltDeg = Mathf.Abs(Mathf.DeltaAngle(body.rotation, 0f));

        if (pursuerEnabled && !demoMode)
        {
            pursuerX += pursuerSpeed * Time.fixedDeltaTime;
            if (pursuerVisual != null)
            {
                var p = pursuerVisual.position;
                p.x = pursuerX;
                pursuerVisual.position = p;
            }
            if (body.position.x < pursuerX)
            {
                AddReward(-2f);
                EndEpisode();
                return;
            }
        }

        float bodyHeightRatio = Mathf.Clamp01((body.position.y - bodyMinY) / (startBodyY - bodyMinY));
        AddReward(velX * 0.3f * bodyHeightRatio);
        AddReward(bodyHeightRatio * bodyHeightBonusScale);

        float dist = body.position.x - startBodyPos.x;
        if (dist > maxDistanceReached)
        {
            AddReward((dist - maxDistanceReached) * 1.0f * bodyHeightRatio);
            maxDistanceReached = dist;
            stepsWithoutProgress = 0;
        }
        else
        {
            stepsWithoutProgress++;
        }

        if (!demoMode && Mathf.Abs(velX) < 0.5f)
        {
            AddReward(-0.02f);
        }

        if (body.position.y < bodyMinY)
        {
            AddReward(-1.0f);
            EndEpisode();
            return;
        }

        if (bodyTiltDeg > maxBodyTilt)
        {
            AddReward(-1.0f);
            EndEpisode();
            return;
        }

        if (head.position.y < fallHeadYThreshold)
        {
            AddReward(-1.0f);
            EndEpisode();
            return;
        }

        if (!demoMode && dist > goalDistance)
        {
            AddReward(10f);
            EndEpisode();
            return;
        }

        if (!demoMode && stepsWithoutProgress > 500)
        {
            AddReward(-2f);
            EndEpisode();
            return;
        }

        if (!demoMode && dist < -5f)
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