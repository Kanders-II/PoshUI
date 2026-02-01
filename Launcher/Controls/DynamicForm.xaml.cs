// Copyright (c) 2025 A Solution IT LLC. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Launcher.Controls
{
    public partial class DynamicForm : UserControl
    {
        public static readonly DependencyProperty FormDataProperty =
            DependencyProperty.Register("FormData", typeof(Dictionary<string, object>), 
                typeof(DynamicForm), new PropertyMetadata(new Dictionary<string, object>()));

        public static readonly DependencyProperty MetadataProperty =
            DependencyProperty.Register("Metadata", typeof(Dictionary<string, object>), 
                typeof(DynamicForm), new PropertyMetadata(null, OnMetadataChanged));

        public Dictionary<string, object> FormData
        {
            get { return (Dictionary<string, object>)GetValue(FormDataProperty); }
            set { SetValue(FormDataProperty, value); }
        }

        public Dictionary<string, object> Metadata
        {
            get { return (Dictionary<string, object>)GetValue(MetadataProperty); }
            set { SetValue(MetadataProperty, value); }
        }

        public DynamicForm()
        {
            InitializeComponent();
        }

        private static void OnMetadataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var form = (DynamicForm)d;
            form.GenerateForm();
        }

        private void GenerateForm()
        {
            FormContainer.Children.Clear();
            if (Metadata == null) return;

            foreach (var param in Metadata)
            {
                var fieldInfo = param.Value as Dictionary<string, object>;
                if (fieldInfo == null) continue;

                // Create field label
                var label = new TextBlock
                {
                    Text = (string)fieldInfo["Label"] ?? param.Key,
                    Margin = new Thickness(0, 0, 0, 5)
                };
                FormContainer.Children.Add(label);

                // Create input control based on type
                FrameworkElement input = CreateInputControl(param.Key, fieldInfo);
                if (input != null)
                {
                    input.Margin = new Thickness(0, 0, 0, 15);
                    FormContainer.Children.Add(input);
                }

                // Add help text if available
                if (fieldInfo.ContainsKey("HelpText") && fieldInfo["HelpText"] != null)
                {
                    var helpText = new TextBlock
                    {
                        Text = (string)fieldInfo["HelpText"],
                        Foreground = (System.Windows.Media.Brush)FindResource("SecondaryBrush"),
                        FontSize = 12,
                        Margin = new Thickness(0, -10, 0, 15)
                    };
                    FormContainer.Children.Add(helpText);
                }
            }
        }

        private FrameworkElement CreateInputControl(string paramName, Dictionary<string, object> fieldInfo)
        {
            var type = (string)fieldInfo["Type"];
            FrameworkElement control;

            string lowerType = type != null ? type.ToLower() : null;
            switch (lowerType)
            {
                case "choice":
                    var comboBox = new ComboBox();
                    if (fieldInfo.ContainsKey("ValidValues"))
                    {
                        foreach (var value in (string[])fieldInfo["ValidValues"])
                        {
                            comboBox.Items.Add(value);
                        }
                    }
                    control = comboBox;
                    break;

                case "password":
                    control = new PasswordBox();
                    break;

                case "multiline":
                    control = new TextBox { AcceptsReturn = true, Height = 100 };
                    break;

                default:
                    var textBox = new TextBox();
                    if (fieldInfo.ContainsKey("Placeholder"))
                    {
                        textBox.Text = (string)fieldInfo["Placeholder"];
                    }
                    control = textBox;
                    break;
            }

            // Set up binding
            if (control is TextBox)
            {
                var textBox = (TextBox)control;
                var bindingPath = string.Format("FormData[{0}]", paramName);
                var binding = new Binding(bindingPath)
                {
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                };
                textBox.SetBinding(TextBox.TextProperty, binding);
            }

            return control;
        }
    }
} 