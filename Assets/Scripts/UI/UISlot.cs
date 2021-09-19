namespace FishRPG.UI
{
    using UnityEngine;
    using UnityEngine.EventSystems;

    public class UISlot : MonoBehaviour, IPointerClickHandler,
        IPointerDownHandler, IPointerUpHandler,
        IPointerEnterHandler, IPointerExitHandler
    {

        // TODO expand these for controllers

        #region Pointer Click

        public void OnPointerClick(PointerEventData eventData)
        {

            switch (eventData.button)
            {
                case PointerEventData.InputButton.Left:
                    HandleLeftClick();
                    break;
                case PointerEventData.InputButton.Right:
                    HandleRightClick();
                    break;
                case PointerEventData.InputButton.Middle:
                    HandleMiddleClick();
                    break;
            }
        }

        protected virtual void HandleLeftClick() { }
        protected virtual void HandleRightClick() { }
        protected virtual void HandleMiddleClick() { }

        #endregion

        #region Pointer Down

        public void OnPointerDown(PointerEventData eventData)
        {
            switch (eventData.button)
            {
                case PointerEventData.InputButton.Left:
                    HandleLeftDown();
                    break;
                case PointerEventData.InputButton.Right:
                    HandleRightDown();
                    break;
                case PointerEventData.InputButton.Middle:
                    HandleMiddleDown();
                    break;
            }
        }

        protected virtual void HandleLeftDown() { }
        protected virtual void HandleRightDown() { }
        protected virtual void HandleMiddleDown() { }

        #endregion

        #region Pointer Up

        public void OnPointerUp(PointerEventData eventData)
        {
            switch (eventData.button)
            {
                case PointerEventData.InputButton.Left:
                    HandleLeftUp();
                    break;
                case PointerEventData.InputButton.Right:
                    HandleRightUp();
                    break;
                case PointerEventData.InputButton.Middle:
                    HandleMiddleUp();
                    break;
            }
        }

        protected virtual void HandleLeftUp() { }
        protected virtual void HandleRightUp() { }
        protected virtual void HandleMiddleUp() { }

        #endregion

        #region Pointer Enter

        public void OnPointerEnter(PointerEventData eventData)
        {
            HandlePointerEnter();
        }

        protected virtual void HandlePointerEnter() { }

        #endregion

        #region Pointer Exit

        public void OnPointerExit(PointerEventData eventData)
        {
            HandlePointerExit();
        }

        /// <summary>
        /// Hides tooltip by default.
        /// </summary>
        protected virtual void HandlePointerExit() => UITooltip.Hide();

        #endregion

    }
}
