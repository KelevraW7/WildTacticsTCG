namespace Wobblewares.Coin
{
    using System;
    using System.Collections;
    using UnityEngine;

    /// <summary>
    /// The core implementation of the Coin.
    /// Add this script to your Coin gameobject and configure the parameters in the inspector.
    /// Use the Flip functions to flip the coin.
    /// </summary>
    public class Coin : MonoBehaviour
    {
        #region Inspector Variables

        [Header("Flip Behaviour")]
        [Tooltip("The chances of the coin landing on heads when flipped.")]
        [Range(0.0f, 1.0f)]
        public float HeadsWeight = 0.5f;

        [Tooltip("The maximum height reached by the flip in units")]
        public float FlipHeight = 1.5f;

        [Tooltip("The duration of the flip in seconds")]
        public float FlipDuration = 0.75f;

        [Tooltip("The amount of full 360 rotations per flip")]
        public int RotationCount = 2;

        [Tooltip("The smoothing curve used for ascent and descent trajectory")]
        public AnimationCurve VerticalOffsetCurve = AnimationCurve.Linear(0.0f, 0.0f, 1.0f, 1.0f);
        
        [Header("Audio")]
        [Tooltip("Audio source used when the coin is flicked")]
        public AudioSource CoinAudioSource = default;
        
        [Tooltip("AudioClip played when the flip begins")]
        public AudioClip FlipAudioClip = default;

        [Tooltip("AudioClip used when the coin lands or collides with an object")]
        public AudioClip CollisionAudioClip = default;
        #endregion

        #region Cached Components

        /// <summary>
        /// The rigidbody on the coin object
        /// </summary>
        private new Rigidbody rigidbody = null;

        #endregion
        
        #region Public API

        /// <summary>
        /// The possible states of the coin
        /// </summary>
        public enum CoinState
        {
            Idle,   // not moving and is not currently flipping
            Flipped, // flipping through the air
            Settling // flip is done but we're waiting for the coin to settle and stop moving
        }
        
        /// <summary>
        /// The possible sides of the coin
        /// </summary>
        public enum CoinSide
        {
            None,  // when neither side is properly facing upwards
            Heads, // when heads is facing upwards
            Tails, // when tails is facing upwards
        }
        
        /// <summary>
        /// The current state of the coin (Idle, Flipped or Settling)
        /// </summary>
        public CoinState State { get; private set; }
        
        /// <summary>
        /// Returns the current side pointing upwards.
        /// </summary>
        public CoinSide Side => GetVisibleSide();

        /// <summary>
        /// Action invoked when the flip starts (i.e Flip is called)
        /// </summary>
        public Action OnFlipStart;
        
        /// <summary>
        /// Action invoked when the flip ends, but before the coin has settled.
        /// This action will not invoke if the flip is interrupted by a collision or a call to
        /// Stop
        /// </summary>
        public Action OnFlipEnd;
        
        /// <summary>
        /// Action invoked with the resulting side once the coin has settled after a flip.
        /// </summary>
        public Action<CoinSide> OnSettled;

        /// <summary>
        /// Action invoked when the coin has reached the apex of its flip and has begun descending.
        /// </summary>
        public Action OnApexReached;
        
        /// <summary>
        /// Action invoked with the resulting side when the coin stops a flip due to a collision
        /// </summary>
        public Action OnCollide;

        /// <summary>
        /// Calculate a flip result based on the weighting of heads and tails
        /// </summary>
        public CoinSide CalculateFlipResult()
        {
            // generate random value between 0 and 1
            float random = UnityEngine.Random.value * 1.0f; 
            
            // determine the flip result based on the weighting of heads and tails
            return random <= HeadsWeight ? CoinSide.Heads : CoinSide.Tails;
        }
        
        /// <summary>
        /// Flip the coin towards target position. This will randomly land on heads or tails
        /// based on the set HeadsWeight unless a targetSide is specified.
        /// </summary>
        public void Flip( Vector3 targetPosition, CoinSide targetSide = CoinSide.None )
        {
            // stop the active flip if the coin is currently flipping
            if (State != CoinState.Idle)
                Stop();

            if ( targetSide == CoinSide.None )
                targetSide = CalculateFlipResult();

            CoinAudioSource.PlayOneShot(FlipAudioClip);
            
            StartCoroutine(FlipAsync( targetPosition, targetSide) );

        }

        /// <summary>
        /// Flip the coin in the specified direction and for the specified distance.
        /// This will randomly land on heads or tails
        /// based on the set HeadsWeight unless a targetSide is specified.
        /// </summary>
        public void Flip( Vector3 direction, float distance, CoinSide targetSide = CoinSide.None )
        {
            Vector3 targetPosition = transform.position + direction * distance;
            Flip(targetPosition, targetSide);
        }

        /// <summary>
        /// Stop any in progress flips - cancelling all events and re-enabling
        /// physics if the rigidbody exists.
        /// </summary>
        public void Stop()
        {
            // stop the active flip
            StopAllCoroutines();

            // reenable gravity
            if(rigidbody)
                rigidbody.useGravity = true;

            State = CoinState.Idle;
        }

        #endregion

        #region Private

        /// <summary>
        /// Get the side that is facing away from the surface.
        /// Returns none if it is 'unclear' which side is facing upwards.
        /// </summary>
        private CoinSide GetVisibleSide()
        {
            if (Vector3.Dot(transform.up, Vector3.up) > 0.25f)
                return CoinSide.Heads;

            if (Vector3.Dot(transform.up, Vector3.up) < -0.25f)
                return CoinSide.Tails;

            return CoinSide.None;
        }


        private void OnCollisionEnter( Collision collision )
        {
            // if a collision occurs during a flip, we wait until the coin stops moving
            if (State == CoinState.Flipped)
            {
                if(!CoinAudioSource.isPlaying)
                    CoinAudioSource.PlayOneShot( CollisionAudioClip );
                rigidbody.useGravity = true;
                OnCollide?.Invoke();
                StopAllCoroutines();
                StartCoroutine(SettleAsync());
            }
            else if(collision.relativeVelocity.magnitude > 2.0f)
            {        
                if(!CoinAudioSource.isPlaying)
                    CoinAudioSource.PlayOneShot( CollisionAudioClip );
            }
        }

        private IEnumerator FlipAsync(Vector3 targetPosition, CoinSide targetSide)
        {
            State = CoinState.Flipped;
            OnFlipStart?.Invoke();
            
            // Fetch rigidbody if it hasn't been set yet
            if (rigidbody == null)
                rigidbody = GetComponent<Rigidbody>();
            
            // turn off gravity so it doesn't affect our flip trajectory
            rigidbody.useGravity = false;

            // prepare vertical adjustment
            float heightDifferenceBetweenStartAndTarget = Mathf.Clamp(targetPosition.y - rigidbody.position.y, float.MinValue, FlipHeight);
            float ascentDistance = FlipHeight; // always ascend to FlipHeight from starting position
            float descentDistance = FlipHeight - heightDifferenceBetweenStartAndTarget; // calculate descent from apex (FlipHeight) minus any difference between start and target
            float ascentRatio = ascentDistance / (ascentDistance + descentDistance); // ratio of ascent to descent
            float ascentDuration = FlipDuration * ascentRatio;
            float descentDuration = FlipDuration * (1.0f - ascentRatio);

            // prepare horizontal adjustment
            Vector3 vecTowardsTarget = targetPosition - rigidbody.position;
            
            // calculate the axis of rotation to rotate naturally 'towards' target position
            // use the coin's right axis if the coin is flipping on the spot
            Vector3 vecBetween = new Vector3((rigidbody.position - targetPosition).x, 0.0f, (rigidbody.position - targetPosition).z).normalized;
            Vector3 rotationAxis = vecBetween.magnitude > 0.0f ? Vector3.Cross(vecBetween, Vector3.up) : transform.right; 

            // Before we start flipping, we need to correct the rotation so that the coin starts flat.
            // This is a bit of a 'cheat' to not have to correct the rotation as the coin flys through the
            // air and in most instances it's not obvious this is happening.
            Vector3 upOrDown = transform.up.y > 0.0f ? Vector3.up : Vector3.down;
            Quaternion correctiveRotation = Quaternion.FromToRotation(transform.up, upOrDown);
            rigidbody.rotation = correctiveRotation * transform.rotation;

            // calculate the required rotations to get from current side to target side
            CoinSide currentSide = GetVisibleSide();
            float requiredRotations = currentSide == targetSide ? RotationCount : RotationCount + 0.5f;

            yield return StartCoroutine(AscendAsync(vecTowardsTarget * ascentRatio, ascentDistance, ascentDuration, rotationAxis, requiredRotations * ascentRatio) );

            OnApexReached?.Invoke();

            yield return StartCoroutine(DescendAsync(vecTowardsTarget * (1.0f - ascentRatio), descentDistance, descentDuration, rotationAxis, requiredRotations * (1.0f - ascentRatio)));
            
            // Reenable physics
            rigidbody.useGravity = true;
            
            if (descentDuration > 0)
            {
                // Maintain descent speed to avoid the coin stopping suddenly if it hasn't hit the ground.
                Vector3 descentMovementVector = -vecTowardsTarget * (1.0f - ascentRatio);
                Vector3 movementVector = new Vector3(-descentMovementVector.x, -descentDistance, -descentMovementVector.z);
                float speed = movementVector.magnitude / descentDuration;
                rigidbody.linearVelocity = movementVector.normalized * speed;
            
                // Maintain rotation speed if an obstacle won't be hit in the next 0.5 seconds.
                // This ensures that the coin does not flip to the incorrect side due to a high angular velocity.
                float distance = Mathf.Abs(rigidbody.linearVelocity.y);
                if (!Physics.Raycast(rigidbody.position, Vector3.down, distance))
                {
                    float rotationSpeed = (requiredRotations * (Mathf.Deg2Rad * 360.0f)) / FlipDuration;
                    rigidbody.angularVelocity = rotationAxis * rotationSpeed;
                }
            }
            
            OnFlipEnd?.Invoke();
        }

        private IEnumerator AscendAsync( Vector3 horizontalMovement, float ascentDistance, float ascentDuration, Vector3 rotationAxis, float requiredRotations )
        {
            float timeElapsed = 0.0f;
            float startHeight = rigidbody.position.y;
            
            while (timeElapsed < ascentDuration)
            {
                // ensure the deltaTime used to rotate never exceeds ascentDuration.
                float dtThisFrame = (timeElapsed + Time.fixedDeltaTime > ascentDuration)
                    ? ascentDuration - timeElapsed
                    : Time.fixedDeltaTime;
                
                timeElapsed += dtThisFrame;

                // move the coin towards target position bit by bit, but use the height curve to calculate the y position
                Vector3 vecTowardsTarget = horizontalMovement * (dtThisFrame / ascentDuration);

                float heightOffsetThisFrame = VerticalOffsetCurve.Evaluate(timeElapsed / ascentDuration) * ascentDistance;
                
                rigidbody.MovePosition( new Vector3(
                    rigidbody.position.x + vecTowardsTarget.x, 
                    startHeight + heightOffsetThisFrame,
                    rigidbody.position.z + vecTowardsTarget.z) );
                
                // rotate along rotation axis by a factor based on how many rotations are required over the ascent
                rigidbody.MoveRotation(
                    Quaternion.Euler(rotationAxis * (360.0f * dtThisFrame * requiredRotations) / ascentDuration) *
                    rigidbody.rotation);
                
                yield return new WaitForFixedUpdate();
            }
        }
        
        private IEnumerator DescendAsync( Vector3 horizontalMovement, float descentDistance, float descentDuration, Vector3 rotationAxis, float requiredRotations )
        {
            float timeElapsed = 0.0f;

            float startHeight = rigidbody.position.y;
            
            while (timeElapsed < descentDuration)
            {
                // ensure the deltaTime used to rotate never exceeds descentDuration.
                float dtThisFrame = (timeElapsed + Time.fixedDeltaTime > descentDuration)
                    ? descentDuration - timeElapsed
                    : Time.fixedDeltaTime;
                
                timeElapsed += dtThisFrame;

                // move the coin towards target position bit by bit, but use the height curve to calculate the y position
                Vector3 vecTowardsTarget = horizontalMovement * (dtThisFrame / descentDuration);
                
                float heightOffsetThisFrame = (1.0f - VerticalOffsetCurve.Evaluate(1.0f - (timeElapsed / descentDuration) )) * descentDistance;
                
                rigidbody.MovePosition( new Vector3(
                    rigidbody.position.x + vecTowardsTarget.x, 
                    startHeight -  heightOffsetThisFrame,
                    rigidbody.position.z + vecTowardsTarget.z) );

                // rotate along rotation axis by a factor based on how many rotations are required over the descent
                rigidbody.MoveRotation(
                    Quaternion.Euler(rotationAxis * (360.0f * dtThisFrame * requiredRotations) / descentDuration) *
                    rigidbody.rotation);

                yield return new WaitForFixedUpdate();
            }
        }
        
        private IEnumerator SettleAsync()
        {
            State = CoinState.Settling;
            
            // wait while its still moving
            float settledTime = 0.0f;

            while (settledTime < 0.25f)
            {
                if (rigidbody.linearVelocity.magnitude < 1.0f && rigidbody.angularVelocity.magnitude < 0.5f)
                    settledTime += Time.fixedDeltaTime;

                yield return new WaitForFixedUpdate();
            }

            State = CoinState.Idle;

            var side = GetVisibleSide();
            if (side == CoinSide.None)
                yield break;

            OnSettled?.Invoke( side );

        }

        #endregion

    }
}