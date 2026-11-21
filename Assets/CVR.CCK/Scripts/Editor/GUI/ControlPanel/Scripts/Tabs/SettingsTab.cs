using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using CCK.CE.Hacks;
using CVR.CCK.Editor.ContentBuilder;
using CVR.CCKEditor.ContentBuilder;
using CVR.CCKEditor.Localization;
using CVR.CCKEditor.Tools;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

public class SettingsTab : CCKTabBase
{
    #region Tab Properties
    
    public override string FullName => "Settings Tab";
    public override string ShortName => "Settings";
    public override string IconResource => "CCKControlPanel/Icons/settings";
    
    public override bool RequiresAuthentication => false;
    
    #endregion Tab Properties

    #region Private Fields
    
    private ListView _listView;
    
    #endregion Private Fields

    #region Tab Lifecycle
    
    protected override void OnCreateTab()
    {
        Resources.Load<VisualTreeAsset>("CCKControlPanel/UI/Settings").CloneTree(TabContainer);
        CCKLocalizationManager.LocalizeVisualTree(TabContainer, "SettingsTab");

        // Listen for setting chanegs

        DropdownField uploadRegion = TabContainer.Q<DropdownField>("dropdown-upload-region");
        uploadRegion.SetValueWithoutNotify(CCKEditorPrefs.PreferredUploadRegion);
        uploadRegion.RegisterValueChangedCallback(_ => CCKEditorPrefs.PreferredUploadRegion = uploadRegion.index);
        
        Toggle dingOnComplete = TabContainer.Q<Toggle>("toggle-ding-on-complete");
        dingOnComplete.SetValueWithoutNotify(CCKEditorPrefs.DingOnComplete);
        dingOnComplete.RegisterValueChangedCallback(changedEvent => CCKEditorPrefs.DingOnComplete = changedEvent.newValue);

        Button dingTest = TabContainer.Q<Button>("button-ding-on-complete-test");
        dingTest.clicked += DingOnComplete.PlayDing;
        
        Toggle warningPartialLanguage = TabContainer.Q<Toggle>("toggle-partial-localization-warning");
        warningPartialLanguage.SetValueWithoutNotify(CCKEditorPrefs.WarningPartialEditorLocalization);
        warningPartialLanguage.RegisterValueChangedCallback(changedEvent => CCKEditorPrefs.WarningPartialEditorLocalization = changedEvent.newValue);
        
        Toggle useDevWasmModule = TabContainer.Q<Toggle>("toggle-use-dev-wasm-module");
        useDevWasmModule.SetValueWithoutNotify(CCKEditorPrefs.UseDevWasmModule);
        useDevWasmModule.RegisterValueChangedCallback(changedEvent => CCKEditorPrefs.UseDevWasmModule = changedEvent.newValue);
        
        Toggle wasmVerboseBuild = TabContainer.Q<Toggle>("toggle-wasm-verbose-build");
        wasmVerboseBuild.SetValueWithoutNotify(CCKEditorPrefs.WasmVerboseBuild);
        wasmVerboseBuild.RegisterValueChangedCallback(changedEvent => CCKEditorPrefs.WasmVerboseBuild = changedEvent.newValue);
        
        /*
        Toggle apiConnectionDebug = TabContainer.Q<Toggle>("toggle-api-logging");
        apiConnectionDebug.SetValueWithoutNotify(CCKEditorPrefs.ApiConnectionDebugLogging);
        apiConnectionDebug.RegisterValueChangedCallback(changedEvent => CCKEditorPrefs.ApiConnectionDebugLogging = changedEvent.newValue);
        */
        
        Toggle biggerSendBuffer = TabContainer.Q<Toggle>("toggle-bigger-send-buffer");
        biggerSendBuffer.SetValueWithoutNotify(CCKEditorPrefs.UseLargerUploadBuffer);
        biggerSendBuffer.RegisterValueChangedCallback(changedEvent => CCKEditorPrefs.UseLargerUploadBuffer = changedEvent.newValue);
        
        // LoadProcessorInfos();
        // SetupListView();
    }

    protected override void OnDestroyTab()
    {
        // Explode :)
        // DestroyListView();
    }

    protected override void HandleLayout() { } // No
    
    #endregion Tab Lifecycle

    #region Advanced - Build Processers
    
    /*
    private List<CCKBuildProcessorSettings.ProcessorInfo> _processorInfos = new();
    
    private void SetupListView()
    {
        _listView = TabContainer.Q<ListView>("Meow"); // Uh
        
        _listView.itemsSource = _processorInfos;
        _listView.fixedItemHeight = 26;
        _listView.reorderable = true;
        _listView.selectionType = SelectionType.None;
        _listView.makeItem = MakeListItem;
        _listView.bindItem = BindListItem;
        _listView.RefreshItems();

        _listView.onSelectedIndicesChange += OnSelectedIndicesChanged;
    }

    private void DestroyListView()
    {
        _listView = null;
        _processorInfos.Clear();
    }

    private VisualElement MakeListItem()
    {
        // TODO: Need proper visual element we instantiate instead of this in code, cause this is meh
        var row = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };

        var toggle = new Toggle { style = { marginRight = 6, width = 20 } };
        toggle.name = "Toggle";
        row.Add(toggle);

        var label = new Label { style = { flexGrow = 1 } };
        label.name = "Label";
        row.Add(label);

        var field = new IntegerField { style = { width = 50, marginRight = 6 } };
        field.name = "OrderField";
        row.Add(field);

        var reset = new Button { text = "↺", style = { width = 24, paddingLeft = 0, paddingRight = 0 } };
        reset.name = "ResetButton";
        row.Add(reset);

        return row;
    }

    private void BindListItem(VisualElement element, int index)
    {
        var info = _processorInfos[index];

        var toggle = element.Q<Toggle>("Toggle");
        var label = element.Q<Label>("Label");
        var orderField = element.Q<IntegerField>("OrderField");
        var resetButton = element.Q<Button>("ResetButton");

        label.text = info.DisplayName;
        toggle.SetValueWithoutNotify(info.IsEnabled);
        orderField.SetValueWithoutNotify(GetEffectiveOrder(info));
        
        // Visual indication: gray for default order, white for custom
        orderField.style.color = info.HasCustomOrder ? Color.white : Color.gray;
        
        toggle.RegisterValueChangedCallback(OnToggleChanged);
        orderField.RegisterValueChangedCallback(OnOrderChanged);

        resetButton.clicked += () => ResetOrder(info);
        return;

        void OnToggleChanged(ChangeEvent<bool> evt)
        {
            CCKBuildProcessorSettings.Instance.SetProcessorEnabled(info.TypeName, evt.newValue);
        }
        void OnOrderChanged(ChangeEvent<int> evt)
        {
            CCKBuildProcessorSettings.Instance.SetProcessorOrder(info.TypeName, evt.newValue);
            SortAndRefresh();
        }
    }
    
    private void LoadProcessorInfos()
    {
        _processorInfos.Clear();
        _processorInfos.AddRange(CCKBuildProcessorSettings.Instance.Settings.ProcessorSettings.Values);

        // Sort by effective order (custom if available, otherwise default)
        _processorInfos.Sort((a, b) => GetEffectiveOrder(a).CompareTo(GetEffectiveOrder(b)));
    }

    private int GetEffectiveOrder(CCKBuildProcessorSettings.ProcessorInfo info)
    {
        return info.HasCustomOrder ? info.CustomOrder : info.DefaultOrder;
    }

    private void ResetOrder(CCKBuildProcessorSettings.ProcessorInfo info)
    {
        CCKBuildProcessorSettings.Instance.ResetProcessorOrder(info.TypeName);
        SortAndRefresh();
    }

    private void SortAndRefresh()
    {
        _processorInfos.Sort((a, b) => GetEffectiveOrder(a).CompareTo(GetEffectiveOrder(b)));
        _listView.itemsSource = _processorInfos;
        _listView.RefreshItems();
    }
    
    private void OnSelectedIndicesChanged(IEnumerable<int> _)
    {
        // If I had reordering logic it would go here

        // And then we refresh & save to disk & hopefully not die
        SortAndRefresh();
    }*/
    
    #endregion Advanced - Build Processers
}