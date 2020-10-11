using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using DaggerfallWorkshop.Utility.AssetInjection;
using System.IO;

namespace DaggerfallWorkshop.Game.Utility.LocationEditor
{
    /// <summary>
    /// Location editor
    /// </summary>
    public class LocationEditorInterior : EditorWindow
    {
        private GUIStyle whiteBG = new GUIStyle();
        private GUIStyle lightGrayBG = new GUIStyle();
        private GUIStyle bigText = new GUIStyle();

        private void CreataGUIStyles()
        {
            whiteBG.normal.background = LocationEditorHelper.CreateColorTexture(1, 1, new Color(0.98f, 0.98f, 0.98f, 1.0f));
            lightGrayBG.normal.background = LocationEditorHelper.CreateColorTexture(1, 1, new Color(0.9f, 0.9f, 0.9f, 1.0f));
            bigText.fontSize = 24;
        }

        private enum EditMode { EditMode, AddItem };
        private GameObject parent, exteriorPlane;
        private string searchField = "", selectedObjectID = "";
        private List<string> searchListNames = new List<string>(), searchListID = new List<string>();
        private EditMode editMode;
        private int objectPicker, chooseFileMode, listMode, billboardSubList, modelSubList;
        private Vector2 scrollPosition2 = Vector2.zero;
        private string[] chooseFileModeString = { "Choose from List", "Add manually" };
        private string[] listModeString = { "3D Model", "Billboard", "NPC", "Door", "Int. Parts" };
        private string[] modelSubListString = { "Civil", "Nature", "Dungeon" , "Signs"};
        private string[] billboardSubListString = { "Interior", "Nature", "Lights", "Treasure", "Markers" };
        private bool isExteriorMode = false;
        private BuildingReplacementData levelData;
        private string currentWorkFile;

        [MenuItem("Daggerfall Tools/Location Editor - Interior")]
        static void Init()
        {
            LocationEditorInterior window = (LocationEditorInterior)GetWindow(typeof(LocationEditorInterior));
            window.titleContent = new GUIContent("Location Editor - Interior");
        }

        private void Awake()
        {
            CreataGUIStyles();
            UpdateSearchList();
        }

        void OnGUI()
        {
            GUIFileMenu();
            if (parent != null)
            {
                if (editMode == EditMode.EditMode)
                {
                    EditInteriorWindow();
                }

                else if (editMode == EditMode.AddItem)
                {
                    AddItemWindow();
                }
            }
        }

        private void GUIFileMenu()
        {
            GUI.BeginGroup(new Rect(8, 8, Screen.width - 16, 48), whiteBG);
            {
                if (GUI.Button(new Rect(8, 8, 96, 32), "Open File"))
                {
                    bool newFile = false;

                    if (parent != null)
                    {
                        newFile = EditorUtility.DisplayDialog("Open New File ?", "Are you sure you wish to open a NEW file, all current unsaved changes will be lost!", "Ok", "Cancel");
                    }

                    if (newFile || parent == null)
                    {
                        OpenFile();
                    }
                }

                else if (parent != null && GUI.Button(new Rect(128, 8, 96, 32), "Save File"))
                {
                    UpdateLevelData();
                    string path = EditorUtility.SaveFilePanel("Save as", LocationEditorHelper.locationFolder, currentWorkFile, "json");


                    //Loop through all doors to give them a unique position ID
                    for (int i = 0, doorID = 0; i < levelData.RmbSubRecord.Interior.BlockDoorRecords.Length; i++)
                    {
                        levelData.RmbSubRecord.Interior.BlockDoorRecords[i].Position = doorID;
                        doorID++;
                    }

                    //Loop through all NPCS to give them a unique position ID
                    for (int i = 0, NPC_ID = 0; i < levelData.RmbSubRecord.Interior.BlockPeopleRecords.Length; i++)
                    {
                        levelData.RmbSubRecord.Interior.BlockPeopleRecords[i].Position = NPC_ID;
                        NPC_ID++;
                    }

                    for (int i = 0; i < levelData.RmbSubRecord.Exterior.BlockSection3Records.Length; i++)
                    {
                        levelData.RmbSubRecord.Exterior.Block3dObjectRecords[i].ObjectType = 4;
                    }

                    LocationEditorHelper.SaveInterior(levelData, path);
                }
            }
            GUI.EndGroup();
        }

        private void LoadObjects()
        {
            foreach (var blockRecord in levelData.RmbSubRecord.Interior.Block3dObjectRecords)
            {
                Create3DObject(blockRecord);
            }
            foreach (var blockRecord in levelData.RmbSubRecord.Interior.BlockFlatObjectRecords)
            {
                CreateFlatObject(blockRecord);
            }
            foreach (var blockRecord in levelData.RmbSubRecord.Interior.BlockPeopleRecords)
            {
                GameObject go = LocationEditorHelper.AddPersonObject(blockRecord);
                go.transform.parent = parent.transform;
                go.AddComponent<LocationEditorObject>().CreateData(blockRecord, false);
            }
            foreach (var blockRecord in levelData.RmbSubRecord.Interior.BlockDoorRecords)
            {
                GameObject go = LocationEditorHelper.AddDoorObject(blockRecord);
                go.transform.parent = parent.transform;
                go.AddComponent<LocationEditorObject>().CreateData(blockRecord, false);
            }
            foreach (var blockRecord in levelData.RmbSubRecord.Exterior.Block3dObjectRecords)
            {
                GameObject go = LocationEditorHelper.Add3dObject(blockRecord);
                go.transform.parent = parent.transform;
                go.AddComponent<LocationEditorObject>().CreateData(blockRecord, true);
            }
            foreach (var blockRecord in levelData.RmbSubRecord.Exterior.BlockFlatObjectRecords)
            {
                GameObject go = LocationEditorHelper.AddFlatObject(blockRecord);
                go.transform.parent = parent.transform;
                go.AddComponent<LocationEditorObject>().CreateData(blockRecord, true);
            }
            foreach (var blockRecord in levelData.RmbSubRecord.Exterior.BlockPeopleRecords)
            {
                GameObject go = LocationEditorHelper.AddPersonObject(blockRecord);
                go.transform.parent = parent.transform;
                go.AddComponent<LocationEditorObject>().CreateData(blockRecord, true);
            }
            foreach (var blockRecord in levelData.RmbSubRecord.Exterior.BlockDoorRecords)
            {
                GameObject go = LocationEditorHelper.AddDoorObject(blockRecord);
                go.transform.parent = parent.transform;
                go.AddComponent<LocationEditorObject>().CreateData(blockRecord, true);
            }
            UpdateObjectsVisibility();
        }

        private void UpdateObjectsVisibility()
        {
            foreach (Transform child in parent.GetComponentInChildren<Transform>())
            {
                if (child.GetComponent<LocationEditorObject>() == null)
                    return;
                child.GetComponent<LocationEditorObject>().UpdateVisibility(isExteriorMode);
            }

            if(isExteriorMode)
            {

            }
        }

        private void EditInteriorWindow()
        {
            Repaint(); //Keep this repainted

            int elementIndex = 0;

            GUI.BeginGroup(new Rect(8, 64, Screen.width - 16, 48), whiteBG);
            {
                if (GUI.Button(new Rect(8, 8, 96, 32), "Add Object"))
                {
                    editMode = EditMode.AddItem;
                }

                else if (parent != null && GUI.Button(new Rect(128, 8, 196, 32), "Toggle Interior/Exterior"))
                {
                    isExteriorMode = !isExteriorMode;
                    UpdateObjectsVisibility();
                }
            }
            GUI.EndGroup();

            if (Selection.activeGameObject == null)
                return;

            if (Selection.Contains(parent))
            {
                GUI.Label(new Rect(16, 128, 96, 28), "XPos: ");
                levelData.RmbSubRecord.XPos = EditorGUI.IntField(new Rect(128, 128, 128, 16), levelData.RmbSubRecord.XPos);

                GUI.Label(new Rect(16, 150, 96, 28), "YPos: ");
                levelData.RmbSubRecord.ZPos = EditorGUI.IntField(new Rect(128, 150, 128, 16), levelData.RmbSubRecord.ZPos);

                GUI.Label(new Rect(16, 182, 96, 28), "YRotation: ");
                levelData.RmbSubRecord.YRotation = EditorGUI.IntField(new Rect(128, 182, 128, 16), levelData.RmbSubRecord.YRotation);

                GUI.Label(new Rect(16, 214, 96, 28), "Building Type: ");
                levelData.BuildingType = (int)(DaggerfallConnect.DFLocation.BuildingTypes)EditorGUI.EnumPopup(new Rect(128, 214, 128, 16), (DaggerfallConnect.DFLocation.BuildingTypes)levelData.BuildingType);

                GUI.Label(new Rect(16, 236, 96, 28), "Faction ID: ");
                levelData.FactionId = (ushort)EditorGUI.IntField(new Rect(128, 236, 128, 16), levelData.FactionId);

                GUI.Label(new Rect(16, 258, 96, 28), "Quality: ");
                levelData.Quality = (byte)EditorGUI.IntSlider(new Rect(128, 258, 128, 16), levelData.Quality, 1, 20);
            }

            else
            {
                if (Selection.activeGameObject.GetComponent<LocationEditorObject>())
                {
                    LocationEditorObject data = Selection.activeGameObject.GetComponent<LocationEditorObject>();
                 
                    if (data.type == (int)LocationEditorHelper.DataType.Object3D)
                    {
                        GUIElementPosition(ref data, ref elementIndex);
                        GUIElementRotation(ref data, ref elementIndex);
                        GUIElementScale(ref data, ref elementIndex);

                        if (isExteriorMode)
                        {
                            return;
                        }

                        if (data.objectType == LocationEditorHelper.InteriorHousePart)
                        {
                            GUIElementSwitchSet(ref data, ref elementIndex);
                        }
                        else if (data.id == LocationEditorHelper.ladder.ToString())
                        {
                            GUIElementObjectType(ref data, ref elementIndex, "Is Climbable");
                        }

                        else if (LocationEditorHelper.shopShelvesObjectGroupIndices.Contains(uint.Parse(data.id) - LocationEditorHelper.containerObjectGroupOffset))
                        {
                            GUIElementObjectType(ref data, ref elementIndex, "Is Container");
                        }

                        else if (uint.Parse(data.id) / 100 == LocationEditorHelper.houseContainerObjectGroup ||
                        LocationEditorHelper.houseContainerObjectGroupIndices.Contains(uint.Parse(data.id) - LocationEditorHelper.containerObjectGroupOffset))
                        {
                            GUIElementObjectType(ref data, ref elementIndex, "Is Container");
                        }

                        if (ArrayUtility.Contains(LocationEditorHelper.paintings, data.id.ToString()))
                        {
                            GUIElement3DSwitchType(ref data, ref elementIndex, LocationEditorHelper.paintings);
                        }
                        else if (ArrayUtility.Contains(LocationEditorHelper.carpets, data.id.ToString()))
                        {
                            GUIElement3DSwitchType(ref data, ref elementIndex, LocationEditorHelper.carpets);
                        }
                        else if (ArrayUtility.Contains(LocationEditorHelper.crates, data.id.ToString()))
                        {
                            GUIElement3DSwitchType(ref data, ref elementIndex, LocationEditorHelper.crates);
                        }
                        else if (ArrayUtility.Contains(LocationEditorHelper.tables, data.id.ToString()))
                        {
                            GUIElement3DSwitchType(ref data, ref elementIndex, LocationEditorHelper.tables);
                        }
                        else if (ArrayUtility.Contains(LocationEditorHelper.woodenTreeLog, data.id.ToString()))
                        {
                            GUIElement3DSwitchType(ref data, ref elementIndex, LocationEditorHelper.woodenTreeLog);
                        }
                        else if (ArrayUtility.Contains(LocationEditorHelper.chests, data.id.ToString()))
                        {
                            GUIElement3DSwitchType(ref data, ref elementIndex, LocationEditorHelper.chests);
                        }
                        else if (ArrayUtility.Contains(LocationEditorHelper.fountains, data.id.ToString()))
                        {
                            GUIElement3DSwitchType(ref data, ref elementIndex, LocationEditorHelper.fountains);
                        }
                        else if (ArrayUtility.Contains(LocationEditorHelper.ships, data.id.ToString()))
                        {
                            GUIElement3DSwitchType(ref data, ref elementIndex, LocationEditorHelper.ships);
                        }
                        else if (ArrayUtility.Contains(LocationEditorHelper.beds, data.id.ToString()))
                        {
                            GUIElement3DSwitchType(ref data, ref elementIndex, LocationEditorHelper.beds);
                        }
                        else if (ArrayUtility.Contains(LocationEditorHelper.chair, data.id.ToString()))
                        {
                            GUIElement3DSwitchType(ref data, ref elementIndex, LocationEditorHelper.chair);
                        }
                        else if (ArrayUtility.Contains(LocationEditorHelper.bench, data.id.ToString()))
                        {
                            GUIElement3DSwitchType(ref data, ref elementIndex, LocationEditorHelper.bench);
                        }
                        else if (ArrayUtility.Contains(LocationEditorHelper.sword, data.id.ToString()))
                        {
                            GUIElement3DSwitchType(ref data, ref elementIndex, LocationEditorHelper.sword);
                        }
                        else if (ArrayUtility.Contains(LocationEditorHelper.brownStoneWallPiece, data.id.ToString()))
                        {
                            GUIElement3DSwitchType(ref data, ref elementIndex, LocationEditorHelper.brownStoneWallPiece);
                        }
                    }                  
                    else if (data.type == (int)LocationEditorHelper.DataType.Flat) //Flat object
                    {
                        GUIElementPosition(ref data, ref elementIndex);
                        GUIElementFaction(ref data, ref elementIndex);
                        GUIElementFlag(ref data, ref elementIndex);

                        if (ArrayUtility.Contains(LocationEditorHelper.bottles, data.id.ToString()))
                        {
                            GUIElementFlatSwitchType(ref data, ref elementIndex, LocationEditorHelper.bottles);
                        }
                        else if (ArrayUtility.Contains(LocationEditorHelper.pottedPlants, data.id.ToString()))
                        {
                            GUIElementFlatSwitchType(ref data, ref elementIndex, LocationEditorHelper.pottedPlants);
                        }
                        else if (ArrayUtility.Contains(LocationEditorHelper.pots, data.id.ToString()))
                        {
                            GUIElementFlatSwitchType(ref data, ref elementIndex, LocationEditorHelper.pots);
                        }
                        else if (ArrayUtility.Contains(LocationEditorHelper.grainBags, data.id.ToString()))
                        {
                            GUIElementFlatSwitchType(ref data, ref elementIndex, LocationEditorHelper.grainBags);
                        }
                        else if (ArrayUtility.Contains(LocationEditorHelper.flowers, data.id.ToString()))
                        {
                            GUIElementFlatSwitchType(ref data, ref elementIndex, LocationEditorHelper.flowers);
                        }
                        else if (ArrayUtility.Contains(LocationEditorHelper.chalice, data.id.ToString()))
                        {
                            GUIElementFlatSwitchType(ref data, ref elementIndex, LocationEditorHelper.chalice);
                        }
                        else if (ArrayUtility.Contains(LocationEditorHelper.hangingPlant, data.id.ToString()))
                        {
                            GUIElementFlatSwitchType(ref data, ref elementIndex, LocationEditorHelper.hangingPlant);
                        }
                        else if (ArrayUtility.Contains(LocationEditorHelper.woodPillar, data.id.ToString()))
                        {
                            GUIElementFlatSwitchType(ref data, ref elementIndex, LocationEditorHelper.woodPillar);
                        }
                    }                  
                    else if (data.type == (int)LocationEditorHelper.DataType.Person) //Person object
                    {
                        GUIElementPosition(ref data, ref elementIndex);
                        GUIElementFaction(ref data, ref elementIndex);
                        GUIElementFlag(ref data, ref elementIndex);
                    }                  
                    else if (data.type == (int)LocationEditorHelper.DataType.Door) //Door object
                    {
                        GUIElementPosition(ref data, ref elementIndex);
                        GUIElementDoorRotation(ref data, ref elementIndex);
                    }
                }
                else
                {
                    GUI.TextArea(new Rect(24, 128, 196, 32), "No level object selected", bigText);
                }
            }
        }

        private void GUIElementSwitchSet(ref LocationEditorObject data, ref int elementIndex)
        {

            GUI.BeginGroup(new Rect(8, 120 + (56 * elementIndex), Screen.width - 16, 48), lightGrayBG);
            {
                if (GUI.Button(new Rect(8, 8, 96, 32), "Switch set"))
                {
                    foreach (GameObject obj in Selection.objects)
                    {
                        if (obj.GetComponent<LocationEditorObject>() && obj.GetComponent<LocationEditorObject>().objectType == LocationEditorHelper.InteriorHousePart)
                        {
                            DaggerfallConnect.DFBlock.RmbBlock3dObjectRecord blockRecord = new DaggerfallConnect.DFBlock.RmbBlock3dObjectRecord();
                            LocationEditorObject interiorPiece = obj.GetComponent<LocationEditorObject>();
                            int currentSet = int.Parse(interiorPiece.id[interiorPiece.id.Length - 3].ToString());
                            currentSet++;

                            if (currentSet > 8)
                            {
                                currentSet = 0;
                            }

                            interiorPiece.id = interiorPiece.id.Remove(interiorPiece.id.Length - 3, 1);
                            interiorPiece.id = interiorPiece.id.Insert(interiorPiece.id.Length - 2, currentSet.ToString());
                            blockRecord.ModelIdNum = uint.Parse(interiorPiece.id);
                            blockRecord.ModelId = interiorPiece.id;
                            blockRecord.ObjectType = interiorPiece.objectType;
                            GameObject tempGo = LocationEditorHelper.Add3dObject(blockRecord);

                            interiorPiece.gameObject.GetComponent<MeshRenderer>().sharedMaterials = tempGo.GetComponent<MeshRenderer>().sharedMaterials;
                            DestroyImmediate(tempGo);
                        }
                    }
                }
            }
            elementIndex++;
            GUI.EndGroup();
        }

        private void GUIElementPosition(ref LocationEditorObject data, ref int elementIndex)
        {

            GUI.BeginGroup(new Rect(8, 120 + (56 * elementIndex), Screen.width - 16, 48), lightGrayBG);
            {
                data.transform.localPosition = EditorGUI.Vector3Field(new Rect(32, 4, 312, 32), "Position", data.transform.localPosition); ;
            }
            elementIndex++;
            GUI.EndGroup();
        }

        private void GUIElementRotation(ref LocationEditorObject data, ref int elementIndex)
        {
            GUI.BeginGroup(new Rect(8, 120 + (56 * elementIndex), Screen.width - 16, 48), lightGrayBG);
            {
                data.transform.eulerAngles = EditorGUI.Vector3Field(new Rect(32, 4, 312, 32), "Rotation", data.transform.eulerAngles);
            }
            elementIndex++;
            GUI.EndGroup();
        }

        private void GUIElementScale(ref LocationEditorObject data, ref int elementIndex)
        {
            GUI.BeginGroup(new Rect(8, 120 + (56 * elementIndex), Screen.width - 16, 48), lightGrayBG);
            {
                data.transform.localScale = EditorGUI.Vector3Field(new Rect(32, 4, 312, 32), "Scale", data.transform.localScale);
            }
            elementIndex++;
            GUI.EndGroup();
        }

        private void GUIElementObjectType(ref LocationEditorObject data, ref int elementIndex, string interactionName)
        {
            GUI.BeginGroup(new Rect(8, 120 + (56 * elementIndex), Screen.width - 16, 48), lightGrayBG);
            {
                bool isInteractable = (data.objectType == LocationEditorHelper.InteractiveObject);
                GUI.Label(new Rect(8, 16, 96, 16), interactionName + " : ");
                isInteractable = GUI.Toggle(new Rect(96, 16, 32, 32), isInteractable, "");
                data.objectType = isInteractable ? LocationEditorHelper.InteractiveObject : (byte)0;
            }
            elementIndex++;
            GUI.EndGroup();
        }

        private void GUIElementFaction(ref LocationEditorObject data, ref int elementIndex)
        {
            GUI.BeginGroup(new Rect(8, 120 + (56 * elementIndex), Screen.width - 16, 48), lightGrayBG);
            {
                GUI.Label(new Rect(8, 16, 96, 16), "Faction ID: ");
                data.factionID = (short)EditorGUI.IntField(new Rect(96, 16, 96, 16), data.factionID);
            }
            elementIndex++;
            GUI.EndGroup();
        }

        private void GUIElementFlag(ref LocationEditorObject data, ref int elementIndex)
        {
            GUI.BeginGroup(new Rect(8, 120 + (56 * elementIndex), Screen.width - 16, 48), lightGrayBG);
            {
                GUI.Label(new Rect(8, 16, 96, 16), "Flags : ");
                data.flags = (byte)EditorGUI.IntField(new Rect(96, 16, 96, 16), data.flags);
            }
            elementIndex++;
            GUI.EndGroup();
        }

        private void GUIElementDoorRotation(ref LocationEditorObject data, ref int elementIndex)
        {
            GUI.BeginGroup(new Rect(8, 120 + (56 * elementIndex), Screen.width - 16, 48), lightGrayBG);
            {
                GUI.Label(new Rect(8, 16, 96, 16), "Open Rotation : ");
                data.openRotation = (byte)EditorGUI.IntField(new Rect(96, 16, 96, 16), data.openRotation);
            }
            elementIndex++;
            GUI.EndGroup();
        }

        private void GUIElement3DSwitchType(ref LocationEditorObject data, ref int elementIndex, string[] tempArray)
        {
            int pos = ArrayUtility.IndexOf(tempArray, data.id.ToString());

            GUI.BeginGroup(new Rect(8, 120 + (56 * elementIndex), Screen.width - 16, 48), lightGrayBG);
            {
                if (GUI.Button(new Rect(16, 12, 32, 24), "<"))
                {
                    pos--;

                    if (pos < 0)
                        pos = tempArray.Length - 1;
                }

                GUI.Label(new Rect(50, 16, 48, 28), tempArray[pos]);

                if (GUI.Button(new Rect(96, 12, 32, 24), ">"))
                {
                    pos++;

                    if (pos >= tempArray.Length)
                        pos = 0;
                }

                if (pos != ArrayUtility.IndexOf(tempArray, data.id.ToString()))
                {
                    data.id = tempArray[pos];
                    DaggerfallConnect.DFBlock.RmbBlock3dObjectRecord newBlockRecord = new DaggerfallConnect.DFBlock.RmbBlock3dObjectRecord();
                    newBlockRecord.ModelId = data.id;
                    newBlockRecord.ModelIdNum = uint.Parse(data.id);
                    GameObject tempGo = Create3DObject(newBlockRecord);
                    data.gameObject.GetComponent<MeshRenderer>().sharedMaterials = tempGo.GetComponent<MeshRenderer>().sharedMaterials;
                    data.gameObject.GetComponent<MeshFilter>().sharedMesh = tempGo.GetComponent<MeshFilter>().sharedMesh;
                    data.gameObject.GetComponent<MeshCollider>().sharedMesh = tempGo.GetComponent<MeshFilter>().sharedMesh;
                    data.gameObject.name = tempGo.gameObject.name;
                    DestroyImmediate(tempGo);
                }

            }
            elementIndex++;
            GUI.EndGroup();
        }

        private void GUIElementFlatSwitchType(ref LocationEditorObject data, ref int elementIndex, string[] tempArray)
        {
            int pos = ArrayUtility.IndexOf(tempArray, data.id.ToString());

            GUI.BeginGroup(new Rect(8, 120 + (56 * elementIndex), Screen.width - 16, 48), lightGrayBG);
            {
                if (GUI.Button(new Rect(16, 12, 32, 24), "<"))
                {
                    pos--;

                    if (pos < 0)
                        pos = tempArray.Length - 1;
                }

                GUI.Label(new Rect(50, 16, 48, 28), tempArray[pos]);

                if (GUI.Button(new Rect(96, 12, 32, 24), ">"))
                {
                    pos++;

                    if (pos >= tempArray.Length)
                        pos = 0;
                }

                if (pos != ArrayUtility.IndexOf(tempArray, data.id.ToString()))
                {
                    data.id = tempArray[pos];
                    DaggerfallConnect.DFBlock.RmbBlockFlatObjectRecord newBlockRecord = new DaggerfallConnect.DFBlock.RmbBlockFlatObjectRecord();
                    newBlockRecord.TextureArchive = int.Parse(data.id.Split('.')[0]);
                    newBlockRecord.TextureRecord = int.Parse(data.id.Split('.')[1]);
                    GameObject go = CreateFlatObject(newBlockRecord);
                    data.gameObject.GetComponent<MeshRenderer>().sharedMaterials = go.GetComponent<MeshRenderer>().sharedMaterials;
                    data.gameObject.GetComponent<MeshFilter>().sharedMesh = go.GetComponent<MeshFilter>().sharedMesh;
                    data.gameObject.name = go.gameObject.name;
                    DestroyImmediate(go);
                }

            }
            elementIndex++;
            GUI.EndGroup();
        }

        private void AddItemWindow()
        {
            GUI.BeginGroup(new Rect(8, 64, Screen.width - 16, 32 ), lightGrayBG);
            chooseFileMode = GUI.SelectionGrid(new Rect(8, 4, 312, 24), chooseFileMode, chooseFileModeString, 2);
            GUI.EndGroup();

            GUI.BeginGroup(new Rect(8, 104, Screen.width - 16, 32), lightGrayBG);
            listMode = GUI.SelectionGrid(new Rect(8, 4, (listModeString.Length * 80) + 8, 24), listMode, listModeString, listModeString.Length);
            GUI.EndGroup();

            if (chooseFileMode == 0)
            {
                if (listMode == 0)
                {
                    GUI.BeginGroup(new Rect(8, 144, Screen.width - 16, 32), lightGrayBG);
                    modelSubList = GUI.SelectionGrid(new Rect(8, 4, (modelSubListString.Length * 80) + 8, 24), modelSubList, modelSubListString, modelSubListString.Length);
                    GUI.EndGroup();
                }


                else if (listMode == 1)
                {
                    GUI.BeginGroup(new Rect(8, 144, Screen.width - 16, 32), lightGrayBG);
                    billboardSubList = GUI.SelectionGrid(new Rect(8, 4, (billboardSubListString.Length * 80) + 8, 24), billboardSubList, billboardSubListString, billboardSubListString.Length);
                    GUI.EndGroup();
                }

                if (listMode != 3)
                {

                    GUI.Label(new Rect(new Rect(16, 190, 64, 16)), "Search: ");
                    searchField = EditorGUI.TextField(new Rect(70, 190, 156, 16), searchField);

                    if (GUI.changed)
                        UpdateSearchList();

                    scrollPosition2 = GUI.BeginScrollView(new Rect(4, 210, 312, 418), scrollPosition2, new Rect(0, 0, 256, 20 + (searchListNames.Count * 24)));
                    objectPicker = GUI.SelectionGrid(new Rect(10, 10, 256, searchListNames.Count * 24), objectPicker, searchListNames.ToArray(), 1);
                    GUI.EndScrollView();
                }
            }

            else if (chooseFileMode == 1)
            {

                GUI.Label(new Rect(new Rect(16, 160, 96, 16)), "Object ID : ");
                selectedObjectID = EditorGUI.TextField(new Rect(128, 160, 156, 16), selectedObjectID);
            }

            if (GUI.Button(new Rect(16, 642, 96, 20), "OK"))
            {
                if (chooseFileMode == 0)
                    selectedObjectID = searchListID[objectPicker];

                GameObject go = null;

                if (listMode == 0 || listMode == 4)
                {
                    DaggerfallConnect.DFBlock.RmbBlock3dObjectRecord blockRecord = new DaggerfallConnect.DFBlock.RmbBlock3dObjectRecord();
                    
                    blockRecord.XScale = 1;
                    blockRecord.YScale = 1;
                    blockRecord.ZScale = 1;

                    blockRecord.ModelIdNum = uint.Parse(selectedObjectID);

                    if (isExteriorMode)
                    {
                        blockRecord.ObjectType = LocationEditorHelper.ExteriorBuilding;
                    }

                    else if (listMode == 4)
                    {
                        blockRecord.ObjectType = LocationEditorHelper.InteriorHousePart;
                    }

                    blockRecord.ModelId = blockRecord.ModelIdNum.ToString();
                    go = LocationEditorHelper.Add3dObject(blockRecord);

                    //Rotate Carpets
                    if (blockRecord.ModelId == "74800")
                    {
                        go.transform.rotation = Quaternion.Euler(270, 0, 0);
                    }

                    go.AddComponent<LocationEditorObject>().CreateData(blockRecord, isExteriorMode);
                }

                else if (listMode == 1)
                {
                    DaggerfallConnect.DFBlock.RmbBlockFlatObjectRecord blockRecord = new DaggerfallConnect.DFBlock.RmbBlockFlatObjectRecord();
                    blockRecord.TextureArchive = int.Parse(selectedObjectID.Split('.')[0]);
                    blockRecord.TextureRecord = int.Parse(selectedObjectID.Split('.')[1]);
                    go = LocationEditorHelper.AddFlatObject(blockRecord);
                    go.AddComponent<LocationEditorObject>().CreateData(blockRecord, isExteriorMode);

                    // Add point lights
                    if (blockRecord.TextureArchive == DaggerfallWorkshop.Utility.TextureReader.LightsTextureArchive)
                    {
                        LocationEditorHelper.AddLight(go.transform, blockRecord);
                    }
                }

                else if (listMode == 2)
                {
                    DaggerfallConnect.DFBlock.RmbBlockPeopleRecord blockRecord = new DaggerfallConnect.DFBlock.RmbBlockPeopleRecord();
                    blockRecord.TextureArchive = int.Parse(selectedObjectID.Split('.')[0]);
                    blockRecord.TextureRecord = int.Parse(selectedObjectID.Split('.')[1]);
                    go = LocationEditorHelper.AddPersonObject(blockRecord);
                    go.AddComponent<LocationEditorObject>().CreateData(blockRecord, isExteriorMode);
                }

                else if (listMode == 3)
                {
                    DaggerfallConnect.DFBlock.RmbBlockDoorRecord blockRecord = new DaggerfallConnect.DFBlock.RmbBlockDoorRecord();
                    blockRecord.OpenRotation = 95; //Seems to be the default rotation used in the game
                    go = LocationEditorHelper.AddDoorObject(blockRecord);
                    go.AddComponent<LocationEditorObject>().CreateData(blockRecord, isExteriorMode);
                }

                if (go != null)
                {
                    go.transform.parent = parent.transform;

                    Ray newRay = new Ray(SceneView.lastActiveSceneView.camera.transform.position, SceneView.lastActiveSceneView.camera.transform.forward);
                    RaycastHit hit = new RaycastHit();
                    if (Physics.Raycast(newRay, out hit, 200))
                    {
                        go.transform.position = hit.point;
                    }
                }

                editMode = EditMode.EditMode;
            }

            if (GUI.Button(new Rect(128, 642, 96, 20), "Cancel"))
            {
                editMode = EditMode.EditMode;
            }
        }

        private GameObject CreateFlatObject(DaggerfallConnect.DFBlock.RmbBlockFlatObjectRecord blockRecord)
        {
            GameObject go = LocationEditorHelper.AddFlatObject(blockRecord);
            go.transform.parent = parent.transform;
            go.AddComponent<LocationEditorObject>().CreateData(blockRecord, false);

            // Add point lights
            if (blockRecord.TextureArchive == DaggerfallWorkshop.Utility.TextureReader.LightsTextureArchive)
            {
                LocationEditorHelper.AddLight(go.transform, blockRecord);
            }

            return go;
        }

        private GameObject Create3DObject(DaggerfallConnect.DFBlock.RmbBlock3dObjectRecord blockRecord)
        {
            GameObject go = LocationEditorHelper.Add3dObject(blockRecord);
            go.transform.parent = parent.transform;
            go.AddComponent<LocationEditorObject>().CreateData(blockRecord, false);
            return go;
        }

        private void UpdateSearchList()
        {
            searchListNames.Clear();
            searchListID.Clear();
            Dictionary<string, string> currentList;

            if (listMode == 0 && modelSubList == 0)
                currentList = LocationEditorHelper.models_civil;
            else if (listMode == 0 && modelSubList == 1)
                currentList = LocationEditorHelper.models_nature;
            else if (listMode == 0 && modelSubList == 2)
                currentList = LocationEditorHelper.models_dungeon;
            else if (listMode == 0 && modelSubList == 3)
                currentList = LocationEditorHelper.models_signs;
            else if (listMode == 1 && billboardSubList == 0)
                currentList = LocationEditorHelper.billboards_interior;
            else if (listMode == 1 && billboardSubList == 1)
                currentList = LocationEditorHelper.billboards_nature;
            else if (listMode == 1 && billboardSubList == 2)
                currentList = LocationEditorHelper.billboards_lights;
            else if (listMode == 1 && billboardSubList == 3)
                currentList = LocationEditorHelper.billboards_treasure;
            else if (listMode == 1 && billboardSubList == 4)
                currentList = LocationEditorHelper.billboards_markers;
            else if (listMode == 4)
                currentList = LocationEditorHelper.houseParts;
            else
                currentList = LocationEditorHelper.NPCs;

            foreach (KeyValuePair<string, string> pair in currentList)
            {
                if (pair.Value.ToLower().Contains(searchField.ToLower()))
                {
                    searchListNames.Add(pair.Value);
                    searchListID.Add(pair.Key);
                }
            }
        }

        private void OnDestroy()
        {
            if (parent != null)
                DestroyImmediate(parent.gameObject);
        }

        private void CreateNewFile()
        {
            if (parent != null)
                DestroyImmediate(parent);

            levelData = new BuildingReplacementData();
            parent = new GameObject("Location Prefab");
        }

        private void OpenFile()
        {
            string path = EditorUtility.OpenFilePanel("Open", LocationEditorHelper.locationFolder, "json");

            if (LocationEditorHelper.LoadInterior(path, out levelData))
            {

                if (parent != null)
                {
                    DestroyImmediate(parent);
                }

                currentWorkFile = Path.GetFileName(path);

                parent = new GameObject("Location : " + currentWorkFile);
                LoadObjects();
                editMode = EditMode.EditMode;
            }
            else
            {
                path = "";
            }

            //We clear the Undo
            Undo.ClearAll();
        }

        private void UpdateLevelData()
        {
            ArrayUtility.Clear(ref levelData.RmbSubRecord.Exterior.Block3dObjectRecords);
            ArrayUtility.Clear(ref levelData.RmbSubRecord.Interior.Block3dObjectRecords);
            ArrayUtility.Clear(ref levelData.RmbSubRecord.Exterior.BlockFlatObjectRecords);
            ArrayUtility.Clear(ref levelData.RmbSubRecord.Interior.BlockFlatObjectRecords);
            ArrayUtility.Clear(ref levelData.RmbSubRecord.Exterior.BlockPeopleRecords);
            ArrayUtility.Clear(ref levelData.RmbSubRecord.Interior.BlockPeopleRecords);
            ArrayUtility.Clear(ref levelData.RmbSubRecord.Exterior.BlockDoorRecords);
            ArrayUtility.Clear(ref levelData.RmbSubRecord.Interior.BlockDoorRecords);

            Vector3 modelPosition;
            LocationEditorObject data;

            foreach (Transform child in parent.GetComponentInChildren<Transform>())
            {
                if (child.GetComponent<LocationEditorObject>() == null)
                    return;

                data = child.GetComponent<LocationEditorObject>();

                //3D models
                if (data.type == (int)LocationEditorHelper.DataType.Object3D)
                {
                    DaggerfallConnect.DFBlock.RmbBlock3dObjectRecord record = new DaggerfallConnect.DFBlock.RmbBlock3dObjectRecord();
                    record.ModelId = data.id;
                    record.ModelIdNum = uint.Parse(data.id);
                    record.ObjectType = data.objectType;

                    if (data.objectType == 3)
                    {
                        Vector3[] vertices = child.GetComponent<MeshFilter>().sharedMesh.vertices;

                        // Props axis needs to be transformed to lowest Y point
                        Vector3 bottom = vertices[0];
                        for (int j = 0; j < vertices.Length; j++)
                        {
                            if (vertices[j].y < bottom.y)
                                bottom = vertices[j];
                        }
                        modelPosition = new Vector3(child.localPosition.x, (child.localPosition.y + (bottom.y)), child.localPosition.z) / MeshReader.GlobalScale;
                    }
                    else
                    {
                        modelPosition = new Vector3(child.localPosition.x, -child.localPosition.y, child.localPosition.z) / MeshReader.GlobalScale;
                    }

                    record.XPos = Mathf.RoundToInt(modelPosition.x);
                    record.YPos = Mathf.RoundToInt(modelPosition.y);
                    record.ZPos = Mathf.RoundToInt(modelPosition.z);
                    record.XRotation = (short)(-child.eulerAngles.x * DaggerfallConnect.Arena2.BlocksFile.RotationDivisor);
                    record.YRotation = (short)(-child.eulerAngles.y * DaggerfallConnect.Arena2.BlocksFile.RotationDivisor);
                    record.ZRotation = (short)(-child.eulerAngles.z * DaggerfallConnect.Arena2.BlocksFile.RotationDivisor);
                    record.XScale = child.localScale.x;
                    record.YScale = child.localScale.y;
                    record.ZScale = child.localScale.z;


                    if (data.isExterior)
                        ArrayUtility.Add(ref levelData.RmbSubRecord.Exterior.Block3dObjectRecords, record);
                    else
                        ArrayUtility.Add(ref levelData.RmbSubRecord.Interior.Block3dObjectRecords, record);
                }

                else if (data.type == (int)LocationEditorHelper.DataType.Flat)
                {
                    DaggerfallConnect.DFBlock.RmbBlockFlatObjectRecord record = new DaggerfallConnect.DFBlock.RmbBlockFlatObjectRecord();
                    record.TextureArchive = int.Parse(data.id.Split('.')[0]);
                    record.TextureRecord = int.Parse(data.id.Split('.')[1]);
                    record.FactionID = data.factionID;
                    record.Flags = data.flags;

                    modelPosition = child.transform.localPosition / MeshReader.GlobalScale;
                    record.XPos = Mathf.RoundToInt(modelPosition.x);
                    record.YPos = Mathf.RoundToInt(-((child.localPosition.y - (child.GetComponent<DaggerfallBillboard>().Summary.Size.y / 2)) / MeshReader.GlobalScale));
                    record.ZPos = Mathf.RoundToInt(modelPosition.z);

                    if (data.isExterior)
                        ArrayUtility.Add(ref levelData.RmbSubRecord.Exterior.BlockFlatObjectRecords, record);
                    else
                        ArrayUtility.Add(ref levelData.RmbSubRecord.Interior.BlockFlatObjectRecords, record);
                }

                else if (data.type == (int)LocationEditorHelper.DataType.Person)
                {
                    DaggerfallConnect.DFBlock.RmbBlockPeopleRecord record = new DaggerfallConnect.DFBlock.RmbBlockPeopleRecord();
                    record.TextureArchive = int.Parse(data.id.Split('.')[0]);
                    record.TextureRecord = int.Parse(data.id.Split('.')[1]);
                    record.FactionID = data.factionID;
                    record.Flags = data.flags;

                    modelPosition = child.transform.localPosition / MeshReader.GlobalScale;
                    record.XPos = Mathf.RoundToInt(modelPosition.x);
                    record.YPos = Mathf.RoundToInt(-((child.localPosition.y - (child.GetComponent<DaggerfallBillboard>().Summary.Size.y / 2)) / MeshReader.GlobalScale));
                    record.ZPos = Mathf.RoundToInt(modelPosition.z);

                    if (data.isExterior)
                        ArrayUtility.Add(ref levelData.RmbSubRecord.Exterior.BlockPeopleRecords, record);
                    else
                        ArrayUtility.Add(ref levelData.RmbSubRecord.Interior.BlockPeopleRecords, record);
                }

                else if (data.type == (int)LocationEditorHelper.DataType.Door)
                {
                    DaggerfallConnect.DFBlock.RmbBlockDoorRecord record = new DaggerfallConnect.DFBlock.RmbBlockDoorRecord();
                    record.OpenRotation = data.openRotation;
                    modelPosition = child.transform.localPosition / MeshReader.GlobalScale;
                    record.XPos = Mathf.RoundToInt(modelPosition.x);
                    record.YPos = -Mathf.RoundToInt(modelPosition.y);
                    record.ZPos = Mathf.RoundToInt(modelPosition.z);
                    record.YRotation = (short)(-child.eulerAngles.y * DaggerfallConnect.Arena2.BlocksFile.RotationDivisor);

                    if (data.isExterior)
                        ArrayUtility.Add(ref levelData.RmbSubRecord.Exterior.BlockDoorRecords, record);
                    else
                        ArrayUtility.Add(ref levelData.RmbSubRecord.Interior.BlockDoorRecords, record);
                }
            }
        }
    }
}