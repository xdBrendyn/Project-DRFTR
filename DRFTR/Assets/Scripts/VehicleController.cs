using System.Linq;
using UnityEngine;

public class VehicleController : MonoBehaviour
{
    public enum groundCheck { rayCast, sphereCaste };
    public enum MovementMode { Velocity, AngularVelocity };
    public MovementMode movementMode;
    public groundCheck GroundCheck;
    public LayerMask drivableSurface;

    public float MaxSpeed, accelaration, BrakeForce = 0.2f, ReverseMultiplier = 0.35f, turn, gravity = 7f, downforce = 5f, wheelRotationMultiplier = 1.8f, flipTorque = 1f;
    public bool AirControl = false;
    public Rigidbody rb, carBody;

    [HideInInspector]
    public RaycastHit hit;
    public AnimationCurve frictionCurve;
    public AnimationCurve turnCurve;
    public PhysicMaterial frictionMaterial;
    [Header("Visuals")]
    public Transform BodyMesh;
    public Transform[] FrontWheels = new Transform[2];
    public Transform[] RearWheels = new Transform[2];
    [HideInInspector]
    public Vector3 carVelocity;

    [Range(0, 10)]
    public float BodyTilt;
    [Header("Audio settings")]
    public AudioSource engineSound;
    [Range(0, 1)]
    public float minPitch;
    [Range(1, 3)]
    public float MaxPitch;
    public AudioSource SkidSound;

    [HideInInspector]
    public float skidWidth;


    private float radius, horizontalInput, verticalInput, normalTurn, FWheelRotationMultiplier, rollingResistance;
    private Vector3 origin;

    private void Start()
    {
        radius = rb.GetComponent<SphereCollider>().radius;
        if (movementMode == MovementMode.AngularVelocity)
        {
            Physics.defaultMaxAngularSpeed = 100;
        }

        normalTurn = turn;
        FWheelRotationMultiplier = wheelRotationMultiplier;
        rollingResistance = MaxSpeed / 3f;
    }

    private void Update()
    {
        horizontalInput = Input.GetAxis("Horizontal"); //turning input
        verticalInput = Input.GetAxis("Vertical");     //accelaration input

        if (Input.GetAxis("Horizontal") != 0)
        {
            // Calculate the target rotation speed multiplier
            float targetMultiplier = 0.5f * wheelRotationMultiplier;

            // Smoothly interpolate between the current and target rotation speed multiplier
            FWheelRotationMultiplier = Mathf.Lerp(FWheelRotationMultiplier, targetMultiplier, 0.5f * Time.deltaTime);
        }
        else
        {
            FWheelRotationMultiplier = wheelRotationMultiplier;
        }

        Visuals();
        AudioManager();

    }

    public void AudioManager()
    {
        engineSound.pitch = Mathf.Lerp(minPitch, MaxPitch, Mathf.Abs(carVelocity.z) / MaxSpeed);
        if (Mathf.Abs(carVelocity.x) > 10 && grounded())
        {
            SkidSound.mute = false;
        }
        else
        {
            SkidSound.mute = true;
        }
    }

    void FixedUpdate()
    {
        carVelocity = carBody.transform.InverseTransformDirection(carBody.velocity);

        if (Mathf.Abs(carVelocity.x) > 0)
        {
            //changes friction according to sideways speed of car
            frictionMaterial.dynamicFriction = frictionCurve.Evaluate(Mathf.Abs(carVelocity.x / 100));
        }

        if (grounded())
        {
            //turnlogic
            float sign = Mathf.Sign(carVelocity.z);
            float TurnMultiplyer = turnCurve.Evaluate(carVelocity.magnitude / MaxSpeed);
            if (verticalInput > 0.1f || carVelocity.z > 1)
            {
                carBody.AddTorque(Vector3.up * horizontalInput * sign * turn * 100 * TurnMultiplyer);
            }
            else if (verticalInput < -0.1f || carVelocity.z < -1)
            {
                carBody.AddTorque(Vector3.up * horizontalInput * sign * turn * 100 * TurnMultiplyer);
            }

            // Brake Logic
            if (Input.GetAxis("Jump") > 0.1f)
            {
                // Double the turn value
                turn = Mathf.Lerp(turn, normalTurn * 2f, Time.deltaTime / 1f);

                // Calculate the current forward velocity of the car
                float forwardVelocity = Vector3.Dot(rb.velocity, carBody.transform.forward);

                // Define the maximum brake force to prevent the car from coming to a complete stop
                float maxBrakeForce = MaxSpeed * BrakeForce; // Adjust this value as needed

                // Calculate the brake force based on the forward velocity
                float brakeForce = Mathf.Clamp(forwardVelocity * BrakeForce, -maxBrakeForce, maxBrakeForce);

                // Apply the brake force in the forward direction
                rb.AddForce(-carBody.transform.forward * brakeForce, ForceMode.Acceleration);
            }
            else
            {
                turn = Mathf.Lerp(turn, normalTurn, Time.deltaTime / 1f);
                rb.constraints = RigidbodyConstraints.None;
            }

            // Acceleration logic
            if (movementMode == MovementMode.AngularVelocity)
            {
                if (Mathf.Abs(verticalInput) > 0.1f)
                {
                    // Check if the input is negative (indicating reverse)
                    float speedMultiplier = verticalInput > 0 ? 1.0f : ReverseMultiplier;

                    // Apply the appropriate speed multiplier based on direction
                    rb.angularVelocity = Vector3.Lerp(rb.angularVelocity, carBody.transform.right * verticalInput * MaxSpeed / radius * speedMultiplier, accelaration * Time.deltaTime);
                }
                else
                {
                    rb.AddForce(-rb.velocity.normalized * rollingResistance, ForceMode.Acceleration);
                }
            }
            else if (movementMode == MovementMode.Velocity)
            {
                if (Mathf.Abs(verticalInput) > 0.1f)
                {
                    // Check if the input is negative (indicating reverse)
                    float speedMultiplier = verticalInput > 0 ? 1.0f : ReverseMultiplier;

                    // Apply the appropriate speed multiplier based on direction
                    rb.velocity = Vector3.Lerp(rb.velocity, carBody.transform.forward * verticalInput * MaxSpeed * speedMultiplier, accelaration / 10 * Time.deltaTime);
                }
                else
                {
                    rb.AddForce(-rb.velocity.normalized * rollingResistance, ForceMode.Acceleration);
                }
            }

            // down force
            rb.AddForce(-transform.up * downforce * rb.mass);

            // body tilt
            carBody.MoveRotation(Quaternion.Slerp(carBody.rotation, Quaternion.FromToRotation(carBody.transform.up, hit.normal) * carBody.transform.rotation, 0.12f));
        }
        else
        {
            if (AirControl)
            {
                // Perform front or backflips using W and S inputs
                if (verticalInput > 0.1f)
                {
                    // Frontflip: rotate the car forward around the X-axis
                    carBody.AddTorque(Vector3.right * flipTorque * 100);
                }
                else if (verticalInput < -0.1f)
                {
                    // Backflip: rotate the car backward around the X-axis
                    carBody.AddTorque(Vector3.left * flipTorque * 100);
                }

                // Turn logic
                float TurnMultiplyer = turnCurve.Evaluate(carVelocity.magnitude / MaxSpeed);
                carBody.AddTorque(Vector3.up * horizontalInput * turn * 100 * TurnMultiplyer);
            }

            // Rotate the car body to align with the world's up direction (gravity)
            carBody.MoveRotation(Quaternion.Slerp(carBody.rotation, Quaternion.FromToRotation(carBody.transform.up, Vector3.up) * carBody.transform.rotation, 0.02f));

            // Apply gravity
            rb.velocity = Vector3.Lerp(rb.velocity, rb.velocity + Vector3.down * gravity, Time.deltaTime * gravity);
        }
    }

    public void Visuals()
    {
        //tires
        foreach (Transform FW in FrontWheels)
        {
            FW.localRotation = Quaternion.Slerp(FW.localRotation, Quaternion.Euler(FW.localRotation.eulerAngles.x,
                               30 * horizontalInput, FW.localRotation.eulerAngles.z), 0.1f);

            // Get the angular velocity of rb
            Vector3 angularVelocity = rb.angularVelocity;

            // Calculate the magnitude of angular velocity to get the speed
            float rotationSpeed = angularVelocity.magnitude;

            // Determine the sign of the angular velocity (positive or negative rotation)
            float rotationDirection = Mathf.Sign(Vector3.Dot(angularVelocity, rb.transform.right));

            // Calculate the rotation amount for the wheel
            float wheelRotationAmount = rotationSpeed * FWheelRotationMultiplier * rotationDirection;

            // Apply the rotation to the wheel
            FW.GetChild(0).Rotate(Vector3.right, wheelRotationAmount);
        }

        foreach (Transform RW in RearWheels)
        {
            // Get the angular velocity of rb
            Vector3 angularVelocity = rb.angularVelocity;

            // Calculate the magnitude of angular velocity to get the speed
            float rotationSpeed = angularVelocity.magnitude;

            // Determine the sign of the angular velocity (positive or negative rotation)
            float rotationDirection = Mathf.Sign(Vector3.Dot(angularVelocity, rb.transform.right));

            // Calculate the rotation amount for the wheel
            float wheelRotationAmount = rotationSpeed * wheelRotationMultiplier * rotationDirection;

            // Apply the rotation to the wheel
            RW.GetChild(0).Rotate(Vector3.right, wheelRotationAmount);
        }


        //Body
        if (carVelocity.z > 1)
        {
            BodyMesh.localRotation = Quaternion.Slerp(BodyMesh.localRotation, Quaternion.Euler(Mathf.Lerp(0, -5, carVelocity.z / MaxSpeed),
                               BodyMesh.localRotation.eulerAngles.y, BodyTilt * horizontalInput), 0.05f);
        }
        else
        {
            BodyMesh.localRotation = Quaternion.Slerp(BodyMesh.localRotation, Quaternion.Euler(0, 0, 0), 0.05f);
        }
    }

    public bool grounded() //checks for if vehicle is grounded or not
    {
        origin = rb.position + rb.GetComponent<SphereCollider>().radius * Vector3.up;
        var direction = -transform.up;
        var maxdistance = rb.GetComponent<SphereCollider>().radius + 0.2f;

        if (GroundCheck == groundCheck.rayCast)
        {
            if (Physics.Raycast(rb.position, Vector3.down, out hit, maxdistance, drivableSurface))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        else if (GroundCheck == groundCheck.sphereCaste)
        {
            if (Physics.SphereCast(origin, radius + 0.1f, direction, out hit, maxdistance, drivableSurface))
            {
                return true;

            }
            else
            {
                return false;
            }
        }

        return false;
    }
}