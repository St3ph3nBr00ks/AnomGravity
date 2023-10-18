using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

// Can this run on the physics update?

namespace AnomDevKit
{
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(Rigidbody))]
    public class Gravity : MonoBehaviour
    {
        public enum GravityEffects {None, ThisEffectsOthers, OthersEffectThis, Both, UseRigidbodyGravity}

        public enum EffectOthersDirection{None, AttractOthers, RepelOthers}
    
        public enum EffectOthersSpace {None, VectorWorld, VectorLocal, DirectionBetweenObjects}
    
        public enum EffectOtherAccelType{None, SetValue, CalculateFromMass}
        
        public enum OrientThis{None, OrientToStrongestInfluence, OrientAwayFromStrongestInfluence, 
            OrientToCalculatedDirection}
        
        [Header("-Inspector Settings-")] 
        public GravityEffects gravityEffects;

        [Header("-Others Affect This-")]
        public OrientThis orientThis;
        
        public float secondsBetweenInfluenceSelf = 1;

        public float secondsBetweenInfluenceSelfTimer;
        
        public float defaultTimeInAir = 1f;

        public bool checkThisGrounded = true;

        public float secondsBetweenGroundCheck = 0.1f;

        public float secondsBetweenGroundCheckTimer = 0;
        
        [SerializeField] protected float groundCheckDistance = 1.2f;
        
        public List<GameObject> groundTestIgnoreObjs;
        
        [Header("-This Affects Others-")]
        public bool effectRigidbodiesWithoutGravComponent;
        public EffectOthersDirection effectOthersDirection;
        public EffectOthersSpace effectOthersSpace;
        public EffectOtherAccelType effectOtherAccelType;
        
        //[Tooltip("Set mass of this object.")]
        //[SerializeField]
        //protected float mass;
        
        [Tooltip("Force of acceleration applied to instances of \"Gravity\" influenced by this.")]
        [SerializeField]
        protected float effectOtherAccelMetersPerSecond;

        public float secondsBetweenInfluenceOthers = 1;

        public float secondsBetweenInfluenceOthersTimer;

        [Header("Events")] 
        public UnityEvent<GameObject> onInfluenceOrRigidbodyAdded;
        public UnityEvent<GameObject> onInfluenceOrRigidbodyRemoved;

        public UnityEvent onGrounded;
        public UnityEvent onNotGrounded;
        
        [Header("-Runtime Values-")]
        [Tooltip("This variable stores a reference to the strongest gravityParent currently effecting this script's " +
                 "RigidBody. If OrientToGravity is set to true, then this script's parent GameObject will orient its " +
                 "rotation toward the strongestInfluece. ")]
        [SerializeField]
        protected Gravity _strongestInfluece;
        
        public Gravity strongestInfluece
        {
            get
            {
                return _strongestInfluece = GetStrongestInfluence();
            }
        }
        
        public bool grounded = false;

        protected RaycastHit[] groundTestHits;
        
        protected int hitsCounter = 0;
        
        // Time the object has been "airborne", not in contact with something that would block movement due to gravity.
        // Often used to calculate acceleration due to gravity on an object or actor that is falling toward the ground.
        public float timeInAir = 0f;
        
        // Gravity instances that affect this Gravity instance.
        public List<Gravity> influences = new List<Gravity>();

        // Rigidbodies without Gravity scripts affected by this object.
        public List<Rigidbody> rigidBodies = new List<Rigidbody>();

        // Pre-allocated value used to temporarily reference a Gravity instance when looking for the strongest influence.
        protected int tempGreatestInfluence = -1;
        
        // Pre-allocated value used to cycle through the "influences" list when looking for the strongest influence.
        protected int influenceCntr = 0;

        // Pre-allocated value used to cycle through the "influences" list.
        protected int influenceCounter = 0;

        // Pre-allocated value that stores the force of gravity applied on this object this frame.
        protected Vector3 calculatedGravityEffectOnThis;

        // Pre-allocated value that stores the force of gravity applied to others this frame.
        protected Vector3 calculatedGravityEffectOnOthers;
        
        // Pre-allocated space where the gravity direction is stored.
        Vector3 direction = Vector3.zero;
        
        Vector3 groundTestDirection = Vector3.zero;
        
        // Protected Values
        protected bool initialized;
        
        // Components
        protected Rigidbody _rb;

        protected Rigidbody rb
        {
            get
            {
                if (_rb == null)
                {
                    _rb = GetComponent<Rigidbody>();
                    
                    if (_rb == null)
                    {
                        _rb = this.gameObject.AddComponent<Rigidbody>();
                    }
                }
                
                return _rb;
            }
        }

        protected Coroutine applyGravityCo;
        
        public void Start()
        {
            Initialize();
        }

        public void Awake()
        {
            Initialize();
        }

        public void Initialize()
        {
            if (!initialized)
            {
                // Disable normal rigidbody gravity
                if (gravityEffects != GravityEffects.UseRigidbodyGravity)
                {
                    rb.useGravity = false;
                }
                else
                {
                    rb.useGravity = true;
                }
                
                applyGravityCo = StartCoroutine(ApplyGravityInfluencesIE());
            }
            
            initialized = true;
        }

        public void AddInfluenceOrRigidbody(GameObject newGo)
        {
            object newObject = null;
                
            if ((newObject = newGo.GetComponent<Gravity>()) != null)
            {
                AddInfluence(newObject as Gravity);
            }
            else if (effectRigidbodiesWithoutGravComponent && (newObject = newGo.GetComponent<Rigidbody>()) != null)
            {
                AddRigidbody(newObject as Rigidbody);
            }
            
            onInfluenceOrRigidbodyAdded.Invoke(newGo);
        }
        
        public void AddInfluence(Gravity newInfluence)
        {
            if (!influences.Contains(newInfluence))
            {
                influences.Add(newInfluence);
            }
            
            // Update the strongest influence.
            _strongestInfluece = GetStrongestInfluence();
        }
        
        public void AddRigidbody(Rigidbody newRigidbody)
        {
            if (newRigidbody != null && !rigidBodies.Contains(newRigidbody))
            {
                Debug.Log("Add " + newRigidbody.name + " to rigidBodies.");
                rigidBodies.Add(newRigidbody);
            }
        }

        public void RemoveInfluenceOrRigidbody(GameObject removeGo)
        {
            object removeObject = null;
                
            if ((removeObject = removeGo.GetComponent<Gravity>()) != null)
            {
                RemoveInfluence(removeObject as Gravity);
            }
            else if ((removeObject = removeGo.GetComponent<Rigidbody>()) != null)
            {
                RemoveRigidbody(removeObject as Rigidbody);
            }
            
            onInfluenceOrRigidbodyRemoved.Invoke(removeGo);
        }
        
        public void RemoveInfluence(Gravity removeInfluence)
        {
            if (influences.Contains(removeInfluence))
            {
                influences.Remove(removeInfluence);
            }
            
            // Update the strongest influence.
            _strongestInfluece = GetStrongestInfluence();
        }

        public void RemoveRigidbody(Rigidbody removeRigidbody)
        {
            if (rigidBodies.Contains(removeRigidbody))
            {
                rigidBodies.Remove(removeRigidbody);
            }
        }

        IEnumerator ApplyGravityInfluencesIE()
        {
            // Reset timers before starting the while loop.
            timeInAir = defaultTimeInAir;
            secondsBetweenInfluenceSelfTimer = 0;
            secondsBetweenInfluenceOthersTimer = 0;
            
            while (true)
            {   
                // Apply Gravity To Self.
                if (gravityEffects is GravityEffects.OthersEffectThis or GravityEffects.Both)
                {
                    if (checkThisGrounded)
                    {
                        secondsBetweenGroundCheckTimer += Time.deltaTime;
                        
                        if(secondsBetweenGroundCheck < secondsBetweenGroundCheckTimer)
                        {
                            // Update time in air
                            if (GetGrounded())
                            {
                                timeInAir = defaultTimeInAir;
                            }
                            else
                            {
                                timeInAir += Time.deltaTime;
                            }

                            secondsBetweenGroundCheckTimer = 0;
                        }
                    }

                    secondsBetweenInfluenceSelfTimer += Time.deltaTime;

                    // If "secondsBetweenGravityTimer" has elapsed, calculate gravity from all influences.
                    if (secondsBetweenInfluenceSelf < secondsBetweenInfluenceSelfTimer)
                    {
                        calculatedGravityEffectOnThis = Vector3.zero;

                        // Add gravitational forces from each influence
                        for (influenceCounter = 0; influenceCounter < influences.Count; influenceCounter++)
                        {
                            switch (influences[influenceCounter].effectOthersSpace)
                            {
                                case EffectOthersSpace.None:
                                    direction *= 0;
                                    break;
                                case EffectOthersSpace.VectorLocal:
                                    direction = influences[influenceCounter].transform.up * -1;
                                    break;
                                case EffectOthersSpace.VectorWorld:
                                    direction = -1 * Vector3.up;
                                    break;
                                case EffectOthersSpace.DirectionBetweenObjects:
                                    direction = (transform.position - influences[influenceCounter].transform.position).normalized;
                                    break;
                            }

                            switch (influences[influenceCounter].effectOthersDirection)
                            {
                                case EffectOthersDirection.None:
                                    direction *= 0;
                                    break;
                                case EffectOthersDirection.AttractOthers:
                                    direction *= -1;
                                    break;
                                case EffectOthersDirection.RepelOthers:
                                    break;
                            }

                            calculatedGravityEffectOnThis += direction * (influences[influenceCounter].effectOtherAccelMetersPerSecond 
                                                            * influences[influenceCounter].effectOtherAccelMetersPerSecond
                                                               * timeInAir * secondsBetweenInfluenceSelf);
                        }

                        calculatedGravityEffectOnThis *= timeInAir;

                        /*
                        Debug.Log(gameObject.name + this.GetType().Namespace + this.GetType().Name +
                                  "ApplyGravity AddForce : " + calculatedGravityEffectOnThis + " | timeInAir : " +
                                  timeInAir);
                        */
                        
                        rb.AddForce(calculatedGravityEffectOnThis);

                        secondsBetweenInfluenceSelfTimer = 0;
                    }
                }

                // Apply gravity to other rigidbodies
                if (gravityEffects is GravityEffects.ThisEffectsOthers or GravityEffects.Both)
                {
                    secondsBetweenInfluenceOthersTimer += Time.deltaTime;

                    // If "secondsBetweenGravityTimer" has elapsed, calculate gravity from all influences.
                    if (secondsBetweenInfluenceOthers < secondsBetweenInfluenceOthersTimer)
                    {
                        calculatedGravityEffectOnOthers = Vector3.zero;

                        // Add gravitational forces from each influence
                        for (influenceCounter = 0; influenceCounter < rigidBodies.Count; influenceCounter++)
                        {
                            switch (effectOthersSpace)
                            {
                                case EffectOthersSpace.None:
                                    direction *= 0;
                                    break;
                                case EffectOthersSpace.VectorLocal:
                                    direction = rigidBodies[influenceCounter].transform.up * -1;
                                    break;
                                case EffectOthersSpace.VectorWorld:
                                    direction = -1 * Vector3.up;
                                    break;
                                case EffectOthersSpace.DirectionBetweenObjects:
                                    direction = (transform.position - rigidBodies[influenceCounter].transform.position)
                                        .normalized;
                                    break;
                            }

                            switch (effectOthersDirection)
                            {
                                case EffectOthersDirection.None:
                                    direction *= 0;
                                    break;
                                case EffectOthersDirection.AttractOthers:
                                    direction *= -1;
                                    break;
                                case EffectOthersDirection.RepelOthers:
                                    break;
                            }

                            calculatedGravityEffectOnOthers += direction * (effectOtherAccelMetersPerSecond 
                                                                * effectOtherAccelMetersPerSecond * secondsBetweenInfluenceOthersTimer);
                            
                            /*
                            Debug.Log(gameObject.name + this.GetType().Namespace + this.GetType().Name +
                                      "ApplyGravity AddForce : " + calculatedGravityEffectOnOthers + " | timeInAir : " +
                                      timeInAir);                            
                            */
                            
                            rigidBodies[influenceCounter].AddForce(calculatedGravityEffectOnOthers);
                        }

                        secondsBetweenInfluenceOthersTimer = 0;
                    }
                }

                yield return null;
            }

            yield break;
        }

        public Gravity GetStrongestInfluence()
        {
            tempGreatestInfluence = -1;

            if (influences != null && influences.Count > 0)
            {
                for (influenceCntr = 0; influenceCntr < influences.Count; influenceCntr++)
                {
                    if (tempGreatestInfluence == -1
                        || influences[tempGreatestInfluence].effectOtherAccelMetersPerSecond <
                        influences[influenceCntr].effectOtherAccelMetersPerSecond)
                    {
                        tempGreatestInfluence = influenceCntr;
                    }
                }

                _strongestInfluece = influences[tempGreatestInfluence];
                return influences[tempGreatestInfluence];
            }

            return null;
        }

        public bool GetGrounded()
        {
            Debug.Log( gameObject.name + " GetGrounded()");
            
            GetStrongestInfluence();

            if (strongestInfluece)
            {
                // Calculate direction to strongest gravity parent
                groundTestDirection = (transform.position - strongestInfluece.transform.position).normalized;
                
                // Make sure the actor is no longer standing on anything before disabling gravity.
                groundTestHits = Physics.SphereCastAll(transform.position + transform.up, 0.2f, groundTestDirection,
                    groundCheckDistance);

                for (hitsCounter = 0; hitsCounter < groundTestHits.Length; hitsCounter++)
                {
                    Debug.Log("Hit : " + gameObject.name + " " + groundTestHits[hitsCounter].transform.gameObject.name);
                    
                    if (!groundTestHits[hitsCounter].collider.isTrigger && groundTestHits[hitsCounter].transform &&
                        groundTestHits[hitsCounter].transform != this.transform &&
                        !groundTestIgnoreObjs.Contains(groundTestHits[hitsCounter].transform.gameObject))
                    {
                        Debug.Log("Valid hit : " + gameObject.name + " " +groundTestHits[hitsCounter].transform.gameObject.name);

                        SetGrounded();

                        Debug.DrawLine(transform.position, groundTestHits[hitsCounter].point, Color.green);
                        
                        return true;
                    }
                }

                SetNotGrounded();
                
                Debug.Log(gameObject.name + " no valid hit found");
                
                Debug.DrawRay(transform.position, groundTestDirection * groundCheckDistance, Color.red);
            }

            return false;
        }
        
        public virtual void SetGrounded()
        {
            grounded = true;
            
            onGrounded.Invoke();
        }

        public virtual void SetNotGrounded()
        {
            grounded = false;
            
            onNotGrounded.Invoke();
        }
    }
}