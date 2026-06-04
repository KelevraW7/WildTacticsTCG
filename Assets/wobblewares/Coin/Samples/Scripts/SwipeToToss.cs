namespace Wobblewares.Coin.Samples
{
    using UnityEngine;

    /// <summary>
    /// A sample component that can be placed on the camera to convert
    /// mouse or touch swipes into coin flips.
    /// </summary>
    public class SwipeToToss : MonoBehaviour
    {
        #region Inspector Variables
        [Tooltip("The coin object in the scene")]
        public Coin coin = null;
        [Tooltip("The forced side landed on when flipping using a swipe")]
        public Coin.CoinSide forcedSide = Coin.CoinSide.None;
        [Tooltip("Distance factor applied to the swipe amount")]
        public float flipPower = 10.0f;
        #endregion

        #region Unity Functions

        private void Awake()
        {
            camera = GetComponent<Camera>();
        }

        private void Update()
        {
            // don't allow flipping if a coin flip is in progress
            if (coin.State == Coin.CoinState.Flipped)
                return;
            
            HandleMouseInput();
            
            HandleTouchInput();
        }
     
        #endregion
        
        #region Private
         
        private const float swipeThreshold = 20f;
        private Vector2 startSwipePosition;
        private Vector2 endSwipePosition;
        private new Camera camera;
        
        /// <summary>
        /// Handle mouse input on PC
        /// </summary>
        private void HandleMouseInput()
        {
            if (Input.GetMouseButtonDown(0)) 
            {
                endSwipePosition = Input.mousePosition;
                startSwipePosition = Input.mousePosition;
            }

            if (Input.GetMouseButtonUp(0) ) 
            {
                startSwipePosition = Input.mousePosition;
                UpdateSwipeDetection();
            }
        }

        /// <summary>
        /// Handle touch input for mobile devices
        /// </summary>
        private void HandleTouchInput()
        {
            foreach (Touch touch in Input.touches) 
            {
                if (touch.phase == TouchPhase.Began) 
                {
                    endSwipePosition = touch.position;
                    startSwipePosition = touch.position;
                }

                if (touch.phase == TouchPhase.Ended) 
                {
                    startSwipePosition = touch.position;
                    UpdateSwipeDetection();
                }
            }
        }
        
        private void UpdateSwipeDetection()
        {
            Vector2 swipeMovement = startSwipePosition - endSwipePosition;
            
            if ( swipeMovement.magnitude > swipeThreshold )
            {
                Vector3 swipeDirection = camera.transform.TransformDirection(new Vector3(swipeMovement.x / Screen.width, 0.0f, swipeMovement.y / Screen.height ));
                swipeDirection.y = 0.0f;
                coin.Flip(swipeDirection.normalized, swipeDirection.magnitude * flipPower, forcedSide);
            }
        }

        #endregion
    }
}