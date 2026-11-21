#if UNITY_EDITOR
using UnityEngine.UIElements;

public interface ICCKTab
{
    string FullName { get; }
    string ShortName { get; }
    string IconResource { get; }
    bool RequiresAuthentication { get; }
    void CreateTab(VisualElement container);
    void DestroyTab();
    void OnLayout(); // Called during EditorWindow.OnGUI Layout event
    void OnAuthLoggedOut();
}

public abstract class CCKTabBase : ICCKTab
{
    protected VisualElement TabContainer { get; private set; }
    public abstract string FullName { get; }
    public abstract string ShortName { get; }
    public abstract string IconResource { get; }
    public abstract bool RequiresAuthentication { get; }
    
    public void CreateTab(VisualElement container)
    {
        TabContainer = container;
        OnCreateTab();
    }
    
    public void DestroyTab()
    {
        OnDestroyTab();
        TabContainer = null;
    }
    
    public void OnLayout()
    {
        if (TabContainer == null) return;
        HandleLayout();
    }
    
    protected abstract void OnCreateTab();
    protected abstract void OnDestroyTab();
    protected abstract void HandleLayout();
    public virtual void OnAuthLoggedOut() { } // Optional method for auth state changes to reset state
}
#endif