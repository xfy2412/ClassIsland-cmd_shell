﻿using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using ClassIsland.Core.Abstractions.Services;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using ClassIsland.Shared.Helpers;
using CommunityToolkit.Mvvm.ComponentModel;
using ClassIsland.Core.Models;
using ClassIsland.Core;
namespace ClassIsland.Services;

public class AutomationService : ObservableRecipient, IAutomationService
{
    public static readonly string AutomationConfigsFolderPath = Path.Combine(App.AppConfigPath, "Automations/");
    string CurrentConfigPath => Path.GetFullPath(Path.Combine(AutomationConfigsFolderPath, SettingsService.Settings.CurrentAutomationConfig + ".json"));

    public AutomationService(ILogger<AutomationService> logger, IRulesetService rulesetService, SettingsService settingsService, IActionService actionService, IWindowRuleService windowRuleService)
    {
        Logger = logger;
        RulesetService = rulesetService;
        SettingsService = settingsService;
        ActionService = actionService;
        WindowRuleService = windowRuleService;

        RefreshConfigs();
        LoadConfig();
        RefreshConfigs();

        RulesetService.StatusUpdated += RulesetServiceOnStatusUpdated;
        Workflows.CollectionChanged += (_, _) => SaveConfig();
        SettingsService.Settings.PropertyChanged += (_, e) => {
            if (e.PropertyName == nameof(SettingsService.Settings.CurrentAutomationConfig))
                LoadConfig();
        };
    }

    public ILogger<AutomationService> Logger { get; }
    public IRulesetService RulesetService { get; }
    public SettingsService SettingsService { get; }
    public IActionService ActionService { get; }
    public IWindowRuleService WindowRuleService { get; }

    private void RulesetServiceOnStatusUpdated(object? sender, EventArgs e)
    {
        if (!SettingsService.Settings.IsAutomationEnabled) return;

        var isPropertyChanged = false; // 如果 Actionset.IsOn 改变，则触发保存。
        foreach (var a in Workflows)
        {
            if (!a.Actionset.IsEnabled) continue;
            if (!a.Actionset.IsOn)
            {
                //if (WindowRuleService.IsForegroundWindowClassIsland()) continue;
                if (!RulesetService.IsRulesetSatisfied(a.Ruleset)) continue;
                ActionService.Invoke(a.Actionset);
                a.Actionset.IsOn = true;
            }
            else
            {
                if (RulesetService.IsRulesetSatisfied(a.Ruleset)) continue;
                ActionService.Revert(a.Actionset);
                a.Actionset.IsOn = false;
            }
            isPropertyChanged = true;
        }
        if (isPropertyChanged)
            SaveConfig();
    }

    private void LoadConfig()
    {
        if (File.Exists(CurrentConfigPath))
        {
            Workflows = ConfigureFileHelper.LoadConfig<ObservableCollection<Workflow>>(CurrentConfigPath);
        }
        else
        {
            Workflows = ConfigureFileHelper.CopyObject(new ObservableCollection<Workflow>());
            SaveConfig();
        }
    }

    public void SaveConfig(string note = "")
    {
        Logger.LogInformation(note == "" ?
            $"写入自动化配置（{SettingsService.Settings.CurrentAutomationConfig}.json）" :
            $"写入自动化配置（{SettingsService.Settings.CurrentAutomationConfig}.json）：{note}");
        ConfigureFileHelper.SaveConfig(CurrentConfigPath, Workflows);
    }

    public void RefreshConfigs()
    {
        Configs = Directory.GetFiles(AutomationConfigsFolderPath, "*.json")
                           .Select(Path.GetFileNameWithoutExtension)
                           .SkipWhile(x => x is null)
                           .ToList()!;
    }

    ObservableCollection<Workflow> _automations = [];
    public ObservableCollection<Workflow> Workflows
    {
        get => _automations;
        set
        {
            if (Equals(value, _automations)) return;
            _automations = value;
            OnPropertyChanged();
        }
    }

    IReadOnlyList<string> _configs = [];
    public IReadOnlyList<string> Configs
    {
        get => _configs;
        set
        {
            if (Equals(value, _configs)) return;
            _configs = value;
            OnPropertyChanged();
        }
    }
}
