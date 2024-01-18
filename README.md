# SizeScanner
My .net vision on Steffen Gerlach's Scanner2

Scans drive and presents it's contents as multi-level pie (doughnut) chart.
Differences from Scanner2:
  * Reports inaccessible data size and affected folders
  * Correctly handles files symlinks
    * Including offline simlinks - e.g. OneDrive "online-only" files
  * Allows fine tuning the layout
  * Scans drives only when told to do so

Functionality is limited but it does all that's necessary.
Console only for testing. 
WinForms - core functions work. Includes quick navigation and from-app delete shortcuts.


![](https://raw.githubusercontent.com/AgentMC/SizeScanner/master/Img/SSSS01.png)
