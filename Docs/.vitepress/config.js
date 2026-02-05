export default {
  title: 'PoshUI',
  description: 'Build beautiful PowerShell wizards and dashboards',
  base: '/PoshUI/',
  appearance: 'dark',
  vite: {
    server: {
      host: '0.0.0.0',
      port: 5173
    }
  },
  themeConfig: {
    logo: '/logo.png',
    siteTitle: false,
    nav: [
      { text: 'Home', link: '/' },
      { text: 'Get Started', link: '/get-started' },
      { text: 'Cmdlet Reference', link: '/cmdlet-reference' }
    ],
    sidebar: [
      {
        text: 'Introduction',
        items: [
          { text: 'About', link: '/' },
          { text: 'Key Features', link: '/key-features' },
          { text: 'Get Started', link: '/get-started' },
          { text: 'Installation', link: '/installation' },
          { text: 'Licensing', link: '/licensing' },
          { text: 'System Requirements', link: '/system-requirements' },
          { text: 'Cmdlet Reference', link: '/cmdlet-reference' }
        ]
      },
      {
        text: 'Wizards',
        items: [
          { text: 'About', link: '/wizards/about' },
          { text: 'Creating Wizards', link: '/wizards/creating-wizards' },
          { text: 'Steps', link: '/wizards/steps' },
          { text: 'Branding', link: '/wizards/branding' },
          { text: 'Execution', link: '/wizards/execution' },
          { text: 'Results', link: '/wizards/results' }
        ]
      },
      {
        text: 'Dashboards',
        items: [
          { text: 'About', link: '/dashboards/about' },
          { text: 'Creating Dashboards', link: '/dashboards/creating-dashboards' },
          { text: 'Dashboard Cards Reference', link: '/dashboard-cards-reference' },
          { text: 'MetricCard', link: '/visualization/metric-cards' },
          { text: 'GraphCard', link: '/visualization/graph-cards' },
          { text: 'DataGridCard', link: '/visualization/datagrid-cards' },
          { text: 'ScriptCard', link: '/visualization/script-cards' },
          { text: 'Live Refresh', link: '/dashboards/refresh' },
          { text: 'Categories', link: '/dashboards/categories' }
        ]
      },
      {
        text: 'Workflows',
        items: [
          { text: 'About', link: '/workflows/about' },
          { text: 'Creating Workflows', link: '/workflows/creating-workflows' },
          { text: 'Working with Tasks', link: '/workflows/tasks' },
          { text: 'Progress Reporting', link: '/workflows/progress-reporting' },
          { text: 'Data Passing Patterns', link: '/workflows/data-passing' },
          { text: 'Reboot & Resume', link: '/workflows/reboot-resume' },
          { text: 'Workflow Logging', link: '/workflows/logging' }
        ]
      },
      {
        text: 'Controls',
        items: [
          { text: 'About', link: '/controls/about' },
          { text: 'Text Controls', link: '/controls/text-controls' },
          { text: 'Selection Controls', link: '/controls/selection-controls' },
          { text: 'Boolean Controls', link: '/controls/boolean-controls' },
          { text: 'Numeric & Date', link: '/controls/numeric-date-controls' },
          { text: 'Path Controls', link: '/controls/path-controls' },
          { text: 'Display Controls', link: '/controls/display-controls' },
          { text: 'Dynamic Controls', link: '/controls/dynamic-controls' }
        ]
      },
      {
        text: 'Platform',
        items: [
          { text: 'Architecture', link: '/platform/architecture' },
          { text: 'Module Structure', link: '/platform/module-structure' },
          { text: 'Security', link: '/platform/security' },
          { text: 'Logging', link: '/platform/logging' },
          { text: 'Theming', link: '/platform/theming' },
          { text: 'Validation', link: '/platform/validation' }
        ]
      },
      {
        text: 'Configuration',
        items: [
          { text: 'Branding', link: '/configuration/branding' },
          { text: 'Icons', link: '/configuration/icons' },
          { text: 'Best Practices', link: '/configuration/best-practices' }
        ]
      },
      {
        text: 'Development',
        items: [
          { text: 'Building from Source', link: '/development/building-from-source' },
          { text: 'Debugging', link: '/development/debugging' }
        ]
      },
      {
        text: 'Examples',
        items: [
          { text: 'All Controls Demo', link: '/examples/demo-all-controls' },
          { text: 'Hyper-V VM Creation', link: '/examples/demo-hyperv' },
          { text: 'Dynamic Controls', link: '/examples/demo-dynamic' },
          { text: 'Dashboard Demo', link: '/examples/demo-dashboard' },
          { text: 'Real-World Scenarios', link: '/examples/real-world-scenarios' }
        ]
      },
      {
        text: 'Troubleshooting',
        items: [
          { text: 'Troubleshooting Guide', link: '/troubleshooting' }
        ]
      }
    ],
    socialLinks: [
      { icon: 'github', link: 'https://github.com/asolutionit/PoshUI' }
    ]
  }
}
