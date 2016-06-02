# filename-in-gui
Tridion GUI Extension to put the Filename in it's own column in the GUI

 Deployment Instructions
 1. Build project, copy DLLs to 'C:\Tridion\web\WebUI\WebRoot\bin'
 2. Create folder 'AddFilename' for GUI Extension config file in 'C:\Tridion\web\WebUI\WebRoot\Editors' 
 3. Create GUI Configuration File and then copy to 'C:\PTridion\web\WebUI\WebRoot\Editors\AddFilename'.  See example in this project.
 4. Update Tridion System.config to enable GUI Extension, 'C:\Tridion\web\WebUI\WebRoot\Configuration'
 <editor name="AddFilename">
<!-- CHANGE THIS PATH -->
  <installpath>C:\Tridion\web\WebUI\WebRoot\Editors\AddFilename</installpath>
  <configuration>AddFilename.config</configuration>
  <vdir/>
</editor>

Implementation:
Create Structure Group Metadata field 'DisableFilenameColumn'
checkbox Value:
- 'yes'

If yes, then no filenames will be shown and each structure group will not be opened by the extension to check the filename.  This will make things faster.
