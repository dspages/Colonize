using UnityEngine;
using System.Collections;

/// MouseLook rotates the transform based on the mouse delta.
/// Minimum and Maximum values can be used to constrain the possible rotation

/// To make an FPS style character:
/// - Create a capsule.
/// - Add the MouseLook script to the capsule.
///   -> Set the mouse look to use LookX. (You want to only turn character but not tilt it)
/// - Add FPSInputController script to the capsule
///   -> A CharacterMotor and a CharacterController component will be automatically added.

/// - Create a camera. Make the camera a child of the capsule. Reset it's transform.
/// - Add a MouseLook script to the camera.
///   -> Set the mouse look to use LookY. (You want the camera to tilt up and down like a head. The character already turns.)
[AddComponentMenu("Camera-Control/Mouse Look")]
public class CameraMove : MonoBehaviour {
	
	public enum RotationAxes { MouseXAndY = 0, MouseX = 1, MouseY = 2 }
	public RotationAxes axes = RotationAxes.MouseXAndY;
	public float sensitivityX = 15F;
	public float sensitivityY = 15F;
	
	public float minimumX = -360F;
	public float maximumX = 360F;
	
	public float minimumY = -60F;
	public float maximumY = 60F;
	public float CameraHeight=10f;
	public float ScrollSensitivity=0.2f;
	public float HeightSensitivity=30f;
	public float MinHeight=20f;
	public float MaxHeight=150f;
	float rotationY = 0F;
	
	void Update ()
	{
		Vector3 CameraSpeed=new Vector3(0f,0f,0f);	
		Vector3 CurrentMousePosition=Input.mousePosition;
		if (CurrentMousePosition.x >= Screen.width-20)
		{
			CameraSpeed.x=10*ScrollSensitivity;
		}
		if (CurrentMousePosition.x <=20)
		{
			CameraSpeed.x=10*-ScrollSensitivity;	
		}
		transform.Translate (CameraSpeed, Space.Self);
		CameraSpeed.x = 0f;//We want to scroll, rather than to zoom in/out. Thus, only the z speed of the camera should be controlled by the x axis of the mouse.
		
		if (CurrentMousePosition.y >= Screen.height-20)
		{
			CameraSpeed.z=10*ScrollSensitivity*(1/Mathf.Sin (Mathf.Deg2Rad*(90-transform.localEulerAngles.x)));
		}
		if (CurrentMousePosition.y <= 20)
		{
			CameraSpeed.z=10*-ScrollSensitivity*(1/Mathf.Sin (Mathf.Deg2Rad*(90-transform.localEulerAngles.x)));
		}
		transform.Translate (CameraSpeed, Space.Self);
		transform.position=new Vector3(Mathf.Clamp (transform.position.x,-1f,15f),
		0.0f,
		Mathf.Clamp (transform.position.z,-1f,15f));
		
		Vector3 HeightChange=transform.position;
		if(Input.GetKey ("a")) CameraHeight+=HeightSensitivity;
		if(Input.GetKey ("z")) CameraHeight-=HeightSensitivity;
		if (CameraHeight > MaxHeight)
		{
			CameraHeight = MaxHeight;
		}
		if (CameraHeight < MinHeight)
		{
			CameraHeight = MinHeight;
		}
		HeightChange.y = CameraHeight - HeightChange.y;
		HeightChange.z = 0;
		HeightChange.x = 0;
		transform.Translate (HeightChange, Space.World);
		
		if (!Input.GetMouseButton (1)) return;//If the right mouse button is NOT pressed, we don't need to execute the remaining code
		
		float rotationX = transform.localEulerAngles.y + Input.GetAxis("Mouse X") * sensitivityX;			
		rotationY += Input.GetAxis("Mouse Y") * sensitivityY;
		rotationY = Mathf.Clamp (rotationY, minimumY, maximumY);			
		transform.localEulerAngles = new Vector3(-rotationY, rotationX, 0);
	}
	
	void Start ()
	{
		// Make the rigid body not change rotation
		if (rigidbody)
			rigidbody.freezeRotation = true;
	}
}