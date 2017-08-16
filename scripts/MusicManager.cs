using UnityEngine;
using System.Collections;

public class MusicManager : MonoBehaviour {

	public AudioClip[] EarlySongs;
	public AudioClip[] LateSongs;
	public AudioClip[] WarSongs;

	const int MUS_STATE_WAR=1;
	const int MUS_STATE_EARLY=2;
	const int MUS_STATE_LATE=3;

	public int iMusState=MUS_STATE_EARLY;

	public void SetWarState()
	{
		iMusState=MUS_STATE_WAR;
	}
	
	public void SetEarlyState()
	{
		iMusState=MUS_STATE_EARLY;
	}

	public void SetLateState()
	{
		iMusState=MUS_STATE_LATE;
	}
	
	void Update ()
	{
		AudioSource ThisSource=GetComponent<AudioSource>();
		if(ThisSource.isPlaying==false)
		{
			if(iMusState==MUS_STATE_EARLY)
			{
				ThisSource.clip=EarlySongs[Random.Range (0,EarlySongs.Length)];
			}
			if(iMusState==MUS_STATE_WAR)
			{
				ThisSource.clip=WarSongs[Random.Range (0,WarSongs.Length)];
			}
			if(iMusState==MUS_STATE_LATE)
			{
				ThisSource.clip=LateSongs[Random.Range (0,LateSongs.Length)];
			}
			ThisSource.Play();
		}
	}
}
