using System;
using System.Collections.Generic;
using Comfort.Common;
using EFT.UI;
using EFT.UI.Settings;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

using static StashSearch.Utils.InstanceManager.SearchObjects;

namespace StashSearch.Search;

public class SettingsComponent : MonoBehaviour
{
    private CommonUI _commonUI => Singleton<CommonUI>.Instance;
    
    private ControlSettingsTab _controlSettingsTab;
    private GameObject _controlSettingsPanel;
    
    private List<CommandKeyPair> _commandKeyPairs;
    private List<CommandAxisPair> _commandAxisPairs;
    
    private Vector3 _oldContolPanelLocalPosition;
    
    // Search GameObject and TMP_InputField
    private GameObject _searchObject;
    private TMP_InputField _inputField;
    
    // Button GameObject
    private GameObject _searchRestoreButtonObject;
    private Button _searchRestoreButton;
    
    private void Awake()
    {
        _controlSettingsTab = (ControlSettingsTab)AccessTools
            .Field(typeof(SettingsScreen), "_controlsSettingsTabScreen")
            .GetValue(_commonUI.SettingsScreen);
        
        _controlSettingsPanel = (GameObject)AccessTools
            .Field(typeof(ControlSettingsTab), "_controlPanel")
            .GetValue(_controlSettingsTab);
        
        
        
        // Instantiate the prefab, set its anchor to the settings panel
        _searchObject = Instantiate(PlayerSearchBoxPrefab, _controlSettingsPanel.transform);
        _searchRestoreButtonObject = Instantiate(SearchRestoreButtonPrefab, _controlSettingsPanel.transform);
        
        // Adjust the rects anchored position
        _searchObject.RectTransform().localPosition = new Vector3(-461, 412);
        _searchRestoreButtonObject.RectTransform().localPosition = new Vector3(-119, 412);
        
        _inputField = _searchObject.GetComponentInChildren<TMP_InputField>();
        
        _inputField.placeholder.GetComponent<TextMeshProUGUI>().SetText("Search Settings");
        _inputField.placeholder.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Italic;
        
        _searchRestoreButton = _searchRestoreButtonObject.GetComponentInChildren<Button>();
        
        _inputField.onEndEdit.AddListener((_) => Search());
        _searchRestoreButton.onClick.AddListener(() => ClearSearch());
    }

    private void OnEnable()
    {
        _oldContolPanelLocalPosition = _controlSettingsPanel.transform.localPosition;
        _controlSettingsPanel.transform.localPosition = new Vector3(-30, -95, 0);
    }

    private void OnDisable()
    {
        _controlSettingsPanel.transform.localPosition = _oldContolPanelLocalPosition;
    }

    private void Search()
    {
        Plugin.Log.LogDebug($"Settings Search Input: {_inputField.text}");
        
        _commandKeyPairs = (List<CommandKeyPair>)AccessTools
            .Field(typeof(ControlSettingsTab), "_keyControls")
            .GetValue(_controlSettingsTab);
        
        _commandAxisPairs = (List<CommandAxisPair>)AccessTools
            .Field(typeof(ControlSettingsTab), "_axisControls")
            .GetValue(_controlSettingsTab);
        
        // don't search for empty string
        if (_inputField.text == string.Empty)
        {
            return;
        }
        
        foreach (var key in _commandKeyPairs)
        {
            if (!key.name.ToLower().Contains(_inputField.text.ToLower()))
            {
                key.gameObject.SetActive(false);
            }
        }
        
        foreach (var key in _commandAxisPairs)
        {
            if (!key.name.ToLower().Contains(_inputField.text.ToLower()))
            {
                key.gameObject.SetActive(false);
            }
        }
        
        _inputField.text = string.Empty;
    }

    private void ClearSearch()
    {
        _inputField.text = string.Empty;
        
        _commandKeyPairs = (List<CommandKeyPair>)AccessTools
            .Field(typeof(ControlSettingsTab), "_keyControls")
            .GetValue(_controlSettingsTab);
        
        _commandAxisPairs = (List<CommandAxisPair>)AccessTools
            .Field(typeof(ControlSettingsTab), "_axisControls")
            .GetValue(_controlSettingsTab);
        
        foreach (var key in _commandKeyPairs)
        {
            key.gameObject.SetActive(true);
        }
        
        foreach (var key in _commandAxisPairs)
        {
            key.gameObject.SetActive(true);
        }
    }
    
    private enum KeyCodes
    {
        LeanLockRight,
        LeanLockLeft,
        Shoot,
        Aim,
        ChangeAimScope,
        ChangeAimScopeMagnification,
        Nidnod,
        ToggleGoggles,
        ToggleHeadLight,
        SwitchHeadLight,
        ToggleVoip,
        PushToTalk,
        Mumble,
        MumbleDropdown,
        MumbleQuick,
        WatchTime,
        WatchTimerAndExits,
        Tactical,
        NextTacticalDevice,
        Next,
        Previous,
        Interact,
        ThrowGrenade,
        ReloadWeapon,
        QuickReloadWeapon,
        DropBackpack,
        NextMagazine,
        PreviousMagazine,
        CheckAmmo,
        ShootingMode,
        ForceAutoWeaponMode,
        CheckFireMode,
        CheckChamber,
        ChamberUnload,
        UnloadMagazine,
        Prone,
        Sprint,
        Duck,
        NextWalkPose,
        PreviousWalkPose,
        Walk,
        BlindShootAbove,
        BlindShootRight,
        StepRight,
        StepLeft,
        ExamineWeapon,
        FoldStock,
        Inventory,
        Jump,
        Knife,
        QuickKnife,
        PrimaryWeaponFirst,
        PrimaryWeaponSecond,
        SecondaryWeapon,
        QuickSecondaryWeapon,
        Slot4,
        Slot5,
        Slot6,
        Slot7,
        Slot8,
        Slot9,
        Slot0,
        OpticCalibrationSwitchUp,
        OpticCalibrationSwitchDown,
        MakeScreenshot,
        ThrowItem,
        Breath,
        ToggleInfo,
        Console,
        LeftStance,
        Vaulting,
    }
}

