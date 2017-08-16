using UnityEngine;
using System.Collections;

public class CelestialBody : MonoBehaviour {

	public float fDist=30f;
	public float fCycleTime=60f;
	public float fAxialTilt=10f;
	public Vector3 vOffSet=new Vector3(7f,0f,7f);

	private float fTime;

	float fPythag(float fDist1, float fDist2)
	{
		return Mathf.Sqrt(Mathf.Pow(fDist1,2)+Mathf.Pow(fDist2,2));
	}

	Quaternion GetFacingToward(Vector3 vOrigin,Vector3 vTarget)
	{
		Vector3 vDiff=vTarget-vOrigin;
		Vector3 vAngle=new Vector3();
		//Debug.Log ("fPythag is "+fPythag(vDiff.y,vDiff.z));
		if(fPythag(vDiff.y,vDiff.z)>0f)
		{
			vAngle.x=Mathf.Rad2Deg*Mathf.Atan(vDiff.x/fPythag(vDiff.y,vDiff.z));
			//Debug.Log ("vAngle.x is "+vAngle.x);
		}
		else vAngle.x=0f;
		if(fPythag(vDiff.y,vDiff.x)>0f)
		{
			vAngle.z=Mathf.Rad2Deg*Mathf.Atan(vDiff.z/fPythag(vDiff.y,vDiff.x));
		}
		else vAngle.z=0f;
		if(fPythag(vDiff.x,vDiff.z)>0f)
		{
			vAngle.y=Mathf.Rad2Deg*Mathf.Atan(vDiff.y/fPythag(vDiff.x,vDiff.z));
		}
		else vAngle.y=0f;
		return Quaternion.Euler(vAngle.y-180f,vAngle.x-180f,vAngle.z-180f);
	}
	
	// Update is called once per frame
	void Update ()
	{
		float fSpeed=Mathf.PI*2/fCycleTime;
		float fTime=Time.time;
		float fX=Mathf.Sin (fTime*fSpeed)*fDist;
		float fY=Mathf.Cos (fTime*fSpeed)*fDist;
		float fZ=Mathf.Sin (Mathf.Deg2Rad*fAxialTilt)*fY;
		fY-=Mathf.Cos (Mathf.Deg2Rad*(90-fAxialTilt))*fY;
		Vector3 vFinalPosition=new Vector3(fX+vOffSet.x,fY+vOffSet.y,fZ+vOffSet.z);
		transform.position=vFinalPosition;
		transform.rotation=GetFacingToward(vFinalPosition,vOffSet);
	}
}
