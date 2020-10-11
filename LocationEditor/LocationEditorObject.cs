using UnityEngine;

namespace DaggerfallWorkshop.Game.Utility.LocationEditor
{
    [ExecuteInEditMode]
    public class LocationEditorObject : MonoBehaviour
    {
        public int type = -1;
        public bool isExterior = false;
        public string id;
        public byte objectType;
        public short factionID;
        public byte flags;
        public short openRotation;
        private int tempLocationX;
        private int tempLocationY;
        private int tempLocationZ;

        public void CreateData(DaggerfallConnect.DFBlock.RmbBlock3dObjectRecord data, bool isExterior)
        {
            HideComponents();
            type = 0;
            this.isExterior = isExterior;
            id = data.ModelId;
            objectType = data.ObjectType;
        }
        public void CreateData(DaggerfallConnect.DFBlock.RmbBlockFlatObjectRecord data, bool isExterior)
        {
            HideComponents();
            type = 1;
            this.isExterior = isExterior;
            id = data.TextureArchive + "." + data.TextureRecord;
            factionID = data.FactionID;
            flags = data.Flags;
        }
        public void CreateData(DaggerfallConnect.DFBlock.RmbBlockPeopleRecord data, bool isExterior)
        {
            HideComponents();
            type = 2;
            this.isExterior = isExterior;
            id = data.TextureArchive + "." + data.TextureRecord;
            factionID = data.FactionID;
            flags = data.Flags;
        }
        public void CreateData(DaggerfallConnect.DFBlock.RmbBlockDoorRecord data, bool isExterior)
        {
            HideComponents();
            type = 3;
            this.isExterior = isExterior;
            openRotation = data.OpenRotation;
        }

        private void HideComponents()
        {
            foreach (Material mat in gameObject.GetComponent<Renderer>().sharedMaterials)
            {
                mat.hideFlags = HideFlags.HideInInspector;
            }

            foreach (Component comp in GetComponents<Component>())
            {
                comp.hideFlags = HideFlags.HideInInspector;
            }
        }
        public void UpdateVisibility(bool isExterior)
        {
            if (this.isExterior != isExterior)
            {
                gameObject.hideFlags = HideFlags.HideInHierarchy;
                gameObject.GetComponent<Renderer>().enabled = false;
            }
            else
            {
                gameObject.hideFlags = HideFlags.None;
                gameObject.GetComponent<Renderer>().enabled = true;
                HideComponents();
            }
        }

        void LateUpdate()
        {
            //Me make sure that objects always snapps to the same degree of precision in which they are stored
            tempLocationX = Mathf.RoundToInt((transform.position.x / MeshReader.GlobalScale));
            tempLocationY = Mathf.RoundToInt((transform.position.y / MeshReader.GlobalScale));
            tempLocationZ = Mathf.RoundToInt((transform.position.z / MeshReader.GlobalScale));
            transform.position = new Vector3(tempLocationX * MeshReader.GlobalScale, tempLocationY * MeshReader.GlobalScale, tempLocationZ * MeshReader.GlobalScale);

            //Reduce the precision of rotation to whole degrees, this is less than needed to be stored properly, but should be enough and makes it easier to snap interior parts together
            transform.rotation = Quaternion.Euler(Mathf.RoundToInt(transform.rotation.eulerAngles.x),
                                                Mathf.RoundToInt(transform.rotation.eulerAngles.y),
                                                Mathf.RoundToInt(transform.rotation.eulerAngles.z));

        }
    }
}