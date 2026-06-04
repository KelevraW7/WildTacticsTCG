namespace Wobblewares.Coin.Sample
{
    using UnityEngine;
    using UnityEngine.UI;
    using UnityEngine.EventSystems;
    
    /// <summary>
    /// Rough implementation code for the in-scene booklet used in the sample scene. This can be used
    /// to see how certain functionality of the coin api can be accessed - but is largely useless to
    /// you and can be deleted once you are done with the samples.
    /// </summary>
    public class CoinSampleBooklet : MonoBehaviour
    {

        #region Inspector Variables
        public Coin coin = default;
        public Text headsPercentageText = default;
        public Text tailsPercentageText = default;
        public InputField durationInputField = default;
        public InputField heightInputField = default;
        public InputField flipsInputField = default;
        public Image headsImage = default;
        public Image tailsImage = default;
        #endregion
        
        #region Public Events

        // Toss the coin up in the air and back to its current position
        public void FlipCoin()
        {
            coin.Flip( coin.transform.position );
        }

        public void StopCoin()
        {
            coin.Stop();
        }
        // Update the weighting of heads or tails
        public void SetWeighting( float tailsWeighting )
        {
            coin.HeadsWeight = 1.0f - tailsWeighting;
            headsPercentageText.text = ((1.0f - tailsWeighting) * 100.0f).ToString("0") + "%";
            tailsPercentageText.text = (tailsWeighting * 100.0f).ToString("0") + "%";
        }

        #endregion

        #region Private

        private void Awake()
        {
            // initialise the events and UI elements
            coin.OnSettled += SetVisibleSide;
            SetVisibleSide(coin.Side);
            SetWeighting(coin.HeadsWeight);

            durationInputField.text = coin.FlipDuration.ToString();
            heightInputField.text = coin.FlipHeight.ToString();
            flipsInputField.text = coin.RotationCount.ToString();

            durationInputField.onValueChanged.AddListener((value) =>
            {
                if (float.TryParse(value, out float duration))
                    coin.FlipDuration = duration;
                else
                    durationInputField.text = coin.FlipDuration.ToString();
            });
            
            heightInputField.onValueChanged.AddListener((value) =>
            {
                if (float.TryParse(value, out float height))
                    coin.FlipHeight = height;
                else
                    heightInputField.text = coin.FlipHeight.ToString();
            });
            
            flipsInputField.onValueChanged.AddListener((value) =>
            {
                if (int.TryParse(value, out int flips))
                    coin.RotationCount = flips;
                else
                    flipsInputField.text = coin.RotationCount.ToString();
            });
        }

        /// <summary>
        /// Update the booklet ui to display which side is currently showing
        /// </summary>
        private void SetVisibleSide( Coin.CoinSide side )
        {
            headsImage.gameObject.SetActive(side == Coin.CoinSide.Heads);
            tailsImage.gameObject.SetActive(side == Coin.CoinSide.Tails);
        }
        
        #endregion
    }
}