*not to be confused with location loader? also by Uncanny_Valley but this is for Inside buildings mostly?

https://forums.dfworkshop.net/viewtopic.php?f=14&t=3217
Uncanny_Valley
I'm releasing my Location Editor here as is. It's far from finished, but works very well at editing existing interior locations. Taverns, shops, homes, temples and guilds (blockName.RMB-nn-buildingX.json files).

Basic Instructions
Download the unity package and import it into the Daggerfall Unity project
Once installed, you will find a new tool under Daggerfall Tools called "Location Editor - Interior"
I highly recommend dragging the opened Location editor window to the inspector view, so it creates an extra tab for you to view it instead of the inspector. The editor was designed to be used in this way
Place your edited location file(s) (.json) in the WorldData folder in streamingAssets to see your changes in the game. (NOTE: the file name must remain the same as the original)
If you later wish to distribute your edited interior(s), you can package the files into a mod

My hope is that sharing this editor now might encourage someone to start a shop or temple re-design project.

Notes:
When you open a location it will load all the objects into your currently opened scene in Unity. 3D objects can be moved, rotated and scaled. Billboards (flats) can only be moved.
When selecting an object in the scene, the editor will show the available "options" for that objects. Selecting a wooden box for example gives you the option to enable/disable it as a container
Select the parent object called (Location : "File Name" ) in the Hierarchy to edit special option for the location such as Building Type
If you wish to add new objects to the location, click the "Add object" button. You can add objects from pre-made lists or add them "manually" by typing in their ID number
Some objects, such as painting, are grouped together. When selecting these objects in the scene view, the editor will give you the option to quickly cycle through the different variances of that objects. You will also notice that when adding new objects from a list, some of them are displayed as "ObjectName"[Number], to indicate that they are grouped together
When moving and rotating objects, you will probably notice that objects will slightly "snap" into place. This is because Daggerfall objects location and rotation is stored with less precision then Unity, so this "snapping" will make sure that any object you move or rotate in the editor will have the exact same position/rotation in the game
To close down a current open location you can either delete the parent object or close down the editor window (as in right click and choose "Close Tab"). NOTE: Do not forget to save before you do this or your changes will be lost!