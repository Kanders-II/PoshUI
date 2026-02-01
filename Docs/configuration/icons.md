# Icons Reference

PoshUI uses the **Segoe MDL2 Assets** icon set for all its UI elements. These icons are built into Windows and provide a professional, native appearance for your wizards, dashboards, and workflows.

## How to Use Icons

Icons are specified using their HTML entity code (e.g., `&#xE710;`). You can apply them to:
- **Steps**: In the sidebar navigation.
- **Header**: In the branding section.
- **Cards**: In banners, visualization cards, and script cards.

```powershell
# Example: Adding a step with a Home icon
Add-UIStep -Name 'Home' -Title 'Introduction' -Icon '&#xE8BC;'
```

## Common Icons

| Icon | Glyph | Code | Description |
|------|-------|------|-------------|
| **Home** | &#xE8BC; | `&#xE8BC;` | Landing or welcome steps |
| **Settings** | &#xE713; | `&#xE713;` | Configuration or setup |
| **User** | &#xE77B; | `&#xE77B;` | User accounts or permissions |
| **Server** | &#xEB51; | `&#xEB51;` | Infrastructure or hosts |
| **Database** | &#xE1D3; | `&#xE1D3;` | SQL or data storage |
| **Network** | &#xE968; | `&#xE968;` | Connectivity or firewall |
| **Security** | &#xE72E; | `&#xE72E;` | Encryption or auditing |
| **Cloud** | &#xE753; | `&#xE753;` | Azure or remote services |
| **Clock** | &#xE823; | `&#xE823;` | Scheduling or history |
| **Success** | &#xE73E; | `&#xE73E;` | Completion or healthy state |
| **Warning** | &#xE7BA; | `&#xE7BA;` | Alerts or important notices |
| **Error** | &#xE783; | `&#xE783;` | Failures or critical issues |

## Dashboard-Specific Icons

| Icon | Glyph | Code | Description |
|------|-------|------|-------------|
| **CPU** | &#xE7C4; | `&#xE7C4;` | Processor metrics |
| **RAM** | &#xE8B7; | `&#xE8B7;` | Memory metrics |
| **Storage** | &#xEDA2; | `&#xEDA2;` | Disk space or I/O |
| **Globe** | &#xE12B; | `&#xE12B;` | Web or public traffic |

## Finding More Icons

You can find the full list of available icons and their codes on the [Microsoft Segoe MDL2 Assets documentation](https://docs.microsoft.com/en-us/windows/uwp/design/style/segoe-ui-symbol-font).

::: tip
When browsing the Microsoft documentation, take the **Unicode hex value** (e.g., `E710`) and wrap it in the HTML entity format: `&#x<VALUE>;` (e.g., `&#xE710;`).
:::

Next: [Best Practices](./best-practices.md)
