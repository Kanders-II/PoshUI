// Copyright (c) 2025 A Solution IT LLC. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Launcher.Models
{
    [DataContract]
    public class UIDefinitionJson
    {
        [DataMember(Name = "Title")]
        public string Title { get; set; }

        [DataMember(Name = "Description")]
        public string Description { get; set; }

        [DataMember(Name = "Template")]
        public string Template { get; set; }

        [DataMember(Name = "Theme")]
        public string Theme { get; set; }

        [DataMember(Name = "AllowCancel")]
        public bool AllowCancel { get; set; } = true;

        [DataMember(Name = "GridColumns")]
        public int GridColumns { get; set; } = 2;

        [DataMember(Name = "Branding")]
        public UIBrandingJson Branding { get; set; }

        [DataMember(Name = "Steps")]
        public List<UIStepJson> Steps { get; set; } = new List<UIStepJson>();

        [DataMember(Name = "ScriptBody")]
        public string ScriptBody { get; set; }
    }

    [DataContract]
    public class UIBrandingJson
    {
        [DataMember(Name = "WindowTitleText")]
        public string WindowTitleText { get; set; }

        [DataMember(Name = "WindowTitleIcon")]
        public string WindowTitleIcon { get; set; }

        [DataMember(Name = "SidebarHeaderText")]
        public string SidebarHeaderText { get; set; }

        [DataMember(Name = "SidebarHeaderIcon")]
        public string SidebarHeaderIcon { get; set; }

        [DataMember(Name = "SidebarHeaderIconPath")]
        public string SidebarHeaderIconPath { get; set; }

        [DataMember(Name = "SidebarHeaderIconOrientation")]
        public string SidebarHeaderIconOrientation { get; set; }

        [DataMember(Name = "Theme")]
        public string Theme { get; set; }

        [DataMember(Name = "OriginalScriptName")]
        public string OriginalScriptName { get; set; }

        [DataMember(Name = "OriginalScriptPath")]
        public string OriginalScriptPath { get; set; }
    }

    [DataContract]
    public class UIStepJson
    {
        [DataMember(Name = "Name")]
        public string Name { get; set; }

        [DataMember(Name = "Title")]
        public string Title { get; set; }

        [DataMember(Name = "Description")]
        public string Description { get; set; }

        [DataMember(Name = "Order")]
        public int Order { get; set; }

        [DataMember(Name = "Type")]
        public string Type { get; set; }

        [DataMember(Name = "Icon")]
        public string Icon { get; set; }

        [DataMember(Name = "Controls")]
        public List<UIControlJson> Controls { get; set; }

        [DataMember(Name = "Banner")]
        public UIBannerJson Banner { get; set; }

        [DataMember(Name = "Banners")]
        public List<UIBannerJson> Banners { get; set; }

        [DataMember(Name = "Cards")]
        public List<UICardJson> Cards { get; set; }
    }

    [DataContract]
    public class UIControlJson
    {
        [DataMember(Name = "Name")]
        public string Name { get; set; }

        [DataMember(Name = "Type")]
        public string Type { get; set; }

        [DataMember(Name = "Label")]
        public string Label { get; set; }

        [DataMember(Name = "Default")]
        public object Default { get; set; }

        [DataMember(Name = "Mandatory")]
        public bool Mandatory { get; set; }

        [DataMember(Name = "HelpText")]
        public string HelpText { get; set; }

        [DataMember(Name = "Width")]
        public int Width { get; set; }

        [DataMember(Name = "ValidationPattern")]
        public string ValidationPattern { get; set; }

        [DataMember(Name = "ValidationScript")]
        public string ValidationScript { get; set; }

        [DataMember(Name = "Choices")]
        public List<string> Choices { get; set; }

        [DataMember(Name = "Multiline")]
        public bool Multiline { get; set; }

        [DataMember(Name = "MaxLength")]
        public int MaxLength { get; set; }

        [DataMember(Name = "Placeholder")]
        public string Placeholder { get; set; }

        [DataMember(Name = "ShowRevealButton")]
        public bool ShowRevealButton { get; set; }

        [DataMember(Name = "Minimum")]
        public double Minimum { get; set; }

        [DataMember(Name = "Maximum")]
        public double Maximum { get; set; }

        [DataMember(Name = "Step")]
        public double Step { get; set; } = 1;

        [DataMember(Name = "Filter")]
        public string Filter { get; set; }

        [DataMember(Name = "IsDynamic")]
        public bool IsDynamic { get; set; }

        [DataMember(Name = "DataSourceScriptBlock")]
        public string DataSourceScriptBlock { get; set; }

        [DataMember(Name = "DataSourceDependsOn")]
        public List<string> DataSourceDependsOn { get; set; }

        [DataMember(Name = "IsMultiSelect")]
        public bool IsMultiSelect { get; set; }

        [DataMember(Name = "Properties")]
        public Dictionary<string, object> Properties { get; set; }
    }

    [DataContract]
    public class UIBannerJson
    {
        // Core Properties
        [DataMember(Name = "Title")]
        public string Title { get; set; }

        [DataMember(Name = "Subtitle")]
        public string Subtitle { get; set; }

        [DataMember(Name = "Description")]
        public string Description { get; set; }

        [DataMember(Name = "DescriptionText")]
        public string DescriptionText { get; set; }

        [DataMember(Name = "Icon")]
        public string Icon { get; set; }

        [DataMember(Name = "Type")]
        public string Type { get; set; }

        [DataMember(Name = "Category")]
        public string Category { get; set; }

        // Layout & Sizing
        [DataMember(Name = "Height")]
        public int Height { get; set; }

        [DataMember(Name = "Width")]
        public int Width { get; set; }

        [DataMember(Name = "MinHeight")]
        public int MinHeight { get; set; }

        [DataMember(Name = "MaxHeight")]
        public int MaxHeight { get; set; }

        [DataMember(Name = "Layout")]
        public string Layout { get; set; }

        [DataMember(Name = "ContentAlignment")]
        public string ContentAlignment { get; set; }

        [DataMember(Name = "VerticalAlignment")]
        public string VerticalAlignment { get; set; }

        [DataMember(Name = "Padding")]
        public string Padding { get; set; }

        [DataMember(Name = "CornerRadius")]
        public int CornerRadius { get; set; }

        [DataMember(Name = "FullWidth")]
        public bool FullWidth { get; set; }

        // Typography
        [DataMember(Name = "TitleFontSize")]
        public string TitleFontSize { get; set; }

        [DataMember(Name = "SubtitleFontSize")]
        public string SubtitleFontSize { get; set; }

        [DataMember(Name = "DescriptionFontSize")]
        public string DescriptionFontSize { get; set; }

        [DataMember(Name = "TitleFontWeight")]
        public string TitleFontWeight { get; set; }

        [DataMember(Name = "SubtitleFontWeight")]
        public string SubtitleFontWeight { get; set; }

        [DataMember(Name = "DescriptionFontWeight")]
        public string DescriptionFontWeight { get; set; }

        [DataMember(Name = "FontFamily")]
        public string FontFamily { get; set; }

        [DataMember(Name = "TitleColor")]
        public string TitleColor { get; set; }

        [DataMember(Name = "SubtitleColor")]
        public string SubtitleColor { get; set; }

        [DataMember(Name = "DescriptionColor")]
        public string DescriptionColor { get; set; }

        [DataMember(Name = "TitleAllCaps")]
        public bool TitleAllCaps { get; set; }

        [DataMember(Name = "TitleLetterSpacing")]
        public double TitleLetterSpacing { get; set; }

        [DataMember(Name = "LineHeight")]
        public double LineHeight { get; set; }

        // Background & Visual Effects
        [DataMember(Name = "BackgroundColor")]
        public string BackgroundColor { get; set; }

        [DataMember(Name = "BackgroundImagePath")]
        public string BackgroundImagePath { get; set; }

        [DataMember(Name = "BackgroundImageOpacity")]
        public double BackgroundImageOpacity { get; set; }

        [DataMember(Name = "BackgroundImageStretch")]
        public string BackgroundImageStretch { get; set; }

        [DataMember(Name = "GradientStart")]
        public string GradientStart { get; set; }

        [DataMember(Name = "GradientEnd")]
        public string GradientEnd { get; set; }

        [DataMember(Name = "GradientAngle")]
        public double GradientAngle { get; set; }

        [DataMember(Name = "BorderColor")]
        public string BorderColor { get; set; }

        [DataMember(Name = "BorderThickness")]
        public int BorderThickness { get; set; }

        [DataMember(Name = "ShadowIntensity")]
        public string ShadowIntensity { get; set; }

        [DataMember(Name = "Opacity")]
        public double Opacity { get; set; }

        // Icon & Image Options
        [DataMember(Name = "IconPath")]
        public string IconPath { get; set; }

        [DataMember(Name = "IconSize")]
        public int IconSize { get; set; }

        [DataMember(Name = "IconPosition")]
        public string IconPosition { get; set; }

        [DataMember(Name = "IconColor")]
        public string IconColor { get; set; }

        [DataMember(Name = "IconAnimation")]
        public string IconAnimation { get; set; }

        // Overlay Image
        [DataMember(Name = "OverlayImagePath")]
        public string OverlayImagePath { get; set; }

        [DataMember(Name = "OverlayImageOpacity")]
        public double OverlayImageOpacity { get; set; }

        [DataMember(Name = "OverlayPosition")]
        public string OverlayPosition { get; set; }

        [DataMember(Name = "OverlayImageSize")]
        public int OverlayImageSize { get; set; }

        // Carousel Properties
        [DataMember(Name = "CarouselSlidesJson")]
        public string CarouselSlidesJson { get; set; }

        [DataMember(Name = "AutoRotate")]
        public bool AutoRotate { get; set; }

        [DataMember(Name = "RotateInterval")]
        public int RotateInterval { get; set; }

        [DataMember(Name = "NavigationStyle")]
        public string NavigationStyle { get; set; }

        // Interactive Properties
        [DataMember(Name = "Clickable")]
        public bool Clickable { get; set; }

        [DataMember(Name = "LinkUrl")]
        public string LinkUrl { get; set; }

        [DataMember(Name = "ButtonText")]
        public string ButtonText { get; set; }

        [DataMember(Name = "ButtonColor")]
        public string ButtonColor { get; set; }
    }

    [DataContract]
    public class UICardJson
    {
        [DataMember(Name = "Type")]
        public string Type { get; set; }

        [DataMember(Name = "CardType")]
        public string CardType { get; set; }

        [DataMember(Name = "Name")]
        public string Name { get; set; }

        [DataMember(Name = "Title")]
        public string Title { get; set; }

        [DataMember(Name = "Subtitle")]
        public string Subtitle { get; set; }

        [DataMember(Name = "Description")]
        public string Description { get; set; }

        [DataMember(Name = "Icon")]
        public string Icon { get; set; }

        [DataMember(Name = "Category")]
        public string Category { get; set; }

        [DataMember(Name = "ScriptPath")]
        public string ScriptPath { get; set; }

        [DataMember(Name = "ScriptBlock")]
        public string ScriptBlock { get; set; }

        // InfoCard properties (serialized at top level by Wizard)
        [DataMember(Name = "Content")]
        public string Content { get; set; }

        [DataMember(Name = "IconPath")]
        public string IconPath { get; set; }

        [DataMember(Name = "ImagePath")]
        public string ImagePath { get; set; }

        [DataMember(Name = "ImageOpacity")]
        public double ImageOpacity { get; set; }

        [DataMember(Name = "LinkUrl")]
        public string LinkUrl { get; set; }

        [DataMember(Name = "LinkText")]
        public string LinkText { get; set; }

        [DataMember(Name = "BackgroundColor")]
        public string BackgroundColor { get; set; }

        [DataMember(Name = "TitleColor")]
        public string TitleColor { get; set; }

        [DataMember(Name = "ContentColor")]
        public string ContentColor { get; set; }

        [DataMember(Name = "CornerRadius")]
        public int CornerRadius { get; set; }

        [DataMember(Name = "GradientStart")]
        public string GradientStart { get; set; }

        [DataMember(Name = "GradientEnd")]
        public string GradientEnd { get; set; }

        [DataMember(Name = "Width")]
        public object Width { get; set; }

        [DataMember(Name = "Height")]
        public object Height { get; set; }

        // Note: Properties dictionary may not deserialize correctly with DataContractJsonSerializer
        // Key card properties are stored in the Properties sub-object from PS serialization
        [DataMember(Name = "Properties")]
        public UICardPropertiesJson Properties { get; set; }
    }

    /// <summary>
    /// Strongly-typed card properties to ensure proper deserialization.
    /// </summary>
    [DataContract]
    public class UICardPropertiesJson
    {
        // Common properties
        [DataMember(Name = "Type")]
        public string Type { get; set; }

        [DataMember(Name = "CardTitle")]
        public string CardTitle { get; set; }

        [DataMember(Name = "CardDescription")]
        public string CardDescription { get; set; }

        [DataMember(Name = "Category")]
        public string Category { get; set; }

        [DataMember(Name = "Icon")]
        public string Icon { get; set; }

        // MetricCard properties
        [DataMember(Name = "Value")]
        public object Value { get; set; }

        [DataMember(Name = "Unit")]
        public string Unit { get; set; }

        [DataMember(Name = "Format")]
        public string Format { get; set; }

        [DataMember(Name = "Trend")]
        public string Trend { get; set; }

        [DataMember(Name = "TrendValue")]
        public object TrendValue { get; set; }

        [DataMember(Name = "Target")]
        public object Target { get; set; }

        [DataMember(Name = "MinValue")]
        public object MinValue { get; set; }

        [DataMember(Name = "MaxValue")]
        public object MaxValue { get; set; }

        [DataMember(Name = "ShowProgressBar")]
        public bool ShowProgressBar { get; set; }

        [DataMember(Name = "ShowTrend")]
        public bool ShowTrend { get; set; }

        [DataMember(Name = "ShowTarget")]
        public bool ShowTarget { get; set; }

        [DataMember(Name = "RefreshScript")]
        public string RefreshScript { get; set; }

        // GraphCard properties
        [DataMember(Name = "ChartType")]
        public string ChartType { get; set; }

        [DataMember(Name = "Data")]
        public object Data { get; set; }

        [DataMember(Name = "ShowLegend")]
        public bool ShowLegend { get; set; }

        [DataMember(Name = "ShowTooltip")]
        public bool ShowTooltip { get; set; }

        // InfoCard properties
        [DataMember(Name = "Content")]
        public string Content { get; set; }

        [DataMember(Name = "CardContent")]
        public string CardContent { get; set; }

        [DataMember(Name = "ImageSource")]
        public string ImageSource { get; set; }

        [DataMember(Name = "Width")]
        public object Width { get; set; }

        [DataMember(Name = "Height")]
        public object Height { get; set; }

        [DataMember(Name = "BackgroundColor")]
        public string BackgroundColor { get; set; }

        [DataMember(Name = "TextColor")]
        public string TextColor { get; set; }

        [DataMember(Name = "BorderColor")]
        public string BorderColor { get; set; }

        [DataMember(Name = "LinkUrl")]
        public string LinkUrl { get; set; }

        // ScriptCard properties
        [DataMember(Name = "ScriptBlock")]
        public string ScriptBlock { get; set; }

        [DataMember(Name = "ScriptPath")]
        public string ScriptPath { get; set; }

        [DataMember(Name = "ScriptSource")]
        public string ScriptSource { get; set; }

        [DataMember(Name = "ParameterControls")]
        public string ParameterControls { get; set; }

        [DataMember(Name = "DefaultParameters")]
        public string DefaultParameters { get; set; }
    }
}
