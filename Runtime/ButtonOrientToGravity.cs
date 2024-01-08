using UnityEngine;

namespace AnomDevKit
{
    public class ButtonOrientToGravity : MonoBehaviour
    {
        [TextArea] [SerializeField] private const string description =
            "This button is intended for use in the editor to orient objects to a gravity source." +
            "When a button referenceing method \"OrientToGravity\" is clicked, the object this is attached to will orient to the " +
            "gravity direction. Useful when setting up environments and objects with a gravity system besides the Unity default.";

        [Tooltip("This orbject will rotate to face \"orientationTarget.transform.position.\"")] [SerializeField]
        private Transform orientationTarget;

        [Tooltip("If true, this object's up direction will point toward \"orientationTarget.\"")] [SerializeField]
        private bool upTowardTarget;

        // When a button referenceing this method is clicked, the object this is attached to will orient to the gravity direction.
        // Useful when setting up environments and objects with a gravity system besides the Unity default.
        public void OrientToGravity()
        {
            if (orientationTarget == null)
            {
                return;
            }

            Debug.Log("Attempt orient to target.");

            // Establish a variable for "local up."
            Vector3 localUp = new Vector3();

            /*
            if (orientationTarget.GetComponent<ADK_GravityParent>())
            {
                Debug.Log("Orient to gravity.");

                var gravParent = orientationTarget.GetComponent<ADK_GravityParent>();
                
                
                // Check the type of gravity direction that should be applied.  
                switch (gravParent.affectOthersType)
                {
                    case GravityType.Attract:
                        localUp = transform.up;
                        Debug.Log("Attract localUp : " + localUp);
                        break;
                    case GravityType.Repel:
                        localUp = transform.up * -1;
                        Debug.Log("Repel localUp : " + localUp);
                        break;
                    case GravityType.Direction:
                        localUp = Vector3.zero;
                        Debug.Log("Direction localUp : " + localUp);
                        break;
                    case GravityType.RigidBody:
                        localUp = Vector3.zero;
                        Debug.Log("RigidBody localUp : " + localUp);
                        break;
                    case GravityType.XDown:
                        localUp = -1 * Vector3.right;
                        Debug.Log("XDown localUp : " + localUp);
                        break;
                    case GravityType.YDown:
                        localUp = -1 * Vector3.up;
                        Debug.Log("YDown localUp : " + localUp);
                        break;
                    case GravityType.ZDown:
                        localUp = -1 * Vector3.forward;
                        Debug.Log("ZDown localUp : " + localUp);
                        break;
                }
                                
                // Get the direction between this object's position and the gravity parent.
                Vector3 gravityUp = (transform.position - gravParent.transform.position).normalized;

                Debug.Log("gravityUp : " + gravityUp);

                // Calculate the target rotation that the child should be at in order to face the gravity parent.
                Quaternion targetRot = Quaternion.FromToRotation(localUp, gravityUp) * transform.rotation;

                Debug.Log("targetRot : " + targetRot);

                // Rotate the object.
                transform.rotation = targetRot;
            }
            */
        }
    }
}
