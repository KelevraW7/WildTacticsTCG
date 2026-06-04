namespace Wobblewares.Coin.Samples
{
    using UnityEngine;
    using UnityEngine.EventSystems;
    
    /// <summary>
    /// A sample component that can be placed on any GameObject in the scene that
    /// allows you to flip the coin to a selected location in the environment.
    /// </summary>
    public class ClickToToss : MonoBehaviour
    {
        #region Inspector Variables
        [Tooltip("The coin object in the scene")]
        public Coin coin = null;
        [Tooltip("The forced side landed on when flipping using a left click")]
        public Coin.CoinSide leftClickForcedSide = Coin.CoinSide.None;
        [Tooltip("The forced side landed on when flipping using a right click")]
        public Coin.CoinSide rightClickForcedSide = Coin.CoinSide.None;
        [Tooltip("The target reticle showing where the coin will land")]
        public Transform targetReticle = default;
        #endregion

        #region Unity Functions
        
        private void Update()
        {
            // hide the target reticle when over ui
            targetReticle.gameObject.SetActive(!EventSystem.current.IsPointerOverGameObject());
            if (EventSystem.current.IsPointerOverGameObject())
                return;

            // update the position of the target reticle and handle click to toss functionality
            Ray ray = Camera.main.ScreenPointToRay(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 1.0f));
            if (Physics.Raycast(ray, out RaycastHit hit, 50.0f))
            {
                targetReticle.transform.position = hit.point + hit.normal * 0.005f;
                targetReticle.transform.forward = hit.normal;
     
                // don't allow flipping if a coin flip is in progress
                if (coin.State == Coin.CoinState.Flipped)
                    return;
                
                if (Input.GetMouseButtonDown(0))
                    coin.Flip(hit.point + hit.normal * 0.1f, leftClickForcedSide);
                else if(Input.GetMouseButtonDown(1))
                    coin.Flip(hit.point + hit.normal * 0.1f, rightClickForcedSide);
            }
        }

        #endregion

    }
}