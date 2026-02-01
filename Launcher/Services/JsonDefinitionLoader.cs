// Copyright (c) 2025 A Solution IT LLC. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using Launcher.Models;
using Launcher.ViewModels;

namespace Launcher.Services
{
    public class JsonDefinitionLoader
    {
        public static ScriptData LoadFromJson(string jsonPath, out WizardBranding branding)
        {
            if (!File.Exists(jsonPath))
            {
                throw new FileNotFoundException("JSON file not found.", jsonPath);
            }

            string jsonContent = File.ReadAllText(jsonPath, Encoding.UTF8);
            
            UIDefinitionJson definition;
            var settings = new DataContractJsonSerializerSettings { UseSimpleDictionaryFormat = true };
            
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(jsonContent)))
            {
                var serializer = new DataContractJsonSerializer(typeof(UIDefinitionJson), settings);
                definition = (UIDefinitionJson)serializer.ReadObject(stream);
            }

            if (definition == null)
            {
                throw new InvalidOperationException("Failed to deserialize JSON");
            }

            var scriptData = new ScriptData();
            branding = MapBranding(definition);
            scriptData.Branding = branding; // Store branding in ScriptData for later access
            scriptData.SidebarHeaderIconOrientation = branding != null ? branding.SidebarHeaderIconOrientation : "Left";
            scriptData.ScriptBody = definition.ScriptBody;
            scriptData.WizardSteps = MapSteps(definition);

            return scriptData;
        }

        private static WizardBranding MapBranding(UIDefinitionJson definition)
        {
            var branding = new WizardBranding
            {
                WindowTitleText = definition.Title ?? "PoshUI",
                Theme = "Auto"
            };

            if (definition.Branding != null)
            {
                branding.WindowTitleText = definition.Branding.WindowTitleText ?? definition.Title ?? "PoshUI";
                branding.WindowTitleIcon = definition.Branding.WindowTitleIcon;
                branding.SidebarHeaderText = definition.Branding.SidebarHeaderText;
                branding.SidebarHeaderIcon = definition.Branding.SidebarHeaderIcon ?? definition.Branding.SidebarHeaderIconPath;
                branding.SidebarHeaderIconOrientation = definition.Branding.SidebarHeaderIconOrientation ?? "Left";
                branding.OriginalScriptName = definition.Branding.OriginalScriptName;
                branding.OriginalScriptPath = definition.Branding.OriginalScriptPath;
            }

            return branding;
        }

        private static List<WizardStep> MapSteps(UIDefinitionJson definition)
        {
            var steps = new List<WizardStep>();
            if (definition.Steps == null) return steps;

            int order = 0;
            LoggingService.Info($"MapSteps: Processing {definition.Steps?.Count ?? 0} steps", component: "JsonDefinitionLoader");
            foreach (var stepJson in definition.Steps)
            {
                var stepType = stepJson.Type ?? "Wizard";
                string pageType = (stepType == "Dashboard" || stepType == "CardGrid") ? "CardGrid" : "GenericForm";
                LoggingService.Info($"  Step '{stepJson.Title}': Type={stepType}, Banner={stepJson.Banner != null}, Cards={stepJson.Cards?.Count ?? 0}, Controls={stepJson.Controls?.Count ?? 0}", component: "JsonDefinitionLoader");

                var step = new WizardStep
                {
                    Title = stepJson.Title ?? "",
                    Description = stepJson.Description ?? "",
                    Order = stepJson.Order > 0 ? stepJson.Order : ++order,
                    PageType = pageType,
                    IconPath = stepJson.Icon,
                    Parameters = MapControls(stepJson),
                    Controls = MapBannersAndCards(stepJson)
                };

                steps.Add(step);
            }

            return steps.OrderBy(s => s.Order).ToList();
        }

        private static System.Collections.ArrayList MapBannersAndCards(UIStepJson stepJson)
        {
            var controls = new System.Collections.ArrayList();

            LoggingService.Info($"MapBannersAndCards: Step has Banner={stepJson.Banner != null}, Banners={stepJson.Banners?.Count ?? 0}, Cards={stepJson.Cards?.Count ?? 0}", component: "JsonDefinitionLoader");

            // Map Banners array if present (new format - multiple banners)
            if (stepJson.Banners != null && stepJson.Banners.Count > 0)
            {
                foreach (var b in stepJson.Banners)
                {
                    controls.Add(new DynamicControl(CreateBannerDictionary(b)));
                }
                LoggingService.Info($"MapBannersAndCards: Added {stepJson.Banners.Count} banners from Banners array", component: "JsonDefinitionLoader");
            }
            // Map single Banner if present (backward compatibility)
            else if (stepJson.Banner != null)
            {
                controls.Add(new DynamicControl(CreateBannerDictionary(stepJson.Banner)));
            }

            // Map Cards if present
            if (stepJson.Cards != null && stepJson.Cards.Count > 0)
            {
                LoggingService.Info($"MapBannersAndCards: Processing {stepJson.Cards.Count} cards", component: "JsonDefinitionLoader");
                foreach (var cardJson in stepJson.Cards)
                {
                    var cardType = cardJson.CardType ?? cardJson.Type ?? "InfoCard";
                    LoggingService.Info($"  Card: CardType={cardJson.CardType}, Type={cardJson.Type}, Title={cardJson.Title}, Content={cardJson.Content?.Substring(0, Math.Min(50, cardJson.Content?.Length ?? 0))}", component: "JsonDefinitionLoader");
                    var cardControl = new Dictionary<string, object>
                    {
                        { "Type", cardType },
                        { "Name", cardJson.Name ?? "" },
                        { "Label", cardJson.Title ?? "" },
                        { "Title", cardJson.Title ?? "" },
                        { "Description", cardJson.Description ?? "" },
                        { "Subtitle", cardJson.Subtitle ?? "" },
                        { "Icon", cardJson.Icon ?? "" },
                        { "Category", cardJson.Category ?? "" }
                    };

                    // Map top-level InfoCard properties (from serialization)
                    if (!string.IsNullOrEmpty(cardJson.Content)) cardControl["Content"] = cardJson.Content;
                    if (!string.IsNullOrEmpty(cardJson.IconPath)) cardControl["IconPath"] = cardJson.IconPath;
                    if (!string.IsNullOrEmpty(cardJson.ImagePath)) cardControl["ImagePath"] = cardJson.ImagePath;
                    if (cardJson.ImageOpacity > 0) cardControl["ImageOpacity"] = cardJson.ImageOpacity;
                    if (!string.IsNullOrEmpty(cardJson.LinkUrl)) cardControl["LinkUrl"] = cardJson.LinkUrl;
                    if (!string.IsNullOrEmpty(cardJson.LinkText)) cardControl["LinkText"] = cardJson.LinkText;
                    if (!string.IsNullOrEmpty(cardJson.BackgroundColor)) cardControl["BackgroundColor"] = cardJson.BackgroundColor;
                    if (!string.IsNullOrEmpty(cardJson.TitleColor)) cardControl["TitleColor"] = cardJson.TitleColor;
                    if (!string.IsNullOrEmpty(cardJson.ContentColor)) cardControl["ContentColor"] = cardJson.ContentColor;
                    if (cardJson.CornerRadius > 0) cardControl["CornerRadius"] = cardJson.CornerRadius;
                    if (!string.IsNullOrEmpty(cardJson.GradientStart)) cardControl["GradientStart"] = cardJson.GradientStart;
                    if (!string.IsNullOrEmpty(cardJson.GradientEnd)) cardControl["GradientEnd"] = cardJson.GradientEnd;
                    if (cardJson.Width != null) cardControl["Width"] = cardJson.Width;
                    if (cardJson.Height != null) cardControl["Height"] = cardJson.Height;
                    if (!string.IsNullOrEmpty(cardJson.ScriptPath)) cardControl["ScriptPath"] = cardJson.ScriptPath;
                    if (!string.IsNullOrEmpty(cardJson.ScriptBlock)) cardControl["ScriptBlock"] = cardJson.ScriptBlock;

                    // Map properties from Properties object if present
                    if (cardJson.Properties != null)
                    {
                        var props = cardJson.Properties;
                        if (!string.IsNullOrEmpty(props.CardTitle)) cardControl["CardTitle"] = props.CardTitle;
                        if (!string.IsNullOrEmpty(props.CardDescription)) cardControl["CardDescription"] = props.CardDescription;
                        if (!string.IsNullOrEmpty(props.Category)) cardControl["Category"] = props.Category;
                        if (!string.IsNullOrEmpty(props.Icon)) cardControl["Icon"] = props.Icon;
                        if (props.Value != null) cardControl["Value"] = props.Value;
                        if (!string.IsNullOrEmpty(props.Unit)) cardControl["Unit"] = props.Unit;
                        if (!string.IsNullOrEmpty(props.Format)) cardControl["Format"] = props.Format;
                        if (!string.IsNullOrEmpty(props.Trend)) cardControl["Trend"] = props.Trend;
                        if (props.TrendValue != null) cardControl["TrendValue"] = props.TrendValue;
                        if (props.Target != null) cardControl["Target"] = props.Target;
                        if (props.MinValue != null) cardControl["MinValue"] = props.MinValue;
                        if (props.MaxValue != null) cardControl["MaxValue"] = props.MaxValue;
                        cardControl["ShowProgressBar"] = props.ShowProgressBar;
                        cardControl["ShowTrend"] = props.ShowTrend;
                        cardControl["ShowTarget"] = props.ShowTarget;
                        if (!string.IsNullOrEmpty(props.RefreshScript)) cardControl["RefreshScript"] = props.RefreshScript;
                        if (!string.IsNullOrEmpty(props.ChartType)) cardControl["ChartType"] = props.ChartType;
                        if (props.Data != null) cardControl["Data"] = props.Data;
                        cardControl["ShowLegend"] = props.ShowLegend;
                        cardControl["ShowTooltip"] = props.ShowTooltip;
                        if (!string.IsNullOrEmpty(props.Content)) cardControl["Content"] = props.Content;
                        else if (!string.IsNullOrEmpty(props.CardContent)) cardControl["Content"] = props.CardContent;
                        if (!string.IsNullOrEmpty(props.ImageSource)) cardControl["ImageSource"] = props.ImageSource;
                        if (props.Width != null) cardControl["Width"] = props.Width;
                        if (props.Height != null) cardControl["Height"] = props.Height;
                        if (!string.IsNullOrEmpty(props.BackgroundColor)) cardControl["BackgroundColor"] = props.BackgroundColor;
                        if (!string.IsNullOrEmpty(props.TextColor)) cardControl["TextColor"] = props.TextColor;
                        if (!string.IsNullOrEmpty(props.BorderColor)) cardControl["BorderColor"] = props.BorderColor;
                        if (!string.IsNullOrEmpty(props.LinkUrl)) cardControl["LinkUrl"] = props.LinkUrl;
                        if (!string.IsNullOrEmpty(props.ScriptBlock)) cardControl["ScriptBlock"] = props.ScriptBlock;
                        if (!string.IsNullOrEmpty(props.ScriptPath)) cardControl["ScriptPath"] = props.ScriptPath;
                        if (!string.IsNullOrEmpty(props.ScriptSource)) cardControl["ScriptSource"] = props.ScriptSource;
                        if (!string.IsNullOrEmpty(props.ParameterControls)) cardControl["ParameterControls"] = props.ParameterControls;
                        if (!string.IsNullOrEmpty(props.DefaultParameters)) cardControl["DefaultParameters"] = props.DefaultParameters;
                    }

                    controls.Add(new DynamicControl(cardControl));
                }
            }

            return controls;
        }

        /// <summary>
        /// Creates a dictionary of banner properties from a UIBannerJson object.
        /// </summary>
        private static Dictionary<string, object> CreateBannerDictionary(UIBannerJson b)
        {
            return new Dictionary<string, object>
            {
                    // Core
                    { "Type", "Banner" },
                    { "Name", "StepBanner" },
                    { "Label", b.Title ?? "" },
                    { "BannerTitle", b.Title ?? "" },
                    { "BannerSubtitle", b.Subtitle ?? "" },
                    { "BannerIcon", b.Icon ?? "" },
                    { "BannerType", b.Type ?? "info" },
                    { "Description", b.Description ?? "" },
                    { "Category", b.Category ?? "General" },
                    
                    // Layout & Sizing
                    { "Height", b.Height > 0 ? b.Height : 180 },
                    { "Width", b.Width > 0 ? b.Width : 700 },
                    { "Layout", b.Layout ?? "Left" },
                    { "ContentAlignment", b.ContentAlignment ?? "Left" },
                    { "VerticalAlignment", b.VerticalAlignment ?? "Center" },
                    { "Padding", b.Padding ?? "32,24" },
                    { "CornerRadius", b.CornerRadius > 0 ? b.CornerRadius : 12 },
                    { "FullWidth", b.FullWidth },
                    
                    // Typography
                    { "TitleFontSize", b.TitleFontSize ?? "32" },
                    { "SubtitleFontSize", b.SubtitleFontSize ?? "16" },
                    { "DescriptionFontSize", b.DescriptionFontSize ?? "14" },
                    { "TitleFontWeight", b.TitleFontWeight ?? "Bold" },
                    { "SubtitleFontWeight", b.SubtitleFontWeight ?? "Normal" },
                    { "FontFamily", b.FontFamily ?? "Segoe UI" },
                    { "TitleColor", b.TitleColor ?? "#FFFFFF" },
                    { "SubtitleColor", b.SubtitleColor ?? "#B0B0B0" },
                    { "DescriptionColor", b.DescriptionColor ?? "#909090" },
                    { "TitleAllCaps", b.TitleAllCaps },
                    
                    // Background & Visual Effects
                    { "BackgroundColor", b.BackgroundColor ?? "#2D2D30" },
                    { "BackgroundImagePath", b.BackgroundImagePath ?? "" },
                    { "BackgroundImageOpacity", b.BackgroundImageOpacity > 0 ? b.BackgroundImageOpacity : 0.3 },
                    { "GradientStart", b.GradientStart ?? "" },
                    { "GradientEnd", b.GradientEnd ?? "" },
                    { "GradientAngle", b.GradientAngle > 0 ? b.GradientAngle : 90 },
                    { "BorderColor", b.BorderColor ?? "Transparent" },
                    { "ShadowIntensity", b.ShadowIntensity ?? "Medium" },
                    
                    // Icon
                    { "IconPath", b.IconPath ?? "" },
                    { "IconSize", b.IconSize > 0 ? b.IconSize : 64 },
                    { "IconPosition", b.IconPosition ?? "Right" },
                    { "IconColor", b.IconColor ?? "#40FFFFFF" },
                    { "IconAnimation", b.IconAnimation ?? "None" },
                    
                    // Overlay Image
                    { "OverlayImagePath", b.OverlayImagePath ?? "" },
                    { "OverlayImageOpacity", b.OverlayImageOpacity > 0 ? b.OverlayImageOpacity : 0.5 },
                    { "OverlayPosition", b.OverlayPosition ?? "Right" },
                    { "OverlayImageSize", b.OverlayImageSize > 0 ? b.OverlayImageSize : 120 },
                    
                    // Carousel
                    { "CarouselSlidesJson", b.CarouselSlidesJson ?? "" },
                    { "AutoRotate", b.AutoRotate },
                    { "RotateInterval", b.RotateInterval > 0 ? b.RotateInterval : 3000 },
                    { "NavigationStyle", b.NavigationStyle ?? "Dots" },
                    
                    // Interactive
                    { "Clickable", b.Clickable },
                    { "LinkUrl", b.LinkUrl ?? "" },
                    { "ButtonText", b.ButtonText ?? "" },
                    { "ButtonColor", b.ButtonColor ?? "" }
            };
        }

        /// <summary>
        /// A simple wrapper class that exposes dictionary values as dynamic properties.
        /// This is used by CardGridViewModel.LoadCardsFromControls to access control data.
        /// </summary>
        public class DynamicControl : System.Dynamic.DynamicObject
        {
            private readonly Dictionary<string, object> _properties;

            public DynamicControl(Dictionary<string, object> properties)
            {
                _properties = properties ?? new Dictionary<string, object>();
            }

            public override bool TryGetMember(System.Dynamic.GetMemberBinder binder, out object result)
            {
                // First check if it's a known property
                if (binder.Name == "Properties")
                {
                    result = _properties;
                    return true;
                }
                if (binder.Name == "Type")
                {
                    result = Type;
                    return true;
                }
                if (binder.Name == "Name")
                {
                    result = Name;
                    return true;
                }
                if (binder.Name == "Label")
                {
                    result = Label;
                    return true;
                }
                // Otherwise look in the properties dictionary
                return _properties.TryGetValue(binder.Name, out result);
            }

            public string Type => _properties.TryGetValue("Type", out var t) ? t?.ToString() : null;
            public string Name => _properties.TryGetValue("Name", out var n) ? n?.ToString() : null;
            public string Label => _properties.TryGetValue("Label", out var l) ? l?.ToString() : null;

            public object GetProperty(string name)
            {
                return _properties.TryGetValue(name, out var v) ? v : null;
            }

            public object GetPropertyOrDefault(string name, object defaultValue)
            {
                return _properties.TryGetValue(name, out var v) ? v : defaultValue;
            }

            public Dictionary<string, object> Properties => _properties;
        }

        private static List<ParameterInfo> MapControls(UIStepJson stepJson)
        {
            var parameters = new List<ParameterInfo>();
            if (stepJson.Controls == null) return parameters;

            foreach (var controlJson in stepJson.Controls)
            {
                var param = MapControlToParameter(controlJson);
                if (param != null)
                {
                    parameters.Add(param);
                }
            }

            return parameters;
        }

        private static ParameterInfo MapControlToParameter(UIControlJson controlJson)
        {
            var controlType = (controlJson.Type ?? "textbox").ToLowerInvariant();
            var controlName = controlJson.Name ?? "";

            // Helper to get property from top-level or Properties dictionary
            Func<string, object> GetProp = (key) =>
            {
                if (controlJson.Properties != null && controlJson.Properties.ContainsKey(key))
                    return controlJson.Properties[key];
                return null;
            };

            var param = new ParameterInfo
            {
                Name = controlName,
                Label = controlJson.Label ?? controlName,
                DefaultValue = controlJson.Default,
                IsMandatory = controlJson.Mandatory,
                ValidationPattern = controlJson.ValidationPattern ?? "",
                ValidationScript = controlJson.ValidationScript ?? "",
                ControlWidth = controlJson.Width > 0 ? controlJson.Width : (double?)null
            };

            switch (controlType)
            {
                case "textbox":
                    param.ParameterType = typeof(string);
                    param.IsMultiLineText = controlJson.Multiline;
                    if (controlJson.MaxLength > 0) param.TextBoxMaxLength = controlJson.MaxLength;
                    param.TextBoxPlaceholder = controlJson.Placeholder;
                    break;
                case "password":
                    param.ParameterType = typeof(System.Security.SecureString);
                    param.PasswordShowReveal = controlJson.ShowRevealButton;
                    break;
                case "checkbox":
                    param.ParameterType = typeof(bool);
                    break;
                case "toggle":
                    param.ParameterType = typeof(bool);
                    param.IsSwitch = true;
                    break;
                case "dropdown":
                case "listbox":
                    param.ParameterType = typeof(string);
                    param.IsListBox = controlType == "listbox";
                    param.IsMultiSelect = controlJson.IsMultiSelect;
                    if (controlJson.Choices != null && controlJson.Choices.Count > 0)
                        param.ValidateSetChoices = new List<string>(controlJson.Choices);
                    
                    // Map dynamic properties with fallback to Properties dictionary
                    param.IsDynamic = controlJson.IsDynamic;
                    if (!param.IsDynamic)
                    {
                         var isDyn = GetProp("IsDynamic");
                         if (isDyn is bool b) param.IsDynamic = b;
                         else if (isDyn is string s && bool.TryParse(s, out bool bp)) param.IsDynamic = bp;
                    }

                    param.DataSourceScriptBlock = controlJson.DataSourceScriptBlock ?? GetProp("DataSourceScriptBlock")?.ToString();
                    
                    if (controlJson.DataSourceDependsOn != null)
                        param.DataSourceDependsOn = new List<string>(controlJson.DataSourceDependsOn);
                    else
                    {
                        var depends = GetProp("DataSourceDependsOn");
                        if (depends is IEnumerable<object> objList)
                            param.DataSourceDependsOn = objList.Select(x => x.ToString()).ToList();
                        else if (depends is System.Collections.IEnumerable enumerable)
                        {
                            var stringList = new List<string>();
                            foreach (var item in enumerable)
                            {
                                if (item != null) stringList.Add(item.ToString());
                            }
                            param.DataSourceDependsOn = stringList;
                        }
                    }
                    break;
                case "numeric":
                    param.ParameterType = typeof(double);
                    param.IsNumeric = true;
                    param.NumericMinimum = controlJson.Minimum;
                    param.NumericMaximum = controlJson.Maximum;
                    // Default to 1 if Step is 0 or not provided
                    param.NumericStep = controlJson.Step > 0 ? controlJson.Step : 1;
                    break;
                case "date":
                    param.ParameterType = typeof(DateTime);
                    param.IsDate = true;
                    break;
                case "filepath":
                    param.ParameterType = typeof(string);
                    param.PathType = PathSelectorType.File;
                    param.PathFilter = controlJson.Filter;
                    break;
                case "folderpath":
                    param.ParameterType = typeof(string);
                    param.PathType = PathSelectorType.Folder;
                    break;
                case "optiongroup":
                    param.ParameterType = typeof(string);
                    param.IsOptionGroup = true;
                    if (controlJson.Choices != null && controlJson.Choices.Count > 0)
                        param.ValidateSetChoices = new List<string>(controlJson.Choices);
                    break;
                default:
                    param.ParameterType = typeof(string);
                    break;
            }

            return param;
        }
    }
}
