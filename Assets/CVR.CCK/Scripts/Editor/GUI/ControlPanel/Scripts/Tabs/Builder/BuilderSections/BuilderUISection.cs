
using UnityEngine.UIElements;

public partial class BuilderTab
{
    private abstract class BuilderUISection : System.IDisposable
    {
        protected readonly VisualElement Root;
        protected readonly BuilderState State;

        protected BuilderUISection(VisualElement root, BuilderState state)
        {
            Root = root;
            State = state;
        }

        public abstract void LockInterface(bool locked);
        public abstract void ClearFields();
        public abstract void Dispose();
    }
}