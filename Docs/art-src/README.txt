# Diagram sources

The guide's figures are drawn by these scripts (WPF, no extra tools). Run from Windows PowerShell with -STA:

- `Make-GuideArt.ps1 -OutDir ..\public\launchpad` draws two diagrams: two-worlds and step-kinds.
- `Make-GuideShots.ps1 -OutDir ..\public\launchpad -Shots <folder>` draws script-anatomy, decide-later and
  passing-values. The first two are numbered callouts on real Launchpad screenshots (testport.png,
  publish-waiting.png, publish-approve.png, captured at 150% from a clean %LOCALAPPDATA%). It paints over the
  package path in the output so no user name shows.
- `Make-RouteMap.ps1 -Out <png>` draws the troubleshooter's route map (also used in the Launchpad example).
