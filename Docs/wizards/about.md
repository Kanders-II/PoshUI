# About Wizards

Wizards in PoshUI are designed for step-by-step data collection and guided workflows. They allow you to break down complex tasks into logical, manageable steps for your users.

![Wizard Dark Theme](../images/visualization/Wizard_Dark_.png)

## Key Features

- **Step-by-Step Navigation**: Guide users through a sequence of pages.
- **Rich Control Set**: Use over 12 different input controls including text boxes, dropdowns, and date pickers.
- **Live Execution Mode**: Execute PowerShell code in real-time during the wizard and show the output to the user.
- **Validation**: Ensure data integrity with built-in validation patterns and mandatory fields.
- **Dynamic Content**: Refresh controls based on user input (cascading dropdowns).
- **Custom Branding**: Personalize the wizard with your own titles, icons, and sidebar text.

## When to Use Wizards

Wizards are ideal for:
- **Provisioning**: Server setup, VM creation, or user onboarding.
- **Deployments**: Guided application or database deployments.
- **Configurations**: Complex settings that require multiple inputs.
- **Self-Service**: Tools for help desk or non-technical staff to perform automated tasks safely.

## Workflow

A typical PoshUI Wizard follows this pattern:

1. **Initialize**: Use `New-PoshUIWizard` to start a new definition.
2. **Define Steps**: Add one or more steps using `Add-UIStep`.
3. **Add Controls**: Populate each step with controls like `Add-UITextBox` or `Add-UIDropdown`.
4. **Execute**: Display the UI using `Show-PoshUIWizard`.
5. **Process**: Receive the user's input as a PowerShell object for further processing.

Next: [Creating Wizards](./creating-wizards.md)
