using UnityEngine;
using System.Collections;
// class for transporting the user between rooms
// attatch to "Transport" objects
public class Transporter : MonoBehaviour {
    public string target_name; // name of room to transport to
    public Vector3 pos; // position offset from target room origin that the user should be transported to
    public float yaw; // direction user should be facing after transport
    private GameObject tourist; // user object
    private GameObject target; // target room object
    private GameObject room_parent; // parent object containing all parts of the room the user is transported from
    void Start() {
        // if no target name is given, set public variables to the starting position in the hub
        if (target_name == "") {
            target_name = "Hub";
            pos = new Vector3(0f, -0.6f, 3f);
            yaw = 180f;
        }
        // get target room object from the given name
        target = GameObject.Find(target_name);
        // get the parent room object, which is 2 levels up for all non-hub transports
        room_parent = transform.parent.parent.gameObject;
        // for hub transports, "Entrances" is the object 2 levels up
        // the object we want is one level higher
        if(room_parent.name == "Entrances") {
            room_parent = room_parent.transform.parent.gameObject;
        }
        // get the tourist object
        tourist = GameObject.Find("Tourist");
        // hide all rooms except the hub at the start
        if(room_parent.name != "Hub") {
            setVisibility(room_parent, false);
        }
    }
    // transport the user to the target room
    public void transport() {
        // make the target room visible
        setVisibility(target, true);
        // create object to access TouristMovement methods
        TouristMovement t_mov = tourist.GetComponent<TouristMovement>();
        // move user to target position
        tourist.transform.position = pos + target.transform.position + new Vector3(0f, tourist.GetComponent<CharacterController>().height / 2, 0f);
        // update current room in touristmovement class
        t_mov.setCurrentRoom(target.transform);
        // face user in the specified direction
        Vector3 old_dir = tourist.transform.GetChild(0).eulerAngles;
        tourist.transform.GetChild(0).eulerAngles = new Vector3(old_dir.x, yaw, old_dir.z);;
        // hide the parent room
        setVisibility(room_parent, false);
    }
    // make a given room visible or invisible
    private void setVisibility(GameObject room, bool visible) {
        // get an array of all mesh renderers under the given room object
        MeshRenderer[] mesh_renderers = room.GetComponentsInChildren<MeshRenderer>(true);
        foreach(MeshRenderer mesh_rend in mesh_renderers) {
            // set the visiblity of all mesh renderers unless they are part of a bounds object (layer 9) or a transport object (layer 10)
            // the visiblity of those objects are controlled by the TouristMovment class
            if(mesh_rend.gameObject.layer < 9) {
                mesh_rend.enabled = visible;
            }
        }
        // disable colliders
        Collider[] colliders = room.GetComponentsInChildren<Collider>(true);
        foreach(Collider col in colliders) {
            col.enabled = visible;
        }
    }
}
