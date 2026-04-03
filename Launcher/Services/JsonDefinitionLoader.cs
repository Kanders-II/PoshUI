// Copyright (c) 2025 Kanders-II. All rights reserved.
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
                branding.Theme = definition.Branding.Theme ?? "Auto";
                branding.ThemeOverrides = definition.Branding.ThemeOverrides;
                branding.ThemeOverridesLight = definition.Branding.ThemeOverridesLight;
                branding.ThemeOverridesDark = definition.Branding.ThemeOverridesDark;
                branding.DisableAnimations = definition.Branding.DisableAnimations;
                branding.OriginalScriptName = definition.Branding.OriginalScriptName;
                branding.OriginalScriptPath = definition.Branding.OriginalScriptPath;
                branding.Navigation = definition.Branding.Navigation;
                branding.GridColumns = definition.Branding.GridColumns;
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
                string pageType;
                if (stepType == "Dashboard" || stepType == "CardGrid")
                {
                    pageType = "CardGrid";
                }
                else if (stepType == "Freeform")
                {
                    // Preserve Freeform identity so the ViewModel can detect Freeform mode
                    // (no wizard step numbers, no Previous/Next/Finish buttons, free navigation)
                    pageType = "Freeform";
                }
                else if (stepType == "Workflow")
                {
                    pageType = "Workflow";
                }
                else
                {
                    pageType = "GenericForm";
                }
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

                    // Map properties from Properties dictionary if present
                    // (UICardJson.Properties is Dictionary<string,object> for reliable deserialization)
                    if (cardJson.Properties != null && cardJson.Properties.Count > 0)
                    {
                        var props = cardJson.Properties;
                        // Helper to get a string value from the dictionary
                        Func<string, string> GetStr = (key) =>
                            props.TryGetValue(key, out var v) && v != null ? v.ToString() : null;
                        Func<string, object> GetObj = (key) =>
                            props.TryGetValue(key, out var v) ? v : null;
                        Func<string, bool> GetBool = (key) =>
                        {
                            if (!props.TryGetValue(key, out var v) || v == null) return false;
                            if (v is bool b) return b;
                            bool.TryParse(v.ToString(), out var result);
                            return result;
                        };

                        // Common
                        var pCardTitle = GetStr("CardTitle");
                        if (!string.IsNullOrEmpty(pCardTitle)) cardControl["CardTitle"] = pCardTitle;
                        var pCardDesc = GetStr("CardDescription");
                        if (!string.IsNullOrEmpty(pCardDesc)) cardControl["CardDescription"] = pCardDesc;
                        var pCategory = GetStr("Category");
                        if (!string.IsNullOrEmpty(pCategory)) cardControl["Category"] = pCategory;
                        var pIcon = GetStr("Icon");
                        if (!string.IsNullOrEmpty(pIcon)) cardControl["Icon"] = pIcon;
                        var pIconPath = GetStr("IconPath");
                        if (!string.IsNullOrEmpty(pIconPath)) cardControl["IconPath"] = pIconPath;

                        // MetricCard
                        var pValue = GetObj("Value");
                        if (pValue != null) cardControl["Value"] = pValue;
                        var pUnit = GetStr("Unit");
                        if (!string.IsNullOrEmpty(pUnit)) cardControl["Unit"] = pUnit;
                        var pFormat = GetStr("Format");
                        if (!string.IsNullOrEmpty(pFormat)) cardControl["Format"] = pFormat;
                        var pTrend = GetStr("Trend");
                        if (!string.IsNullOrEmpty(pTrend)) cardControl["Trend"] = pTrend;
                        var pTrendValue = GetObj("TrendValue");
                        if (pTrendValue != null) cardControl["TrendValue"] = pTrendValue;
                        var pTarget = GetObj("Target");
                        if (pTarget != null) cardControl["Target"] = pTarget;
                        var pMinValue = GetObj("MinValue");
                        if (pMinValue != null) cardControl["MinValue"] = pMinValue;
                        var pMaxValue = GetObj("MaxValue");
                        if (pMaxValue != null) cardControl["MaxValue"] = pMaxValue;
                        cardControl["ShowProgressBar"] = GetBool("ShowProgressBar");
                        cardControl["ShowTrend"] = GetBool("ShowTrend");
                        cardControl["ShowTarget"] = GetBool("ShowTarget");
                        cardControl["ShowGauge"] = GetBool("ShowGauge");
                        cardControl["AutoSparkline"] = GetBool("AutoSparkline");
                        var pSparkline = GetObj("SparklineData");
                        if (pSparkline != null) cardControl["SparklineData"] = pSparkline;
                        var pRefreshScript = GetStr("RefreshScript");
                        if (!string.IsNullOrEmpty(pRefreshScript)) cardControl["RefreshScript"] = pRefreshScript;

                        // GraphCard
                        var pChartType = GetStr("ChartType");
                        if (!string.IsNullOrEmpty(pChartType)) cardControl["ChartType"] = pChartType;
                        var pData = GetObj("Data");
                        if (pData != null) cardControl["Data"] = pData;
                        cardControl["ShowLegend"] = GetBool("ShowLegend");
                        cardControl["ShowTooltip"] = GetBool("ShowTooltip");

                        // InfoCard - Content (try Content first, then CardContent)
                        var pContent = GetStr("Content");
                        var pCardContent = GetStr("CardContent");
                        if (!string.IsNullOrEmpty(pContent)) cardControl["Content"] = pContent;
                        else if (!string.IsNullOrEmpty(pCardContent)) cardControl["Content"] = pCardContent;

                        var pImageSource = GetStr("ImageSource");
                        if (!string.IsNullOrEmpty(pImageSource)) cardControl["ImageSource"] = pImageSource;
                        var pWidth = GetObj("Width");
                        if (pWidth != null) cardControl["Width"] = pWidth;
                        var pHeight = GetObj("Height");
                        if (pHeight != null) cardControl["Height"] = pHeight;
                        var pBgColor = GetStr("BackgroundColor");
                        if (!string.IsNullOrEmpty(pBgColor)) cardControl["BackgroundColor"] = pBgColor;
                        var pTextColor = GetStr("TextColor");
                        if (!string.IsNullOrEmpty(pTextColor)) cardControl["TextColor"] = pTextColor;
                        var pBorderColor = GetStr("BorderColor");
                        if (!string.IsNullOrEmpty(pBorderColor)) cardControl["BorderColor"] = pBorderColor;
                        var pLinkUrl = GetStr("LinkUrl");
                        if (!string.IsNullOrEmpty(pLinkUrl)) cardControl["LinkUrl"] = pLinkUrl;
                        var pLinkText = GetStr("LinkText");
                        if (!string.IsNullOrEmpty(pLinkText)) cardControl["LinkText"] = pLinkText;
                        var pCardStyle = GetStr("CardStyle");
                        if (!string.IsNullOrEmpty(pCardStyle)) cardControl["CardStyle"] = pCardStyle;
                        var pSubtitle = GetStr("Subtitle");
                        if (!string.IsNullOrEmpty(pSubtitle)) cardControl["Subtitle"] = pSubtitle;
                        cardControl["Collapsible"] = GetBool("Collapsible");
                        cardControl["IsExpanded"] = GetBool("IsExpanded");
                        var pAccentColor = GetStr("AccentColor");
                        if (!string.IsNullOrEmpty(pAccentColor)) cardControl["AccentColor"] = pAccentColor;
                        var pButtonText = GetStr("ButtonText");
                        if (!string.IsNullOrEmpty(pButtonText)) cardControl["ButtonText"] = pButtonText;

                        // ScriptCard
                        var pScriptBlock = GetStr("ScriptBlock");
                        if (!string.IsNullOrEmpty(pScriptBlock)) cardControl["ScriptBlock"] = pScriptBlock;
                        var pScriptPath = GetStr("ScriptPath");
                        if (!string.IsNullOrEmpty(pScriptPath)) cardControl["ScriptPath"] = pScriptPath;
                        var pScriptSource = GetStr("ScriptSource");
                        if (!string.IsNullOrEmpty(pScriptSource)) cardControl["ScriptSource"] = pScriptSource;
                        var pParamControls = GetStr("ParameterControls");
                        if (!string.IsNullOrEmpty(pParamControls)) cardControl["ParameterControls"] = pParamControls;
                        var pDefaultParams = GetStr("DefaultParameters");
                        if (!string.IsNullOrEmpty(pDefaultParams)) cardControl["DefaultParameters"] = pDefaultParams;
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
                case "button":
                    param.ParameterType = typeof(string);
                    param.IsButton = true;
                    param.ButtonStyle = GetProp("Style")?.ToString() ?? "Primary";
                    param.ButtonIcon = GetProp("Icon")?.ToString();
                    param.ButtonIconPath = GetProp("IconPath")?.ToString();
                    param.ButtonCategory = !string.IsNullOrEmpty(controlJson.Category) ? controlJson.Category : GetProp("Category")?.ToString();
                    param.OnClickScript = GetProp("OnClick")?.ToString();
                    param.FlyoutScript = GetProp("FlyoutScript")?.ToString();
                    param.FlyoutTitle = GetProp("FlyoutTitle")?.ToString();
                    var showMd = GetProp("FlyoutShowMarkdown");
                    if (showMd is bool mdBool) param.FlyoutShowMarkdown = mdBool;
                    else if (showMd is string mdStr && bool.TryParse(mdStr, out bool mdParsed)) param.FlyoutShowMarkdown = mdParsed;
                    break;
                case "label":
                    param.ParameterType = typeof(string);
                    param.IsLabel = true;
                    param.IsPlaceholder = true; // Labels don't collect data
                    // Set DefaultValue from Text property so {Binding Value} displays the text
                    var labelText = GetProp("Text")?.ToString();
                    if (!string.IsNullOrEmpty(labelText))
                        param.DefaultValue = labelText;
                    else if (!string.IsNullOrEmpty(controlJson.Label))
                        param.DefaultValue = controlJson.Label;
                    var fs = GetProp("FontSize");
                    if (fs != null && double.TryParse(fs.ToString(), out double fontSize)) param.FontSize = fontSize;
                    param.FontWeight = GetProp("FontWeight")?.ToString();
                    param.Foreground = GetProp("Foreground")?.ToString();
                    break;
                case "image":
                    param.ParameterType = typeof(string);
                    param.IsImage = true;
                    param.IsPlaceholder = true; // Images don't collect data
                    param.ImagePath = GetProp("Path")?.ToString();
                    param.ImageStretch = GetProp("Stretch")?.ToString() ?? "Uniform";
                    var imgH = GetProp("Height");
                    if (imgH != null && double.TryParse(imgH.ToString(), out double imgHeight)) param.ControlHeight = imgHeight;
                    break;
                case "slider":
                    param.ParameterType = typeof(double);
                    param.IsSlider = true;
                    param.IsNumeric = true; // Enable NumericValue binding
                    param.NumericAllowDecimal = true;
                    var sMin = GetProp("Min");
                    if (sMin != null && double.TryParse(sMin.ToString(), out double slMin)) { param.SliderMin = slMin; param.NumericMinimum = slMin; }
                    var sMax = GetProp("Max");
                    if (sMax != null && double.TryParse(sMax.ToString(), out double slMax)) { param.SliderMax = slMax; param.NumericMaximum = slMax; }
                    var sStep = GetProp("Step");
                    if (sStep != null && double.TryParse(sStep.ToString(), out double slStep)) { param.SliderStep = slStep; param.NumericStep = slStep; }
                    break;
                case "progressbar":
                    param.ParameterType = typeof(double);
                    param.IsProgressBar = true;
                    param.IsNumeric = true; // Enable NumericValue binding for XAML
                    param.IsPlaceholder = true; // ProgressBars don't collect data
                    param.NumericMinimum = 0;
                    var pMax = GetProp("Max");
                    if (pMax != null && double.TryParse(pMax.ToString(), out double prMax)) { param.ProgressMax = prMax; param.NumericMaximum = prMax; }
                    else { param.ProgressMax = 100; param.NumericMaximum = 100; }
                    var pIndet = GetProp("Indeterminate");
                    if (pIndet is bool indBool) param.ProgressIndeterminate = indBool;
                    else if (pIndet is string indStr && bool.TryParse(indStr, out bool indParsed)) param.ProgressIndeterminate = indParsed;
                    break;
                case "tabcontrol":
                    // TabControl is not a parameter — it defines tab names for layout grouping.
                    // Read Tabs from Properties, store them on ParameterInfo for the ViewModel to consume.
                    param.ParameterType = typeof(string);
                    param.IsPlaceholder = true;
                    var tabsProp = GetProp("Tabs");
                    if (tabsProp is IEnumerable<object> tabList)
                        param.Tabs = tabList.Select(x => x.ToString()).ToList();
                    else if (tabsProp is System.Collections.IEnumerable tabEnum)
                    {
                        var tabs = new List<string>();
                        foreach (var item in tabEnum)
                            if (item != null) tabs.Add(item.ToString());
                        param.Tabs = tabs;
                    }
                    return param;
                // Freeform card types that should be handled as dashboard cards (routed to Controls list)
                case "metriccard":
                case "chartcard":
                case "tablecard":
                case "scriptcard":
                    // These are handled by MapBannersAndCards, return null to skip as parameter
                    return null;
                default:
                    param.ParameterType = typeof(string);
                    break;
            }

            // Grid layout properties (Freeform) - apply to all control types
            var rowProp = GetProp("Row");
            if (rowProp != null && int.TryParse(rowProp.ToString(), out int gridRow)) param.GridRow = gridRow;
            var colProp = GetProp("Column");
            if (colProp != null && int.TryParse(colProp.ToString(), out int gridCol)) param.GridColumn = gridCol;
            var colSpanProp = GetProp("ColumnSpan");
            if (colSpanProp != null && int.TryParse(colSpanProp.ToString(), out int colSpan)) param.ColumnSpan = colSpan;
            var rowSpanProp = GetProp("RowSpan");
            if (rowSpanProp != null && int.TryParse(rowSpanProp.ToString(), out int rowSpan)) param.RowSpan = rowSpan;

            // Settings UI category grouping - read from top-level first, then Properties dict
            if (!string.IsNullOrEmpty(controlJson.Category))
                param.Category = controlJson.Category;
            else
            {
                var categoryProp = GetProp("Category");
                if (categoryProp != null) param.Category = categoryProp.ToString();
            }
            if (!string.IsNullOrEmpty(controlJson.HelpText))
                param.HelpText = controlJson.HelpText;
            else
            {
                var helpTextProp = GetProp("HelpText");
                if (helpTextProp != null) param.HelpText = helpTextProp.ToString();
            }
            if (string.IsNullOrEmpty(param.HelpText))
            {
                var descProp = GetProp("Description");
                if (descProp != null) param.HelpText = descProp.ToString();
            }

            // Tab assignment - which tab this control belongs to
            var tabProp = GetProp("Tab");
            if (tabProp != null) param.Tab = tabProp.ToString();

            // Control-level icon (displayed next to label)
            var iconPathProp = GetProp("IconPath");
            if (iconPathProp != null) param.IconPath = iconPathProp.ToString();
            return param;
        }
    }
}
