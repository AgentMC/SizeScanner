# SizeScanner
My .net vision on Steffen Gerlach's Scanner2

Scans drive and presents it's contents as multi-level pie (doughnut) chart.
Differences from Scanner2:
  * Reports inaccessible data size and affected folders
  * Correctly handles files symlinks
    * Including offline simlinks - e.g. OneDrive "online-only" files
  * Allows fine tuning the layout
  * Scans drives only when told to do so

Alpha state.
Console only for testing. WPF - not implemented chart control. WinForms - core functions work.

Context menus, FS actions - all planned.
![](https://raw.githubusercontent.com/AgentMC/SizeScanner/master/Img/SSSS01.png)
