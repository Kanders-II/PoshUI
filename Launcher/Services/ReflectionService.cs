// Copyright (c) 2025 A Solution IT LLC. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Management.Automation.Runspaces;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Text;
using Launcher.Attributes;

namespace Launcher.Services
{
    // Enum to define the type of path selector
    public enum PathSelectorType
    {
        None,  // Default, no selector
        File,  // Use OpenFileDialog
        Folder // Use folder selection logic
    }

    public class ParameterInfo
    {
        public string Name { get; set; }
        public Type ParameterType { get; set; }
        public object DefaultValue { get; set; }
        public bool IsMandatory { get; set; }
        public string Label { get; set; }
        public List<string> ValidateSetChoices { get; set; }
        public string ValidationPattern { get; set; }
        public string ValidationScript { get; set; }
        public double? ControlWidth { get; set; }
        public bool IsSwitch { get; set; }
        public PathSelectorType PathType { get; set; } = PathSelectorType.None; // Add PathType property
        public string PathFilter { get; set; } // File filter (e.g., "*.ps1" or "*.log;*.txt")
        public string DialogTitle { get; set; } // Dialog title for path selectors
        public bool IsListBox { get; set; } // Display as ListBox instead of ComboBox
        public bool IsMultiSelect { get; set; } // Allow multiple selection in ListBox
        // Card properties
        public bool IsCard { get; set; }  // True when WizardCard attribute is present
        public string CardTitle { get; set; }
        public string CardContent { get; set; }
        public string CardImagePath { get; set; }
        public string CardLinkUrl { get; set; }
        public string CardLinkText { get; set; }
        public string CardIconPath { get; set; }
        public string CardIcon { get; set; }  // Segoe MDL2 glyph
        public double? CardImageOpacity { get; set; }
        public string CardBackgroundColor { get; set; }
        public string CardTitleColor { get; set; }
        public string CardContentColor { get; set; }
        public int? CardCornerRadius { get; set; }
        public string CardGradientStart { get; set; }
        public string CardGradientEnd { get; set; }
        // Placeholder marker - true for Welcome/Summary/Branding placeholders
        public bool IsPlaceholder { get; set; }
        public bool IsNumeric { get; set; }
        public double? NumericMinimum { get; set; }
        public double? NumericMaximum { get; set; }
        public double? NumericStep { get; set; }
        public bool NumericAllowDecimal { get; set; }
        public string NumericFormat { get; set; } // Display format (e.g., "C2", "P0", "N2")
        public bool IsDate { get; set; }
        public DateTime? DateMinimum { get; set; }
        public DateTime? DateMaximum { get; set; }
        public string DateFormat { get; set; }
        public bool IsOptionGroup { get; set; }
        public bool OptionGroupHorizontalLayout { get; set; }
        public bool IsMultiLineText { get; set; }
        public int? MultiLineRows { get; set; }
        
        // Enhanced control properties (optional explicit attributes)
        public int? TextBoxMaxLength { get; set; }
        public string TextBoxPlaceholder { get; set; }
        public bool? PasswordShowReveal { get; set; }
        public int? PasswordMinLength { get; set; }
        public string CheckBoxCheckedLabel { get; set; }
        public string CheckBoxUncheckedLabel { get; set; }
        
        // Dynamic parameter properties
        public bool IsDynamic { get; set; }
        public string DataSourceScriptBlock { get; set; }  // Script block as string
        public string DataSourceCsvPath { get; set; }
        public string DataSourceCsvColumn { get; set; }
        public string DataSourceCsvFilter { get; set; }  // Filter script as string
        public List<string> DataSourceDependsOn { get; set; }
        public bool DataSourceAsync { get; set; }
        public bool DataSourceShowRefresh { get; set; }

        // Script cards JSON data (from UIScriptCards attribute)
        public string ScriptCardsJson { get; set; }
        
        // Banner JSON data (from UIBanner attribute)
        public string BannerJson { get; set; }
        
        // Workflow tasks JSON data (from WizardWorkflowTasks attribute)
        public string WorkflowTasksJson { get; set; }
    }

    public class WizardStep
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public int Order { get; set; }
        public string PageType { get; set; } = "GenericForm"; // Default to GenericForm
        public List<ParameterInfo> Parameters { get; set; } = new List<ParameterInfo>();

        // New properties for structured welcome page
        public string IntroductionText { get; set; }
        public string Prerequisites { get; set; }
        public string SupportLink { get; set; }
        public string IconPath { get; set; }
        public string IconGlyph { get; set; }
        
        // Controls added via Add-WizardBanner, Add-WizardVisualizationCard, etc.
        public System.Collections.IList Controls { get; set; }
    }

    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public class WizardStepAttribute : Attribute
    {
        // Positional arguments remain mandatory for core step definition
        public string Title { get; }
        public int Order { get; }

        // Optional named arguments 
        public string Description { get; set; }
        public string PageType { get; set; } // Allow specifying page type
        public string IntroductionText { get; set; }
        public string Prerequisites { get; set; }
        public string SupportLink { get; set; }
        public string IconPath { get; set; }
        public string IconGlyph { get; set; }

        // Constructor uses positional arguments
        public WizardStepAttribute(string title, int order)
        {
            Title = title;
            Order = order;
            // Defaults can be set here if desired, but better handled in ReflectionService parsing
            PageType = "GenericForm"; // Explicitly default here or rely on WizardStep class default
        }
    }

    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public class WizardCardAttribute : Attribute
    {
        public string Title { get; }
        public string Content { get; }

        public WizardCardAttribute(string title, string content)
        {
            Title = title;
            Content = content;
        }
    }

    // NEW: Branding model and attribute for global UI customization
    public class WizardBranding
    {
        public string WindowTitleText { get; set; }
        public string WindowTitleIcon { get; set; }
        public string SidebarHeaderText { get; set; }
        public string SidebarHeaderIcon { get; set; }
        public string SidebarHeaderIconOrientation { get; set; }
        public string Theme { get; set; }  // Light, Dark, or Auto
        public string OriginalScriptName { get; set; }  // For Module API: original calling script name for log files
        public string OriginalScriptPath { get; set; }  // For Module API: original calling script directory for per-execution logs
        public bool SkipToWorkflow { get; set; }  // For resume: skip wizard pages and go directly to workflow
        public string LogPath { get; set; }  // Custom log file path
        public string PreviousLogFilePath { get; set; }  // For resume: previous log file to restore content from
    }

    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public class WizardBrandingAttribute : Attribute
    {
        public string WindowTitleText { get; set; }
        public string WindowTitleIcon { get; set; }
        public string SidebarHeaderText { get; set; }
        public string SidebarHeaderIcon { get; set; }
        public string SidebarHeaderIconPath { get; set; }  // Alias for SidebarHeaderIcon
        public string SidebarHeaderIconOrientation { get; set; }
        public string Theme { get; set; }  // Light, Dark, or Auto
        public string OriginalScriptName { get; set; }  // For Module API: original calling script name
        public string OriginalScriptPath { get; set; }  // For Module API: original calling script directory
        public string SkipToWorkflow { get; set; }  // For resume: skip wizard pages
        public string LogPath { get; set; }  // Custom log file path
        public string PreviousLogFilePath { get; set; }  // For resume: previous log file
    }

    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public class WizardDropdownAttribute : Attribute
    {
        public string[] Choices { get; }

        public WizardDropdownAttribute(params string[] choices)
        {
            Choices = choices;
        }
    }

    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public class WizardFilePathAttribute : Attribute
    {
        public string Label { get; set; }
        public string Filter { get; set; } = ""; // e.g., "*.txt" or "*.log;*.txt"
        public string DialogTitle { get; set; } = "";
        public WizardFilePathAttribute() { }
    }

    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public class WizardFolderPathAttribute : Attribute
    {
        public string Label { get; set; }
        public string DialogTitle { get; set; } = "";
        public WizardFolderPathAttribute() { }
    }

    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public class WizardDropdownFromCsvAttribute : Attribute
    {
        public string CsvFilePath { get; }
        public string ValueColumn { get; }

        public WizardDropdownFromCsvAttribute(string csvFilePath, string valueColumn)
        {
            CsvFilePath = csvFilePath;
            ValueColumn = valueColumn;
        }
    }

    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public class WizardParameterDetailsAttribute : Attribute
    {
        public string Label { get; set; }
        public double ControlWidth { get; set; } = double.NaN;
    }

    // Define the new attribute for path selection
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public class WizardPathSelectorAttribute : Attribute
    {
        public PathSelectorType Type { get; set; } = PathSelectorType.None;
        public string Filter { get; set; } = ""; // e.g., "*.txt" or "*.log;*.txt"
        public string DialogTitle { get; set; } = "";
        public bool ValidateExists { get; set; } = false;
    }
    
    // Attribute for ListBox control (alternative to ComboBox dropdown)
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public class WizardListBoxAttribute : Attribute
    {
        public bool MultiSelect { get; set; } = false; // Enable multi-select mode
        public int VisibleItems { get; set; } = 5; // Number of visible items (height)
    }

    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public class WizardNumericAttribute : Attribute
    {
        public double Minimum { get; set; } = double.NaN;
        public double Maximum { get; set; } = double.NaN;
        public double Step { get; set; } = double.NaN;
        public bool AllowDecimal { get; set; } = false;
        public string Format { get; set; } = ""; // e.g., "C2" (currency), "P0" (percent), "N2" (number with 2 decimals)
    }

    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public class WizardDateAttribute : Attribute
    {
        public string Minimum { get; set; }
        public string Maximum { get; set; }
        public string Format { get; set; }
    }

    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public class WizardOptionGroupAttribute : Attribute
    {
        public string Orientation { get; set; } = "Vertical";
        public string[] Options { get; }

        public WizardOptionGroupAttribute() { }

        public WizardOptionGroupAttribute(params string[] options)
        {
            Options = options;
        }
    }

    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public class WizardMultiLineAttribute : Attribute
    {
        public int Rows { get; set; } = 4;
    }

    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public class WizardTextBoxAttribute : Attribute
    {
        public int MaxLength { get; set; } = -1;
        public string Placeholder { get; set; } = "";
    }

    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public class WizardPasswordAttribute : Attribute
    {
        public bool ShowRevealButton { get; set; } = true;
        public int MinLength { get; set; } = 0;
        public string ValidationPattern { get; set; } = "";
        public string ValidationScript { get; set; } = "";
    }

    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public class WizardCheckBoxAttribute : Attribute
    {
        public string CheckedLabel { get; set; } = "";
        public string UncheckedLabel { get; set; } = "";
    }

    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public class WizardWorkflowTasksAttribute : Attribute
    {
        public string TasksJson { get; }

        public WizardWorkflowTasksAttribute(string tasksJson)
        {
            TasksJson = tasksJson;
        }
    }


    public class ReflectionService
    {
        public List<WizardStep> LoadWizardStepsFromScript(string scriptPath, out WizardBranding branding)
        {
            branding = null; // default
            if (!File.Exists(scriptPath))
            {
                LoggingService.Error($"Script file not found: {scriptPath}", component: "ReflectionService");
                throw new FileNotFoundException("Script file not found.", scriptPath);
            }

            var steps = new Dictionary<string, WizardStep>();
            string scriptDirectory = Path.GetDirectoryName(scriptPath) ?? Directory.GetCurrentDirectory();
            LoggingService.Info($"Loading script: {scriptPath}", component: "ReflectionService");
            WizardStep currentStep = null; // Keep track of the current step

            try
            {
                string scriptContent = File.ReadAllText(scriptPath);
                ScriptBlockAst scriptBlockAst = Parser.ParseInput(scriptContent, out _, out ParseError[] errors);

                if (errors.Any())
                {
                    string errorMessages = string.Join("\n", errors.Select(e => e.Message));
                    LoggingService.Error($"Error parsing script {scriptPath}:\n{errorMessages}", component: "ReflectionService");
                    throw new InvalidOperationException($"Error parsing script:\n{errorMessages}");
                }

                if (scriptBlockAst.ParamBlock == null)
                {
                    LoggingService.Warn($"Script {scriptPath} has no parameter block.", component: "ReflectionService");
                    return new List<WizardStep>();
                }

                foreach (var parameterAst in scriptBlockAst.ParamBlock.Parameters)
                {
                    string paramLabel = null;
                    double? paramControlWidth = null;
                    bool isSwitch = false;
                    List<string> validateSetChoices = null;
                    string validationPattern = null;
                    string validationScript = null;
                    string csvFilePath = null;
                    string csvValueColumn = null;
                    PathSelectorType paramPathType = PathSelectorType.None;
                    string pathFilter = null;
                    string pathDialogTitle = null;
                    bool isListBox = false;
                    bool isMultiSelect = false;
                    // Card support (for Card pages)
                    bool hasWizardCard = false;
                    string cardTitle = null;
                    string cardContent = null;
                    string cardImagePath = null;
                    string cardLinkUrl = null;
                    string cardLinkText = null;
                    string cardIconPath = null;
                    string cardIcon = null;
                    double? imageOpacity = null;
                    string backgroundColor = null;
                    string titleColor = null;
                    string contentColor = null;
                    int? cornerRadius = null;
                    string gradientStart = null;
                    string gradientEnd = null;
                    bool isNumeric = false;
                    double? numericMinimum = null;
                    double? numericMaximum = null;
                    double? numericStep = null;
                    bool numericAllowDecimal = false;
                    string numericFormat = null;
                    bool isDate = false;
                    DateTime? dateMinimum = null;
                    DateTime? dateMaximum = null;
                    string dateFormat = null;
                    bool isOptionGroup = false;
                    bool optionGroupHorizontal = false;
                    List<string> optionGroupChoices = null;
                    bool isMultiLine = false;
                    int? multiLineRows = null;
                    
                    // Enhanced control variables (optional explicit attributes)
                    int? textBoxMaxLength = null;
                    string textBoxPlaceholder = null;
                    bool? passwordShowReveal = null;
                    int? passwordMinLength = null;
                    string checkBoxCheckedLabel = null;
                    string checkBoxUncheckedLabel = null;
                    
                    // Dynamic parameter variables
                    bool isDynamic = false;
                    string dataSourceScriptBlock = null;
                    string dataSourceCsvPath = null;
                    string dataSourceCsvColumn = null;
                    string dataSourceCsvFilter = null;
                    List<string> dataSourceDependsOn = null;
                    bool dataSourceAsync = false;
                    bool dataSourceShowRefresh = false;

                    // Script cards JSON data (from UIScriptCards attribute)
                    string scriptCardsJson = null;
                    
                    // Banner JSON data (from UIBanner attribute)
                    string bannerJson = null;
                    
                    // Workflow tasks JSON data (from WizardWorkflowTasks attribute)
                    string workflowTasksJson = null;

                    Type paramType = typeof(object);

                    if (parameterAst.Attributes.OfType<TypeConstraintAst>().Any())
                    {
                        // Skip Wizard* type constraints and find the actual type
                        var typeConstraint = parameterAst.Attributes.OfType<TypeConstraintAst>()
                            .FirstOrDefault(tc => !tc.TypeName.Name.StartsWith("Wizard", StringComparison.OrdinalIgnoreCase));
                        
                        if (typeConstraint != null)
                        {
                            string typeName = typeConstraint.TypeName.Name;
                            string fullTypeName = typeConstraint.TypeName.FullName;

                            LoggingService.Trace($"  Parameter '{parameterAst.Name.VariablePath.UserPath}': Found TypeConstraintAst. TypeName='{typeName}', FullTypeName='{fullTypeName}'", component: "ReflectionService");

                            if (typeName.Equals("switch", StringComparison.OrdinalIgnoreCase))
                        {
                            paramType = typeof(bool);
                            isSwitch = true;
                            LoggingService.Trace($"    -> Determined type: bool (switch)", component: "ReflectionService");
                        }
                        else if (typeName.Equals("SecureString", StringComparison.OrdinalIgnoreCase) || fullTypeName.Equals("System.Security.SecureString", StringComparison.OrdinalIgnoreCase))
                        {
                            paramType = typeof(System.Security.SecureString);
                            LoggingService.Trace("    -> Determined type: SecureString", component: "ReflectionService");
                        }
                        else if (typeName.Equals("string", StringComparison.OrdinalIgnoreCase)) { paramType = typeof(string); LoggingService.Trace("    -> Determined type: string", component: "ReflectionService"); }
                        else if (typeName.Equals("int", StringComparison.OrdinalIgnoreCase)) { paramType = typeof(int); LoggingService.Trace("    -> Determined type: int", component: "ReflectionService"); }
                        else if (typeName.Equals("bool", StringComparison.OrdinalIgnoreCase)) { paramType = typeof(bool); LoggingService.Trace("    -> Determined type: bool", component: "ReflectionService"); }
                        else if (typeName.Equals("double", StringComparison.OrdinalIgnoreCase)) { paramType = typeof(double); LoggingService.Trace("    -> Determined type: double", component: "ReflectionService"); }
                        else if (typeName.Equals("DateTime", StringComparison.OrdinalIgnoreCase)) { paramType = typeof(DateTime); LoggingService.Trace("    -> Determined type: DateTime", component: "ReflectionService"); }
                        else
                        {
                            try
                            {
                                paramType = Type.GetType(fullTypeName, throwOnError: true, ignoreCase: true);
                                LoggingService.Trace($"    -> Determined type via Type.GetType: {paramType.FullName}", component: "ReflectionService");
                            }
                            catch (Exception ex)
                            {
                                LoggingService.Warn($"Could not resolve type '{fullTypeName}' for parameter '{parameterAst.Name.VariablePath.UserPath}'. Error: {ex.Message}. Defaulting to string.", component: "ReflectionService");
                                paramType = typeof(string); // Fallback to string
                            }
                        }
                        }
                    }
                    else if (parameterAst.StaticType != null && parameterAst.StaticType != typeof(object))
                    {
                         paramType = parameterAst.StaticType;
                        LoggingService.Trace($"  Parameter '{parameterAst.Name.VariablePath.UserPath}': Using StaticType: {paramType.FullName}", component: "ReflectionService");
                         if (paramType.Name == "SwitchParameter")
                         {
                            isSwitch = true;
                            paramType = typeof(bool);
                            LoggingService.Trace("    -> Converted SwitchParameter to bool", component: "ReflectionService");
                         }
                    }

                    // Process attributes
                    foreach (var attributeAst in parameterAst.Attributes)
                    {
                        if (attributeAst is AttributeAst attr)
                        {
                            string attrTypeName = attr.TypeName.Name;
                            LoggingService.Info($"  Processing attribute: {attrTypeName} (FullName: {attr.TypeName.FullName})", component: "ReflectionService");

                            if (attrTypeName == "WizardStep")
                                    {
                                string title = GetAttributeValue<string>(attr, "Title") ?? "Untitled Step";
                                
                                // Check if step already exists (for multi-parameter steps)
                                if (steps.ContainsKey(title))
                                {
                                    currentStep = steps[title];
                                    
                                    // Only update properties if they're not already set (preserve first occurrence)
                                    if (string.IsNullOrEmpty(currentStep.Description))
                                        currentStep.Description = GetAttributeValue<string>(attr, "Description");
                                    if (string.IsNullOrEmpty(currentStep.IntroductionText))
                                        currentStep.IntroductionText = GetAttributeValue<string>(attr, "IntroductionText");
                                    if (string.IsNullOrEmpty(currentStep.Prerequisites))
                                        currentStep.Prerequisites = GetAttributeValue<string>(attr, "Prerequisites");
                                    if (string.IsNullOrEmpty(currentStep.SupportLink))
                                        currentStep.SupportLink = GetAttributeValue<string>(attr, "SupportLink");
                                    if (string.IsNullOrEmpty(currentStep.IconPath))
                                        currentStep.IconPath = GetAttributeValue<string>(attr, "IconPath");
                                    if (string.IsNullOrEmpty(currentStep.IconGlyph))
                                        currentStep.IconGlyph = GetAttributeValue<string>(attr, "IconGlyph");
                                    
                                    LoggingService.Trace($"  Using existing step: Title='{title}'", component: "ReflectionService");
                                }
                                else
                                {
                                    // Create new step
                                    var stepAttr = new WizardStep();
                                    stepAttr.Title = title;
                                    stepAttr.Order = GetAttributeValue<int>(attr, "Order");
                                    stepAttr.Description = GetAttributeValue<string>(attr, "Description");
                                    stepAttr.PageType = GetAttributeValue<string>(attr, "PageType") ?? "GenericForm";
                                    stepAttr.IntroductionText = GetAttributeValue<string>(attr, "IntroductionText");
                                    stepAttr.Prerequisites = GetAttributeValue<string>(attr, "Prerequisites");
                                    stepAttr.SupportLink = GetAttributeValue<string>(attr, "SupportLink");
                                    stepAttr.IconPath = GetAttributeValue<string>(attr, "IconPath");
                                    stepAttr.IconGlyph = GetAttributeValue<string>(attr, "IconGlyph");

                                    currentStep = stepAttr;
                                    steps[stepAttr.Title] = stepAttr;
                                    LoggingService.Trace($"  Created new step: Title='{stepAttr.Title}', Order={stepAttr.Order}, PageType='{stepAttr.PageType}'", component: "ReflectionService");
                                }
                            }
                            else if (attrTypeName == "WizardParameterDetails")
                            {
                                paramLabel = GetAttributeValue<string>(attr, "Label");
                                if (TryConvertToDouble(GetAttributeValue<object>(attr, "ControlWidth"), out double width))
                                            {
                                    paramControlWidth = width;
                                }
                                LoggingService.Trace($"  Found parameter details: Label='{paramLabel}', ControlWidth={paramControlWidth}", component: "ReflectionService");
                            }
                            else if (attrTypeName == "ValidateSet")
                            {
                                validateSetChoices = attr.PositionalArguments
                                    .Select(arg => arg.ToString().Trim('\'', '"'))
                                    .ToList();
                                LoggingService.Trace($"  Found ValidateSet with {validateSetChoices.Count} choices: {string.Join(", ", validateSetChoices)}", component: "ReflectionService");
                            }
                            else if (attrTypeName == "WizardDropdown")
                            {
                                validateSetChoices = attr.PositionalArguments
                                    .Select(arg => arg.ToString().Trim('\'', '"'))
                                    .ToList();
                                LoggingService.Trace($"  Found WizardDropdown with {validateSetChoices.Count} choices: {string.Join(", ", validateSetChoices)}", component: "ReflectionService");
                            }
                            else if (attrTypeName == "ValidatePattern")
                            {
                                validationPattern = attr.PositionalArguments
                                    .FirstOrDefault()?.ToString().Trim('\'', '"');
                                LoggingService.Trace($"  Found ValidatePattern: {validationPattern}", component: "ReflectionService");
                            }
                            else if (attrTypeName == "WizardFilePath")
                            {
                                paramPathType = PathSelectorType.File;
                                // Check if Label is specified in the attribute
                                var labelValue = GetAttributeValue<string>(attr, "Label");
                                if (!string.IsNullOrEmpty(labelValue))
                                {
                                    paramLabel = labelValue;
                                }
                                
                                // Extract Filter and DialogTitle
                                if (TryGetNamedArgument(attr, "Filter", out string filterValue) && !string.IsNullOrEmpty(filterValue))
                                {
                                    pathFilter = filterValue.Trim('\'', '"');
                                    LoggingService.Trace($"  File Filter: {pathFilter}", component: "ReflectionService");
                                }
                                if (TryGetNamedArgument(attr, "DialogTitle", out string dialogTitleValue) && !string.IsNullOrEmpty(dialogTitleValue))
                                {
                                    pathDialogTitle = dialogTitleValue.Trim('\'', '"');
                                    LoggingService.Trace($"  Dialog Title: {pathDialogTitle}", component: "ReflectionService");
                                }
                                
                                LoggingService.Trace($"  Found WizardFilePath, Label='{paramLabel}', Filter='{pathFilter}', DialogTitle='{pathDialogTitle}'", component: "ReflectionService");
                            }
                            else if (attrTypeName == "WizardFolderPath")
                            {
                                paramPathType = PathSelectorType.Folder;
                                // Check if Label is specified in the attribute
                                var labelValue = GetAttributeValue<string>(attr, "Label");
                                if (!string.IsNullOrEmpty(labelValue))
                                {
                                    paramLabel = labelValue;
                                }
                                
                                // Extract DialogTitle
                                if (TryGetNamedArgument(attr, "DialogTitle", out string dialogTitleValue) && !string.IsNullOrEmpty(dialogTitleValue))
                                {
                                    pathDialogTitle = dialogTitleValue.Trim('\'', '"');
                                    LoggingService.Trace($"  Dialog Title: {pathDialogTitle}", component: "ReflectionService");
                                }
                                
                                LoggingService.Trace($"  Found WizardFolderPath, Label='{paramLabel}', DialogTitle='{pathDialogTitle}'", component: "ReflectionService");
                            }
                            else if (attrTypeName == "WizardDropdownFromCsv")
                            {
                                csvFilePath = GetAttributeValue<string>(attr, "CsvFilePath");
                                csvValueColumn = GetAttributeValue<string>(attr, "ValueColumn");
                                LoggingService.Trace($"  Found CSV dropdown config: Path='{csvFilePath}', Column='{csvValueColumn}'", component: "ReflectionService");
                            }
                            else if (attrTypeName == "WizardNumeric")
                            {
                                isNumeric = true;

                                if (TryGetNamedArgument(attr, "Minimum", out string minRaw) && TryParseDouble(minRaw, out double minVal))
                                {
                                    numericMinimum = minVal;
                                }
                                if (TryGetNamedArgument(attr, "Maximum", out string maxRaw) && TryParseDouble(maxRaw, out double maxVal))
                                {
                                    numericMaximum = maxVal;
                                }
                                if (TryGetNamedArgument(attr, "Step", out string stepRaw) && TryParseDouble(stepRaw, out double stepVal))
                                {
                                    numericStep = stepVal;
                                }
                                if (TryGetNamedArgument(attr, "AllowDecimal", out string allowDecimalRaw) && TryParseBool(allowDecimalRaw, out bool allowDecimal))
                                {
                                    numericAllowDecimal = allowDecimal;
                                }
                                if (TryGetNamedArgument(attr, "Format", out string formatValue) && !string.IsNullOrEmpty(formatValue))
                                {
                                    numericFormat = formatValue.Trim('\'', '"');
                                    LoggingService.Trace($"  Numeric Format: {numericFormat}", component: "ReflectionService");
                                }
                                LoggingService.Trace($"  Found WizardNumeric: Min={numericMinimum}, Max={numericMaximum}, Step={numericStep}, AllowDecimal={numericAllowDecimal}, Format={numericFormat}", component: "ReflectionService");
                            }
                            else if (attrTypeName == "WizardDate")
                            {
                                try
                                {
                                    isDate = true;

                                    if (TryGetNamedArgument(attr, "Minimum", out string minDateRaw) && !string.IsNullOrWhiteSpace(minDateRaw))
                                    {
                                        if (TryParseDateTime(minDateRaw, out DateTime minDate))
                                        {
                                            dateMinimum = minDate;
                                            LoggingService.Trace($"  WizardDate Minimum parsed: {dateMinimum}", component: "ReflectionService");
                                        }
                                        else
                                        {
                                            LoggingService.Warn($"  Failed to parse WizardDate Minimum: '{minDateRaw}'", component: "ReflectionService");
                                        }
                                    }
                                    
                                    if (TryGetNamedArgument(attr, "Maximum", out string maxDateRaw) && !string.IsNullOrWhiteSpace(maxDateRaw))
                                    {
                                        if (TryParseDateTime(maxDateRaw, out DateTime maxDate))
                                        {
                                            dateMaximum = maxDate;
                                            LoggingService.Trace($"  WizardDate Maximum parsed: {dateMaximum}", component: "ReflectionService");
                                        }
                                        else
                                        {
                                            LoggingService.Warn($"  Failed to parse WizardDate Maximum: '{maxDateRaw}'", component: "ReflectionService");
                                        }
                                    }
                                    
                                    if (TryGetNamedArgument(attr, "Format", out string formatRaw))
                                    {
                                        dateFormat = string.IsNullOrWhiteSpace(formatRaw) ? null : formatRaw;
                                    }
                                    
                                    LoggingService.Info($"  Found WizardDate: Min={dateMinimum}, Max={dateMaximum}, Format='{dateFormat}'", component: "ReflectionService");
                                }
                                catch (Exception ex)
                                {
                                    LoggingService.Error($"Error processing WizardDate attribute: {ex.Message}", ex, component: "ReflectionService");
                                    // Don't let date parsing failure crash the whole wizard
                                    isDate = false;
                                }
                            }
                            else if (attrTypeName == "WizardOptionGroup")
                            {
                                isOptionGroup = true;

                                if (TryGetNamedArgument(attr, "Orientation", out string orientationRaw))
                                {
                                    optionGroupHorizontal = orientationRaw.Equals("Horizontal", StringComparison.OrdinalIgnoreCase);
                                }

                                var positionalOptions = new List<string>();
                                for (int i = 0; i < attr.PositionalArguments.Count; i++)
                                {
                                    if (TryGetPositionalArgument(attr, i, out string optionValue) && !string.IsNullOrWhiteSpace(optionValue))
                                    {
                                        positionalOptions.Add(optionValue);
                                    }
                                }

                                if (positionalOptions.Count > 0)
                                {
                                    if (optionGroupChoices == null)
                                    {
                                        optionGroupChoices = new List<string>();
                                    }
                                    optionGroupChoices.AddRange(positionalOptions);
                                }

                                LoggingService.Trace($"  Found WizardOptionGroup: Orientation={(optionGroupHorizontal ? "Horizontal" : "Vertical")}, Options={(optionGroupChoices?.Count ?? 0)}", component: "ReflectionService");
                            }
                            else if (attrTypeName == "WizardMultiLine")
                            {
                                isMultiLine = true;
                                if (TryGetNamedArgument(attr, "Rows", out string rowsRaw) && TryParseInt(rowsRaw, out int rows) && rows > 0)
                                {
                                    multiLineRows = rows;
                                }
                                LoggingService.Trace($"  Found WizardMultiLine: Rows={multiLineRows}", component: "ReflectionService");
                            }
                            else if (attrTypeName == "WizardTextBox")
                            {
                                // Optional explicit TextBox attribute for enhanced features
                                if (TryGetNamedArgument(attr, "MaxLength", out string maxLenRaw) && TryParseInt(maxLenRaw, out int maxLen) && maxLen > 0)
                                {
                                    textBoxMaxLength = maxLen;
                                }
                                if (TryGetNamedArgument(attr, "Placeholder", out string placeholderRaw))
                                {
                                    textBoxPlaceholder = placeholderRaw?.Trim('\'', '"');
                                }
                                
                                LoggingService.Trace($"  Found WizardTextBox: MaxLength={textBoxMaxLength}, Placeholder={textBoxPlaceholder}", component: "ReflectionService");
                            }
                            else if (attrTypeName == "WizardPassword")
                            {
                                // Optional explicit Password attribute for enhanced features
                                if (TryGetNamedArgument(attr, "ShowRevealButton", out string showRevealRaw) && bool.TryParse(showRevealRaw, out bool showRevealVal))
                                {
                                    passwordShowReveal = showRevealVal;
                                }
                                if (TryGetNamedArgument(attr, "MinLength", out string minLenRaw) && TryParseInt(minLenRaw, out int minLen) && minLen > 0)
                                {
                                    passwordMinLength = minLen;
                                }
                                if (TryGetNamedArgument(attr, "ValidationPattern", out string validationPatternRaw))
                                {
                                    validationPattern = validationPatternRaw?.Trim('\'', '"');
                                }
                                if (TryGetNamedArgument(attr, "ValidationScript", out string validationScriptRaw))
                                {
                                    // ValidationScript is Base64 encoded to avoid parsing issues
                                    var base64Script = validationScriptRaw?.Trim('\'', '"');
                                    if (!string.IsNullOrEmpty(base64Script))
                                    {
                                        try
                                        {
                                            var bytes = Convert.FromBase64String(base64Script);
                                            validationScript = System.Text.Encoding.UTF8.GetString(bytes);
                                        }
                                        catch
                                        {
                                            // If not valid Base64, use as-is (backward compatibility)
                                            validationScript = base64Script;
                                        }
                                    }
                                }
                                
                                LoggingService.Trace($"  Found WizardPassword: ShowRevealButton={passwordShowReveal}, MinLength={passwordMinLength}, ValidationPattern={validationPattern}, ValidationScript={validationScript?.Substring(0, Math.Min(50, validationScript?.Length ?? 0))}...", component: "ReflectionService");
                            }
                            else if (attrTypeName == "WizardCheckBox")
                            {
                                // Optional explicit CheckBox attribute for enhanced features
                                if (TryGetNamedArgument(attr, "CheckedLabel", out string checkedRaw))
                                {
                                    checkBoxCheckedLabel = checkedRaw?.Trim('\'', '"');
                                }
                                if (TryGetNamedArgument(attr, "UncheckedLabel", out string uncheckedRaw))
                                {
                                    checkBoxUncheckedLabel = uncheckedRaw?.Trim('\'', '"');
                                }
                                
                                LoggingService.Trace($"  Found WizardCheckBox: CheckedLabel={checkBoxCheckedLabel}, UncheckedLabel={checkBoxUncheckedLabel}", component: "ReflectionService");
                            }
                            else if (attrTypeName == "UIDataSource")
                            {
                                LoggingService.Trace("  Found UIDataSource attribute", component: "ReflectionService");
                                
                                // Check for ScriptBlock (positional or named)
                                if (attr.PositionalArguments.Count > 0)
                                {
                                    var scriptArg = attr.PositionalArguments[0];
                                    if (scriptArg is ScriptBlockExpressionAst scriptBlock)
                                    {
                                        // Get script block text and strip outer braces
                                        string scriptText = scriptBlock.Extent.Text.Trim();
                                        if (scriptText.StartsWith("{") && scriptText.EndsWith("}"))
                                        {
                                            scriptText = scriptText.Substring(1, scriptText.Length - 2).Trim();
                                        }
                                        dataSourceScriptBlock = scriptText;
                                        LoggingService.Trace($"    ScriptBlock: {dataSourceScriptBlock.Substring(0, Math.Min(50, dataSourceScriptBlock.Length))}...", component: "ReflectionService");
                                        
                                        // Auto-detect dependencies from script block parameters (Phase 2)
                                        if (scriptBlock.ScriptBlock?.ParamBlock?.Parameters != null)
                                        {
                                            dataSourceDependsOn = new List<string>();
                                            foreach (var paramAst in scriptBlock.ScriptBlock.ParamBlock.Parameters)
                                            {
                                                string paramName = paramAst.Name.VariablePath.UserPath;
                                                dataSourceDependsOn.Add(paramName);
                                                LoggingService.Trace($"    Auto-detected dependency: {paramName}", component: "ReflectionService");
                                            }
                                            if (dataSourceDependsOn.Count > 0)
                                            {
                                                LoggingService.Info($"    Dependencies auto-detected from script block: [{string.Join(", ", dataSourceDependsOn)}]", component: "ReflectionService");
                                            }
                                        }
                                    }
                                }
                                
                                // Check for CSV configuration
                                if (TryGetNamedArgument(attr, "CsvPath", out string csvPath))
                                {
                                    csvPath = csvPath.Trim('\'', '"');
                                    // Resolve relative paths to the script directory
                                    dataSourceCsvPath = Path.IsPathRooted(csvPath)
                                        ? csvPath
                                        : Path.GetFullPath(Path.Combine(scriptDirectory, csvPath));
                                    LoggingService.Trace($"    Resolved CSV path: {dataSourceCsvPath}", component: "ReflectionService");
                                }
                                if (TryGetNamedArgument(attr, "CsvColumn", out string csvCol))
                                {
                                    dataSourceCsvColumn = csvCol.Trim('\'', '"');
                                }
                                if (TryGetNamedArgument(attr, "CsvFilter", out string csvFilterRaw))
                                {
                                    // Extract script block from CsvFilter
                                    dataSourceCsvFilter = csvFilterRaw;
                                }
                                
                                // Check for DependsOn
                                if (TryGetNamedArgument(attr, "DependsOn", out string dependsOnRaw))
                                {
                                    // Parse array: @('Param1', 'Param2')
                                    dataSourceDependsOn = ParseStringArray(dependsOnRaw);
                                    LoggingService.Trace($"    DependsOn: [{string.Join(", ", dataSourceDependsOn)}]", component: "ReflectionService");
                                }
                                
                                // Check for other properties
                                if (TryGetNamedArgument(attr, "Async", out string asyncRaw) && TryParseBool(asyncRaw, out bool asyncVal))
                                {
                                    dataSourceAsync = asyncVal;
                                }
                                if (TryGetNamedArgument(attr, "ShowRefreshButton", out string refreshRaw) && TryParseBool(refreshRaw, out bool refreshVal))
                                {
                                    dataSourceShowRefresh = refreshVal;
                                }
                                isDynamic = true;
                                LoggingService.Info($"  Parameter '{parameterAst.Name.VariablePath.UserPath}' marked as dynamic", component: "ReflectionService");
                            }
                            else if (attrTypeName == "WizardCard")
                            {
                                hasWizardCard = true;
                                try
                                {
                                    // Support both named and positional arguments
                                    string t = GetAttributeValue<string>(attr, "Title");
                                    string c = GetAttributeValue<string>(attr, "Content");
                                    if (string.IsNullOrEmpty(t) && attr.PositionalArguments.Count > 0)
                                    {
                                        t = attr.PositionalArguments[0].ToString().Trim('\'', '"');
                                    }
                                    if (string.IsNullOrEmpty(c) && attr.PositionalArguments.Count > 1)
                                    {
                                        c = attr.PositionalArguments[1].ToString().Trim('\'', '"');
                                    }

                                    // Process PowerShell escape sequences in card content
                                    if (!string.IsNullOrEmpty(c))
                                    {
                                        c = c.Replace("``n", "\n")
                                             .Replace("``r", "\r")
                                             .Replace("``t", "\t")
                                             .Replace("`n", "\n")
                                             .Replace("`r", "\r")
                                             .Replace("`t", "\t")
                                             .Replace("''", "'");  // Un-escape PowerShell single quotes

                                        if (c.StartsWith("@'", StringComparison.Ordinal) && c.EndsWith("'@", StringComparison.Ordinal) && c.Length >= 4)
                                        {
                                            c = c.Substring(2, c.Length - 4);
                                        }
                                    }
                                    
                                    // Also un-escape title
                                    if (!string.IsNullOrEmpty(t))
                                    {
                                        t = t.Replace("''", "'");
                                    }

                                    cardTitle = t;
                                    cardContent = c;
                                    
                                    // Parse enhanced card properties
                                    cardImagePath = GetAttributeValue<string>(attr, "ImagePath");
                                    cardLinkUrl = GetAttributeValue<string>(attr, "LinkUrl");
                                    cardLinkText = GetAttributeValue<string>(attr, "LinkText");
                                    cardIconPath = GetAttributeValue<string>(attr, "IconPath");
                                    cardIcon = GetAttributeValue<string>(attr, "Icon");
                                    
                                    // Parse styling properties
                                    imageOpacity = GetAttributeValue<double?>(attr, "ImageOpacity");
                                    backgroundColor = GetAttributeValue<string>(attr, "BackgroundColor");
                                    titleColor = GetAttributeValue<string>(attr, "TitleColor");
                                    contentColor = GetAttributeValue<string>(attr, "ContentColor");
                                    cornerRadius = GetAttributeValue<int?>(attr, "CornerRadius");
                                    gradientStart = GetAttributeValue<string>(attr, "GradientStart");
                                    gradientEnd = GetAttributeValue<string>(attr, "GradientEnd");
                                    
                                    LoggingService.Info($"  Found WizardCard: Title='{cardTitle}', IconPath='{cardIconPath ?? "null"}', ImagePath='{cardImagePath ?? "null"}', BackgroundColor='{backgroundColor ?? "null"}', NamedArgs={attr.NamedArguments.Count}", component: "ReflectionService");
                                }
                                catch (Exception ex)
                                {
                                    LoggingService.Error($"Error parsing WizardCard attribute: {ex.Message}", component: "ReflectionService");
                                }
                            }
                            else if (attrTypeName == "WizardPathSelector")
                            {
                                // Try positional argument first (index 0), then named "Type" property
                                var pathTypeStr = GetAttributeValue<string>(attr, "Type");
                                if (string.IsNullOrEmpty(pathTypeStr) && attr.PositionalArguments.Count > 0)
                                {
                                    var posArg = attr.PositionalArguments[0];
                                    if (posArg is StringConstantExpressionAst stringConst)
                                    {
                                        pathTypeStr = stringConst.Value;
                                    }
                                }
                                
                                if (!string.IsNullOrEmpty(pathTypeStr) && Enum.TryParse<PathSelectorType>(pathTypeStr, true, out PathSelectorType pathType))
                                {
                                    paramPathType = pathType;
                                    LoggingService.Trace($"  Found path selector type: {paramPathType}", component: "ReflectionService");
                                }
                            }
                            else if (attrTypeName == "WizardListBox")
                            {
                                isListBox = true;
                                isMultiSelect = GetAttributeValue<bool>(attr, "MultiSelect");
                                LoggingService.Trace($"  Found WizardListBox: MultiSelect={isMultiSelect}", component: "ReflectionService");
                            }
                            else if (attrTypeName == "WizardSwitch")
                            {
                                isSwitch = true;
                                paramType = typeof(bool);
                                LoggingService.Trace("  Found WizardSwitch attribute - marking as switch", component: "ReflectionService");
                            }
                            else if (attrTypeName == "UIScriptCards")
                            {
                                // CardGrid script cards JSON data
                                if (attr.PositionalArguments.Count > 0)
                                {
                                    string jsonArg = attr.PositionalArguments[0].ToString().Trim('\'', '"');

                                    // Check if Base64 encoded (new format to preserve special characters)
                                    if (jsonArg.StartsWith("BASE64:", StringComparison.OrdinalIgnoreCase))
                                    {
                                        try
                                        {
                                            string base64Data = jsonArg.Substring(7); // Remove "BASE64:" prefix
                                            byte[] jsonBytes = Convert.FromBase64String(base64Data);
                                            scriptCardsJson = System.Text.Encoding.UTF8.GetString(jsonBytes);
                                            LoggingService.Info($"  Found UIScriptCards (Base64) with {scriptCardsJson.Length} chars of JSON", component: "ReflectionService");
                                        }
                                        catch (Exception ex)
                                        {
                                            LoggingService.Error($"Failed to decode Base64 UIScriptCards: {ex.Message}", component: "ReflectionService");
                                            scriptCardsJson = null;
                                        }
                                    }
                                    else
                                    {
                                        // Legacy format: unescape the JSON (was escaped for PowerShell attribute syntax)
                                        scriptCardsJson = jsonArg.Replace("`\"", "\"");
                                        LoggingService.Info($"  Found UIScriptCards (legacy) with {scriptCardsJson.Length} chars of JSON", component: "ReflectionService");
                                    }
                                }
                            }
                            else if (attrTypeName == "UIBanner")
                            {
                                // Banner JSON data for wizard steps
                                if (attr.PositionalArguments.Count > 0)
                                {
                                    string jsonArg = attr.PositionalArguments[0].ToString().Trim('\'', '"');

                                    // Check if Base64 encoded
                                    if (jsonArg.StartsWith("BASE64:", StringComparison.OrdinalIgnoreCase))
                                    {
                                        try
                                        {
                                            string base64Data = jsonArg.Substring(7); // Remove "BASE64:" prefix
                                            byte[] jsonBytes = Convert.FromBase64String(base64Data);
                                            bannerJson = System.Text.Encoding.UTF8.GetString(jsonBytes);
                                            LoggingService.Info($"  Found UIBanner (Base64) with {bannerJson.Length} chars of JSON", component: "ReflectionService");
                                        }
                                        catch (Exception ex)
                                        {
                                            LoggingService.Error($"Failed to decode Base64 UIBanner: {ex.Message}", component: "ReflectionService");
                                            bannerJson = null;
                                        }
                                    }
                                    else
                                    {
                                        bannerJson = jsonArg;
                                        LoggingService.Info($"  Found UIBanner with {bannerJson.Length} chars of JSON", component: "ReflectionService");
                                    }
                                }
                            }
                            else if (attrTypeName == "WizardWorkflowTasks")
                            {
                                // Workflow tasks JSON data
                                if (attr.PositionalArguments.Count > 0)
                                {
                                    string jsonArg = attr.PositionalArguments[0].ToString().Trim('\'', '"');
                                    // Unescape the JSON (was escaped for PowerShell attribute syntax)
                                    workflowTasksJson = jsonArg.Replace("''", "'");
                                    LoggingService.Info($"  Found WizardWorkflowTasks with {workflowTasksJson.Length} chars of JSON", component: "ReflectionService");
                                }
                            }
                            else if (attrTypeName == "WizardBranding")
                            {
                                // Global branding info can be specified on any parameter
                                if (branding == null) branding = new WizardBranding();
                                var wt = GetAttributeValue<string>(attr, "WindowTitleText");
                                var wi = GetAttributeValue<string>(attr, "WindowTitleIcon");
                                var st = GetAttributeValue<string>(attr, "SidebarHeaderText");
                                var si = GetAttributeValue<string>(attr, "SidebarHeaderIcon");
                                var sip = GetAttributeValue<string>(attr, "SidebarHeaderIconPath"); // Support alias
                                var sio = GetAttributeValue<string>(attr, "SidebarHeaderIconOrientation");
                                var theme = GetAttributeValue<string>(attr, "Theme");
                                var osn = GetAttributeValue<string>(attr, "OriginalScriptName"); // For Module API log naming
                                var osp = GetAttributeValue<string>(attr, "OriginalScriptPath");
                                if (!string.IsNullOrEmpty(wt)) branding.WindowTitleText = wt;
                                if (!string.IsNullOrEmpty(wi)) branding.WindowTitleIcon = wi;
                                if (!string.IsNullOrEmpty(st)) branding.SidebarHeaderText = st;
                                if (!string.IsNullOrEmpty(si)) branding.SidebarHeaderIcon = si;
                                if (!string.IsNullOrEmpty(sip)) branding.SidebarHeaderIcon = sip; // Use Path if provided
                                if (!string.IsNullOrEmpty(sio)) branding.SidebarHeaderIconOrientation = sio;
                                if (!string.IsNullOrEmpty(theme)) branding.Theme = theme;
                                if (!string.IsNullOrEmpty(osn)) branding.OriginalScriptName = osn;
                                if (!string.IsNullOrEmpty(osp)) branding.OriginalScriptPath = osp;
                                var skipToWorkflow = GetAttributeValue<string>(attr, "SkipToWorkflow");
                                if (!string.IsNullOrEmpty(skipToWorkflow) && skipToWorkflow.Equals("true", StringComparison.OrdinalIgnoreCase))
                                {
                                    branding.SkipToWorkflow = true;
                                    LoggingService.Info("  SkipToWorkflow enabled - will skip to workflow step on load", component: "ReflectionService");
                                }
                                var logPath = GetAttributeValue<string>(attr, "LogPath");
                                if (!string.IsNullOrEmpty(logPath))
                                {
                                    branding.LogPath = logPath;
                                    LoggingService.Info($"  Custom log path: {logPath}", component: "ReflectionService");
                                }
                                var prevLogPath = GetAttributeValue<string>(attr, "PreviousLogFilePath");
                                if (!string.IsNullOrEmpty(prevLogPath))
                                {
                                    branding.PreviousLogFilePath = prevLogPath;
                                    LoggingService.Info($"  Previous log file path for resume: {prevLogPath}", component: "ReflectionService");
                                }
                                LoggingService.Trace("  Captured WizardBranding attribute for global UI customization", component: "ReflectionService");
                            }
                        }
                    }

                    if (currentStep != null)
                    {
                        if (isOptionGroup && optionGroupChoices != null && optionGroupChoices.Count > 0)
                        {
                            validateSetChoices = optionGroupChoices;
                        }

                        // Determine if this is a placeholder parameter
                        bool isPlaceholder = false;
                        string paramName = parameterAst.Name.VariablePath.UserPath;
                        
                        // Mark placeholders for Welcome, Summary, Card, Banner, and Branding pages
                        if (
                            currentStep.PageType == "Card" ||
                            hasWizardCard ||  // Has WizardCard attribute (even with empty title)
                            !string.IsNullOrEmpty(bannerJson) ||  // Has UIBanner attribute
                            paramName.EndsWith("Placeholder", StringComparison.OrdinalIgnoreCase))
                        {
                            isPlaceholder = true;
                            LoggingService.Trace($"  Marked parameter '{paramName}' as placeholder (PageType: {currentStep.PageType}, HasBanner: {!string.IsNullOrEmpty(bannerJson)})", component: "ReflectionService");
                        }
                        
                        // Check for mandatory attribute
                        bool isMandatory = parameterAst.Attributes
                            .OfType<AttributeAst>()
                            .Any(a => a.TypeName.Name == "Parameter" && 
                                      GetAttributeValue<bool>(a, "Mandatory"));
                        
                        object defaultValue = null;
                        
                        var paramInfo = new ParameterInfo
                        {
                            Name = parameterAst.Name.VariablePath.UserPath,
                            ParameterType = paramType,
                            DefaultValue = defaultValue,
                            IsMandatory = isMandatory,
                            Label = paramLabel,
                            ValidateSetChoices = validateSetChoices,
                            ValidationPattern = validationPattern,
                            ValidationScript = validationScript,
                            ControlWidth = paramControlWidth,
                            IsSwitch = isSwitch,
                            PathType = paramPathType,
                            PathFilter = pathFilter,
                            DialogTitle = pathDialogTitle,
                            IsListBox = isListBox,
                            IsMultiSelect = isMultiSelect,
                            IsCard = hasWizardCard,
                            CardTitle = cardTitle,
                            CardContent = cardContent,
                            CardImagePath = cardImagePath,
                            CardLinkUrl = cardLinkUrl,
                            CardLinkText = cardLinkText,
                            CardIconPath = cardIconPath,
                            CardIcon = cardIcon,
                            CardImageOpacity = imageOpacity,
                            CardBackgroundColor = backgroundColor,
                            CardTitleColor = titleColor,
                            CardContentColor = contentColor,
                            CardCornerRadius = cornerRadius,
                            CardGradientStart = gradientStart,
                            CardGradientEnd = gradientEnd,
                            IsPlaceholder = isPlaceholder,
                            IsNumeric = isNumeric,
                            NumericMinimum = numericMinimum,
                            NumericMaximum = numericMaximum,
                            NumericStep = numericStep,
                            NumericAllowDecimal = numericAllowDecimal,
                            NumericFormat = numericFormat,
                            IsDate = isDate,
                            DateMinimum = dateMinimum,
                            DateMaximum = dateMaximum,
                            DateFormat = dateFormat,
                            IsOptionGroup = isOptionGroup,
                            OptionGroupHorizontalLayout = optionGroupHorizontal,
                            IsMultiLineText = isMultiLine,
                            MultiLineRows = multiLineRows,
                            
                            // Enhanced control properties (optional explicit attributes)
                            TextBoxMaxLength = textBoxMaxLength,
                            TextBoxPlaceholder = textBoxPlaceholder,
                            PasswordShowReveal = passwordShowReveal,
                            PasswordMinLength = passwordMinLength,
                            CheckBoxCheckedLabel = checkBoxCheckedLabel,
                            CheckBoxUncheckedLabel = checkBoxUncheckedLabel,
                            
                            // Dynamic parameter properties
                            IsDynamic = isDynamic,
                            DataSourceScriptBlock = dataSourceScriptBlock,
                            DataSourceCsvPath = dataSourceCsvPath,
                            DataSourceCsvColumn = dataSourceCsvColumn,
                            DataSourceCsvFilter = dataSourceCsvFilter,
                            DataSourceDependsOn = dataSourceDependsOn ?? new List<string>(),
                            DataSourceAsync = dataSourceAsync,
                            DataSourceShowRefresh = dataSourceShowRefresh,
                            
                            // CardGrid script cards JSON
                            ScriptCardsJson = scriptCardsJson,
                            
                            // Banner JSON for wizard steps
                            BannerJson = bannerJson,
                            
                            // Workflow tasks JSON
                            WorkflowTasksJson = workflowTasksJson
                        };
                        // Handle default value
                        if (parameterAst.DefaultValue != null)
                        {
                            paramInfo.DefaultValue = parameterAst.DefaultValue.SafeGetValue();
                            LoggingService.Trace($"  Set default value for '{paramInfo.Name}': {paramInfo.DefaultValue}", component: "ReflectionService");
                        }

                        // Process CSV choices if specified
                        if (!string.IsNullOrEmpty(csvFilePath))
                        {
                            try
                            {
                                string resolvedCsvPath = Path.IsPathRooted(csvFilePath)
                                    ? csvFilePath
                                    : Path.GetFullPath(Path.Combine(scriptDirectory, csvFilePath));

                                LoggingService.Trace($"  Loading CSV choices from: {resolvedCsvPath}", component: "ReflectionService");
                                paramInfo.ValidateSetChoices = ReadCsvColumn(resolvedCsvPath, csvValueColumn);
                                LoggingService.Trace($"  Loaded {paramInfo.ValidateSetChoices?.Count ?? 0} choices from CSV", component: "ReflectionService");
            }
                            catch (Exception ex)
            {
                                LoggingService.Error($"Error loading CSV choices for parameter '{paramInfo.Name}': {ex.Message}", component: "ReflectionService");
                            }
                        }

                        currentStep.Parameters.Add(paramInfo);
                        LoggingService.Trace($"Added parameter to step '{currentStep.Title}': {paramInfo.Name} ({paramInfo.ParameterType.Name})", component: "ReflectionService");
                    }
                }

                var orderedSteps = steps.Values
                    .OrderBy(s => s.Order)
                    .ToList();

                LoggingService.Info($"Successfully loaded {orderedSteps.Count} steps from script", component: "ReflectionService");
                
                // Execute dynamic parameter script blocks to populate initial choices
                // Note: This happens before UI is shown, so no progress overlay is possible here
                // For UI refresh on dependency changes, see MainWindowViewModel.RefreshParameterChoices
                var dynamicParams = orderedSteps
                    .SelectMany(step => step.Parameters)
                    .Where(p => p.IsDynamic)
                    .ToList();

                if (dynamicParams.Any())
                {
                    LoggingService.Info($"Found {dynamicParams.Count} dynamic parameters, executing data sources...", component: "ReflectionService");
                    
                    using (var runspace = RunspaceFactory.CreateRunspace())
                    {
                        runspace.Open();
                        var dynamicManager = new DynamicParameterManager(runspace);
                        
                        // Register all dynamic parameters
                        foreach (var param in dynamicParams)
                        {
                            var attr = new UIDataSourceAttribute();
                            
                            if (!string.IsNullOrEmpty(param.DataSourceScriptBlock))
                            {
                                // Pass script directly - do NOT wrap in braces
                                // Wrapping in braces creates a nested script block literal that returns itself
                                attr.ScriptBlock = ScriptBlock.Create(param.DataSourceScriptBlock);
                            }
                            else if (!string.IsNullOrEmpty(param.DataSourceCsvPath))
                            {
                                attr.CsvPath = param.DataSourceCsvPath;
                                attr.CsvColumn = param.DataSourceCsvColumn;
                                if (!string.IsNullOrEmpty(param.DataSourceCsvFilter))
                                {
                                    attr.CsvFilter = ScriptBlock.Create(param.DataSourceCsvFilter);
                                }
                            }
                            
                            attr.DependsOn = param.DataSourceDependsOn.ToArray();
                            attr.Async = param.DataSourceAsync;
                            attr.ShowRefreshButton = param.DataSourceShowRefresh;
                            
                            dynamicManager.RegisterParameter(param.Name, attr);
                        }
                        
                        // Get execution order (topological sort handles dependencies)
                        var executionOrder = dynamicManager.GetExecutionOrder();
                        LoggingService.Info($"Dynamic parameter execution order: [{string.Join(", ", executionOrder)}]", component: "ReflectionService");
                        
                        // Execute parameters in order
                        var paramValues = new Dictionary<string, object>();
                        foreach (var paramName in executionOrder)
                        {
                            try
                            {
                                LoggingService.Debug($"Executing data source for '{paramName}'...", component: "ReflectionService");
                                var choices = dynamicManager.ExecuteDataSource(paramName, paramValues);
                                
                                // Find the parameter and update its choices
                                var param = dynamicParams.FirstOrDefault(p => p.Name.Equals(paramName, StringComparison.OrdinalIgnoreCase));
                                if (param != null)
                                {
                                    param.ValidateSetChoices = choices.ToList();
                                    LoggingService.Info($"  Populated '{paramName}' with {choices.Length} choices", component: "ReflectionService");
                                    
                                    // Store default value for dependent parameters
                                    if (param.DefaultValue != null)
                                    {
                                        paramValues[paramName] = param.DefaultValue;
                                    }
                                    else if (choices.Length > 0)
                                    {
                                        paramValues[paramName] = choices[0]; // Use first choice as default
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                LoggingService.Error($"Failed to execute data source for '{paramName}': {ex.Message}", component: "ReflectionService");
                                // Continue with other parameters
                            }
                        }
                        
                        runspace.Close();
                    }
                }
                
                return orderedSteps;
            }
            catch (Exception ex)
            {
                LoggingService.Error($"Error loading wizard steps from script: {ex.Message}", component: "ReflectionService");
                throw;
            }
        }

        private bool IsParameterMandatory(ParameterAst parameterAst)
        {
            var parameterAttribute = parameterAst.Attributes
                .OfType<AttributeAst>()
                .FirstOrDefault(attr => attr.TypeName.Name == "Parameter");

            if (parameterAttribute != null)
            {
                var mandatoryArg = parameterAttribute.NamedArguments
                    .FirstOrDefault(arg => arg.ArgumentName == "Mandatory");

                if (mandatoryArg != null)
                {
                    // Handle different argument types
                    var argValue = mandatoryArg.Argument;
                    if (argValue is VariableExpressionAst varExpr)
                    {
                        // Handle $true/$false variables
                        return varExpr.VariablePath.UserPath.Equals("true", StringComparison.OrdinalIgnoreCase);
                    }
                    else if (argValue is ConstantExpressionAst constExpr)
                    {
                        // Handle literal true/false
                        if (constExpr.Value is bool boolVal)
                            return boolVal;
                        if (bool.TryParse(constExpr.Value?.ToString(), out bool result))
                            return result;
                    }
                    else
                    {
                        // Fallback: try to parse the string representation
                        var argStr = argValue.ToString();
                        if (argStr.Equals("$true", StringComparison.OrdinalIgnoreCase) || 
                            argStr.Equals("true", StringComparison.OrdinalIgnoreCase))
                            return true;
                        if (argStr.Equals("$false", StringComparison.OrdinalIgnoreCase) || 
                            argStr.Equals("false", StringComparison.OrdinalIgnoreCase))
                            return false;
                    }
                }
                else
                {
                    // Check if Mandatory is used without a value (implicitly true)
                    return parameterAttribute.PositionalArguments
                        .Any(arg => arg.ToString().Contains("Mandatory"));
                }
            }

            return false;
        }

        private List<string> ReadCsvColumn(string csvFilePath, string columnIdentifier)
        {
            try
            {
            if (!File.Exists(csvFilePath))
            {
                    LoggingService.Error($"CSV file not found: {csvFilePath}", component: "ReflectionService");
                    return new List<string>();
            }

                var values = new List<string>();
            string[] lines = File.ReadAllLines(csvFilePath);
            
                if (lines.Length == 0)
            {
                    LoggingService.Warn($"CSV file is empty: {csvFilePath}", component: "ReflectionService");
                return values;
            }

                // Parse header row
            string[] headers = SplitCsvLine(lines[0]);
            int columnIndex = -1;

                // Try to find column by name first
                if (!string.IsNullOrEmpty(columnIdentifier))
                {
                    columnIndex = Array.FindIndex(headers, h => 
                        h.Equals(columnIdentifier, StringComparison.OrdinalIgnoreCase));
            }

                // If not found by name and it's a number, try as index
                if (columnIndex == -1 && int.TryParse(columnIdentifier, out int index))
                    {
                    columnIndex = index < headers.Length ? index : -1;
            }

            if (columnIndex == -1)
            {
                    LoggingService.Error($"Column '{columnIdentifier}' not found in CSV file", component: "ReflectionService");
                    return values;
            }

                // Process data rows
            for (int i = 1; i < lines.Length; i++)
                {
                    if (string.IsNullOrWhiteSpace(lines[i])) continue;

                string[] fields = SplitCsvLine(lines[i]);
                    if (columnIndex < fields.Length)
                {
                    string value = fields[columnIndex].Trim();
                        if (!string.IsNullOrEmpty(value))
                    {
                        values.Add(value);
                        }
                    }
                }

                LoggingService.Trace($"Read {values.Count} values from CSV column '{columnIdentifier}'", component: "ReflectionService");
                return values;
            }
            catch (Exception ex)
            {
                LoggingService.Error($"Error reading CSV file: {ex.Message}", component: "ReflectionService");
                return new List<string>();
            }
        }

        private string[] SplitCsvLine(string line)
        {
            var result = new List<string>();
            var currentField = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                if (line[i] == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        // Handle escaped quotes
                        currentField.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (line[i] == ',' && !inQuotes)
                {
                    result.Add(currentField.ToString().Trim());
                    currentField.Clear();
                }
                else
                {
                    currentField.Append(line[i]);
                }
            }

            result.Add(currentField.ToString().Trim());
            return result.ToArray();
        }

        private bool TryGetNamedArgument(AttributeAst attributeAst, string argumentName, out string value)
        {
            value = null;
            var namedArg = attributeAst.NamedArguments
                .FirstOrDefault(arg => arg.ArgumentName.Equals(argumentName, StringComparison.OrdinalIgnoreCase));

            if (namedArg != null)
            {
                value = namedArg.Argument?.ToString()?.Trim('\'','"');
                return !string.IsNullOrEmpty(value);
            }

            return false;
        }

        private bool TryGetPositionalArgument(AttributeAst attributeAst, int index, out string value)
        {
            value = null;
            if (index < 0 || index >= attributeAst.PositionalArguments.Count)
            {
                return false;
            }

            var arg = attributeAst.PositionalArguments[index];
            value = arg?.ToString()?.Trim('\'','"');
            return !string.IsNullOrEmpty(value);
        }

        private bool TryParseDouble(string rawValue, out double result)
        {
            result = 0;
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return false;
            }

            return double.TryParse(rawValue, NumberStyles.Any, CultureInfo.InvariantCulture, out result);
        }

        private bool TryParseInt(string rawValue, out int result)
        {
            result = 0;
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return false;
            }

            return int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
        }

        private bool TryParseBool(string rawValue, out bool result)
        {
            result = false;
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return false;
            }

            if (rawValue.Equals("$true", StringComparison.OrdinalIgnoreCase) || rawValue.Equals("true", StringComparison.OrdinalIgnoreCase))
            {
                result = true;
                return true;
            }

            if (rawValue.Equals("$false", StringComparison.OrdinalIgnoreCase) || rawValue.Equals("false", StringComparison.OrdinalIgnoreCase))
            {
                result = false;
                return true;
            }

            return bool.TryParse(rawValue, out result);
        }

        private bool TryParseDateTime(string rawValue, out DateTime result)
        {
            result = default;
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return false;
            }

            if (DateTime.TryParse(rawValue, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out result))
            {
                return true;
            }

            // Support yyyy-MM-dd and yyyy-MM-ddTHH:mm:ss formats explicitly
            string[] formats = { "yyyy-MM-dd", "yyyy-MM-ddTHH:mm:ss", "yyyy-MM-ddTHH:mm:ssZ" };
            return DateTime.TryParseExact(rawValue, formats, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out result);
        }

        private T GetAttributeValue<T>(AttributeAst attributeAst, string argumentName)
        {
            try
            {
                // First check named arguments
                var namedArg = attributeAst.NamedArguments
                    .FirstOrDefault(arg => arg.ArgumentName.Equals(argumentName, StringComparison.OrdinalIgnoreCase));

                if (namedArg != null)
                {
                    string rawValue = namedArg.Argument.ToString().Trim('\'','"');
                    
                    // Special handling for boolean values from PowerShell
                    if (typeof(T) == typeof(bool))
                    {
                        // Handle PowerShell $true/$false variables
                        if (rawValue.Equals("$true", StringComparison.OrdinalIgnoreCase) || rawValue.Equals("true", StringComparison.OrdinalIgnoreCase))
                            return (T)(object)true;
                        if (rawValue.Equals("$false", StringComparison.OrdinalIgnoreCase) || rawValue.Equals("false", StringComparison.OrdinalIgnoreCase))
                            return (T)(object)false;
                        
                        // Try standard boolean parsing
                        if (bool.TryParse(rawValue, out bool boolResult))
                            return (T)(object)boolResult;
                            
                        return default(T);
                    }
                    
                    return (T)Convert.ChangeType(rawValue, typeof(T), CultureInfo.InvariantCulture);
                }

                // Support positional arguments for WizardDropdownFromCsv
                if (argumentName == "CsvFilePath" && attributeAst.PositionalArguments.Count > 0)
                {
                    string rawValue = attributeAst.PositionalArguments[0].ToString().Trim('\'','"');
                    return (T)Convert.ChangeType(rawValue, typeof(T), CultureInfo.InvariantCulture);
                }
                else if (argumentName == "ValueColumn" && attributeAst.PositionalArguments.Count > 1)
                {
                    string rawValue = attributeAst.PositionalArguments[1].ToString().Trim('\'','"');
                    return (T)Convert.ChangeType(rawValue, typeof(T), CultureInfo.InvariantCulture);
                }

                // If not found in named arguments and it's a positional parameter for WizardStep
                if (argumentName == "Title" && attributeAst.PositionalArguments.Count > 0)
                {
                    string rawValue = attributeAst.PositionalArguments[0].ToString().Trim('\'','"');
                    return (T)Convert.ChangeType(rawValue, typeof(T), CultureInfo.InvariantCulture);
                }
                else if (argumentName == "Order" && attributeAst.PositionalArguments.Count > 1)
                {
                    string rawValue = attributeAst.PositionalArguments[1].ToString().Trim('\'','"');
                    return (T)Convert.ChangeType(rawValue, typeof(T), CultureInfo.InvariantCulture);
                }

                return default(T);
            }
            catch (Exception ex)
            {
                LoggingService.Error($"Error getting attribute value for {argumentName}: {ex.Message}", component: "ReflectionService");
                return default(T);
            }
        }

        private bool TryConvertToDouble(object value, out double result)
        {
            result = 0;
            if (value == null) return false;

            try
            {
                if (value is double d)
                {
                    result = d;
                    return true;
                }

                string strValue = value.ToString().Trim();
                if (double.TryParse(strValue, NumberStyles.Any, CultureInfo.InvariantCulture, out result))
                  {
                    return true;
                }

                return false;
                  }
            catch
            {
            return false;
            }
        }

        private string GetFriendlyName(string variableName)
        {
            if (string.IsNullOrEmpty(variableName)) return string.Empty;

            StringBuilder result = new StringBuilder();
            result.Append(char.ToUpper(variableName[0]));

            for (int i = 1; i < variableName.Length; i++)
             {
                if (char.IsUpper(variableName[i]) && i > 0 && !char.IsUpper(variableName[i - 1]))
                {
                    result.Append(' ');
                }
                result.Append(variableName[i]);
            }

            return result.ToString();
        }
        
        private List<string> ParseStringArray(string arrayString)
        {
            var result = new List<string>();
            
            // Remove @( ) wrapper
            arrayString = arrayString.Trim();
            if (arrayString.StartsWith("@("))
            {
                arrayString = arrayString.Substring(2);
            }
            if (arrayString.EndsWith(")"))
            {
                arrayString = arrayString.Substring(0, arrayString.Length - 1);
            }
            
            // Split by comma and clean up
            var items = arrayString.Split(',');
            foreach (var item in items)
            {
                var cleaned = item.Trim().Trim('\'', '"');
                if (!string.IsNullOrEmpty(cleaned))
                {
                    result.Add(cleaned);
                }
            }
            
            return result;
        }
    }
} 