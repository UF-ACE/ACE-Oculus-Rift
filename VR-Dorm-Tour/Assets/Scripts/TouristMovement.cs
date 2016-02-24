using UnityEngine;
using System.Collections;
using UnityEngine.VR;
// class for user movement
// attach to root "Tourist" object
public class TouristMovement : MonoBehaviour {
    private CharacterController tourist_ctrl; // component used to move user object in a given direction; needed for proper collider use;
                                              // attatched to root "Tourist" object
    private Transform tourist_head_pitch; // object used for pitch rotation when not in VR mode
    private Transform tourist_head_yaw;   // object use for yaw rotation
    private Transform tourist_cam; // camera object, represents users actual head location as reported by VR headset;
                                   // position and rotation of this object is made read-only by Unity's VR engine
    public float rot_size; // digital yaw rotation size in degrees
    public float look_spd; // analog rotation speed multiplier
    public float move_spd; // movment speed multiplier
    //private Vector3 tourist_fwrd, tourist_right; // forward and right movment directions 
    private GameObject laser_pointer; // laser pointer object used for selecting doors
    private GameObject hit_door; // the door touched by the laser pointer
    private bool yaw_axis_pushed;
    private Transform room; // transform of current room, used for calculating room-tourist offset
    public Vector3 room_tourist_offset; // shows current offset between room origin and user
    void Start() {
        // set movement variables to default values
        rot_size = 30f;
        look_spd = 0.5f;
        move_spd = 0.7f;
        // get components and objects
        tourist_ctrl = GetComponent<CharacterController>();
        tourist_head_yaw = transform.GetChild(0);
        tourist_head_pitch = tourist_head_yaw.GetChild(0);
        tourist_cam = tourist_head_pitch.GetChild(0);
        laser_pointer = tourist_cam.GetChild(0).gameObject;
        // set unity VR oversampling setting
        VRSettings.renderScale = 2;
        // initialize other variables
        hit_door = null;
        yaw_axis_pushed = false;
        if (GameObject.Find("Hub") != null) {
            room = GameObject.Find("Hub").transform;
        } else {
            room = null;
        }
    }
    // called each frame
	void Update () {
        if (room != null) {
            updateRoomTouristOffset();
        }
        // press r on keyboard or 1 on wiimote to recenter tracking
        if (Input.GetButtonDown("Reset")) {
                recenterTourist(true);
        }
        // rotation
        // only use analog rotation if not in VR mode
        if (!VRSettings.enabled) {
            // get X and Y axis values
            // currently just from mouse
            float look_hrz = Input.GetAxisRaw("X Look") * look_spd;
            float look_vrt = Input.GetAxisRaw("Y Look") * look_spd;
            // apply yaw roatation
            // head object must be rotated around the camera, otherwise the user's view circles around the VR tracking center
            // doesn't matter in non-VR mode, but might as well do it that way here in case we want to add analog yaw back in (unlikely)
            // a side effect of this approach is that the head gets rotated away from body when the user rotates with their head in VR tracking is offcenter
            // this should be corrected after all rotation is done (not yet implemented)
            tourist_head_yaw.RotateAround(tourist_cam.position, Vector3.up, look_hrz);
            // apply pitch rotation
            tourist_head_pitch.Rotate(-look_vrt, 0f, 0f);
        }
        // digital rotation
        if (!yaw_axis_pushed){
            // press q and e on keyboard or left and right on wiimote to rotate left or right
            if (Input.GetAxisRaw("Digital Yaw") != 0){
                // recenter scamera as before digital rotation as a temporary fix to camera-body alignment
                // will probably cause an annoying shift the first time user rotates after moving around a bit,
                // but will prevent alignment from getting worse with each rotation
                recenterTourist(false);
                // see RotateAround() explanation above
                tourist_head_yaw.RotateAround(tourist_cam.position, Vector3.up, rot_size * Input.GetAxisRaw("Digital Yaw"));
                yaw_axis_pushed = true;
            }
        }else{
            if(Input.GetAxisRaw("Digital Yaw") == 0){
                yaw_axis_pushed = false;
            }
        }
        // realign body with head
        // cannot figure out a way to do this with built-in modules
        // will try implementing and using custom movment restriction mechanism instead of using collisions

        // movement
        // get movment input (WASD, up and down on wiimote)
        // not planning on allowing left and right movment outside of development, it is less comfortable than forward movement
        // and excluding it should encourage people to move their head more. Backwards movement is also less comfortable,
        // but removing that might make movement too difficult/annoying
        Vector2 move_input = new Vector2(Input.GetAxisRaw("X Move"), Input.GetAxisRaw("Y Move")).normalized * move_spd;
        // calculate forward and right movement axes from head orientation
        Vector3 tourist_fwrd = Quaternion.AngleAxis(tourist_cam.eulerAngles.y, Vector3.up) * Vector3.forward;
        Vector3 tourist_right = Quaternion.AngleAxis(tourist_cam.eulerAngles.y, Vector3.up) * Vector3.right;
        // calculate final movement vector
        Vector3 tourist_move = move_input.y * tourist_fwrd + move_input.x * tourist_right;
        // move user
        tourist_ctrl.SimpleMove(tourist_move);
        // door selection
        // press left mouse button or A on wiimote to activate the laser pointer
        if (Input.GetButtonDown("Point")) {
            laser_pointer.SetActive(true);
        }
        // if the laser pointer button is being pressed
        if (Input.GetButton("Point")) {
            RaycastHit door_hit;
            RaycastHit bound_hit;
            // raycast from camera in the direction the user is looking
            // if a door was hit
            if(Physics.Raycast(tourist_cam.position, tourist_cam.forward, out door_hit, 30f, ~(1 << 2 | 1 << 9))){
                // point the laser at the center of the user's view 
                laser_pointer.transform.LookAt(door_hit.point);
                // if no door what being pointed at on the previous frame
                if(hit_door == null) {
                    // store the door object
                    hit_door = door_hit.collider.gameObject;
                    // make the door highlight visible
                    hit_door.GetComponent<MeshRenderer>().enabled = true;
                // if another door was being pointed at on the previous frame
                }else if(hit_door != door_hit.collider.gameObject) {
                    // hide the old door's highlight
                    hit_door.GetComponent<MeshRenderer>().enabled = false;
                    // store the new door object
                    hit_door = door_hit.collider.gameObject;
                    // make the new door's highlight visible
                    hit_door.GetComponent<MeshRenderer>().enabled = true;
                }
                // press right mouse button or B on wiimote to select a door
                if (Input.GetButtonDown("Select")) {
                    // transport user to room of selected door
                    hit_door.GetComponent<Transporter>().transport();
                }
            // if no door was hit
            } else {
                // if the door object is still set
                if (hit_door != null) {
                    // hide door highlight and set object to null
                    hit_door.GetComponent<MeshRenderer>().enabled = false;
                    hit_door = null;
                }
                // raycast from camera in the direction the user is looking
                // if a boundary was hit
                if (Physics.Raycast(tourist_cam.position, tourist_cam.forward, out bound_hit, 30f, ~(1 << 2 | 1 << 10))) {
                    // point the laser at the center of the user's view 
                    laser_pointer.transform.LookAt(bound_hit.point);
                }
            }
        }
        // if the user stops pointing
        if(Input.GetButtonUp("Point")){
            // if they were pointing at a door
            if(hit_door != null) {
                // hide door hilight and set object to null
                hit_door.GetComponent<MeshRenderer>().enabled = false;
                hit_door = null;
            }
            // hide laser pointer
            laser_pointer.SetActive(false);
        }
    }
    // recenter head and camera
    // full specifies whether full headset tracking or only camera position should be recentered
    public void recenterTourist(bool full) {
        if (full) {
            // center head on body
            tourist_head_yaw.localPosition = (new Vector3(0f, GetComponent<CharacterController>().height / 2, 0f));
            // only recenter head tracking if in VR mode
            if (VRSettings.enabled) {
                // center camera on head and set current yaw as forward
                InputTracking.Recenter();
            }
        } else {
            // center camera x,z on body
            tourist_head_yaw.Translate(new Vector3(transform.position.x - tourist_cam.position.x, 0f, transform.position.z - tourist_cam.position.z), Space.World);
        }
    }
    // used by Transporter class to update the room the user is in when teleporting to new room
    public void setCurrentRoom(Transform t) {
        room = t;
    }
    void updateRoomTouristOffset() {
        room_tourist_offset = transform.position - room.position - new Vector3(0, GetComponent<CharacterController>().height / 2, 0);
    }
}
