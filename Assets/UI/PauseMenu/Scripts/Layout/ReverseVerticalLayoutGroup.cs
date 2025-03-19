namespace UnityEngine.UI {
    [AddComponentMenu("Layout/Reverse Vertical Layout Group")]

    public class ReverseVerticalLayoutGroup : VerticalLayoutGroup {
        protected ReverseVerticalLayoutGroup() { }

        /// <summary>
        /// This allows for UI elements to remain in the hierarchy according to draw order
        /// while allowing them to display in reversed vertical order.
        /// </summary>
        public override void SetLayoutVertical() {
            RectTransform[] reversedRectChildren = new RectTransform[rectChildren.Count];
            int rectChildrenCount = rectChildren.Count;
            int lastIndex = rectChildrenCount - 1;
            for (int i = lastIndex; i >= 0; i--)
                reversedRectChildren[lastIndex - i] = rectChildren[i];
            for (int i = 0; i < rectChildrenCount; i++)
                rectChildren[i] = reversedRectChildren[i];

            base.SetLayoutVertical();
        }
    }
}