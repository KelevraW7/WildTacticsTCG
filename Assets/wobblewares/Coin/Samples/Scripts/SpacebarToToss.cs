namespace Wobblewares.Coin.Samples
{
    using UnityEngine;
    using UnityEngine.EventSystems;
    
    public class SpacebarToToss : MonoBehaviour
    {
        #region Inspector Variables
        [Tooltip("The coin object in the scene")]
        public Coin coin = null;
        #endregion

        #region Unity Functions
        
        private void Update()
        {
            if (EventSystem.current.IsPointerOverGameObject())
                return;
            
            // don't allow flipping if a coin flip is in progress
            if (coin.State == Coin.CoinState.Flipped)
                return;

            if (Input.GetKeyDown(KeyCode.Space))
                coin.Flip(coin.transform.position);
        }

        #endregion

    }
}