using UnityEngine;
using System.Collections;
using System.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;

//dspages@hotmail.com wrote this file. As long as you retain this notice you
//can do whatever you want with this stuff. If we meet some day, and you think
//this stuff is worth it, you can buy me a beer in return

public class GameController : MonoBehaviour {

	[Serializable] class SaveData
	{
		public List<INHABITANT_TEMPLATE> InhabitantList=new List<INHABITANT_TEMPLATE>();
		public List <int> GEARSTOCKPILES=new List<int>();
		public List<int> EVENTFLAGS=new List<int>();
		public int StasisPods=0;
		public List<AREA_TEMPLATE> AreaList=new List<AREA_TEMPLATE>();
		public List<int> resourceStockpiles=new List<int>();
		public List<string> resourceNames=new List<string>();
		public List<bool> TechsResearched=new List<bool>();
		public List<bool> TypesCloned=new List<bool>();
		public string ThisSeason=SEASON_SPRING;
		public int ThisYear=1;
	}

	public void SaveGame(string sFileName)
	{
		BinaryFormatter BF=new BinaryFormatter();
		FileStream MyFile=File.Open(Environment.CurrentDirectory+"/Saves/"+sFileName+".dat",FileMode.Create);
		SaveData ThisGame=new SaveData();
		ThisGame.InhabitantList=InhabitantList;
		ThisGame.GEARSTOCKPILES=GEARSTOCKPILES;
		ThisGame.EVENTFLAGS=EVENTFLAGS;
		ThisGame.StasisPods=StasisPods;
		ThisGame.AreaList=AreaList;
		ThisGame.resourceStockpiles=resourceStockpiles;
		ThisGame.resourceNames=resourceNames;
		ThisGame.TechsResearched=TechsResearched;
		ThisGame.TypesCloned=TypesCloned;
		ThisGame.ThisSeason=ThisSeason;
		ThisGame.ThisYear=ThisYear;
		BF.Serialize(MyFile,ThisGame);
		MyFile.Close();
		MessageText("Saved game "+sFileName+".dat!");
	}

	bool LoadGame(string sFileName)
	{
		if(File.Exists(Environment.CurrentDirectory+"/Saves/"+sFileName+".dat"))
		{
			BinaryFormatter BF=new BinaryFormatter();
			try
			{
				FileStream MyFile=File.Open(Environment.CurrentDirectory+"/Saves/"+sFileName+".dat",FileMode.Open);
				SaveData ThisGame=(SaveData)BF.Deserialize(MyFile);
				MyFile.Close();
				InhabitantList=ThisGame.InhabitantList;
				GEARSTOCKPILES=ThisGame.GEARSTOCKPILES;
				EVENTFLAGS=ThisGame.EVENTFLAGS;
				StasisPods=ThisGame.StasisPods;
				AreaList=ThisGame.AreaList;
				resourceStockpiles=ThisGame.resourceStockpiles;
				resourceNames=ThisGame.resourceNames;
				TechsResearched=ThisGame.TechsResearched;
				TypesCloned=ThisGame.TypesCloned;
				ThisSeason=ThisGame.ThisSeason;
				ThisYear=ThisGame.ThisYear;
				MessageText("Loaded Save "+sFileName+".dat");
				UnloadAreaTemplate();
				LoadAreaTemplate(AreaList[0]);
				return true;
			}
			catch
			{
				MessageText("Failed to load file "+sFileName);
				return false;
			}
		}
		MessageText("File not found "+sFileName);
		return false;
	}

    const int MUS_STATE_WAR = 1;
    const int MUS_STATE_EARLY = 2;
    const int MUS_STATE_LATE = 3;

	const int DEBUGMODE=0;

	const int BUILDING_KIND_NONE=1;
	const int BUILDING_KIND_COMMAND_CENTER=2;
	const int BUILDING_KIND_WORKED_TILE_MATERIAL=7;
	const int BUILDING_KIND_FOG_TILE_MATERIAL=8;
	const int BUILDING_KIND_BEING_SCOUTED=9;
	const int BUILDING_KIND_COMBATANT=19;
	const int BUILDING_KIND_ENEMY=25;

	const int TILE_STATUS_WORKED=1;
	const int TILE_STATUS_UNWORKED=2;
	const int TILE_STATUS_COMBATANT=3;
	const int AREA_TERRAIN_PLAINS=1;

	const int MENU_NODE_MAIN=0;
	const int MENU_NODE_COLONIST_SELECT=1;
	const int MENU_NODE_COLONIST_CHOOSESTRUCTURE=2;
	const int MENU_NODE_COLONIST_CHOOSEJOB=3;
	const int MENU_NODE_COLONIST_CHOOSETILE=4;
	const int MENU_NODE_COLONIST_CHOOSECONSTRUCTION=5;
	const int MENU_NODE_COLONIST_VIEWSKILLS=6;
	const int MENU_NODE_COLONIST_CHOOSETECH=7;
	const int MENU_NODE_COLONIST_CHOOSELEARN=8;
	const int MENU_NODE_COLONIST_VIEWHEALTH=9;
	const int MENU_NODE_COLONIST_VIEWINVENTORY=10;
	const int MENU_NODE_VIEW_ALLSTATS=11;
	const int MENU_NODE_COLONIST_COMBATDEPLOY=12;
	const int MENU_NODE_COLONIST_SWAPGEAR=13;
	const int MENU_NODE_COLONIST_ATTACKTARGET=14;
	const int MENU_NODE_COLONIST_WORKSHOPPICK=15;
	const int MENU_NODE_COLONIST_RENAME=16;
	const int MENU_NODE_TUTORIAL=17;
	const int MENU_NODE_SYSTEM=18;
	const int MENU_NODE_SAVE=19;
	const int MENU_NODE_LOAD=20;

	int TUTORIAL_STATE=0;

	const int SPECIES_HUMAN=1;

	const int GEAR_TYPE_HAND_NONE=1;
	const int GEAR_TYPE_ARMOR_NONE=2;
	const int GEAR_TYPE_ACCESSORY_NONE=3;

	static int JOB_TYPE_IDLE=5;

	public int selectedMenuNode=MENU_NODE_MAIN;
	public int TILE_SELECTED_X=1;
	public int TILE_SELECTED_Z=1;
	public int JOB_SELECTED=0;
	public int JOB_SELECTED_PARAM1=0;
	public int SELECTED_COLONIST=0;
	public int GEAR_SWAP_TYPE=1;

	public Material MATERIAL_DEFAULT;

	public GameObject TILE_PREFAB;

	public List<INHABITANT_TEMPLATE> InhabitantList=new List<INHABITANT_TEMPLATE>();
	public List <int> GEARSTOCKPILES=new List<int>();
	public List<int> EVENTFLAGS=new List<int>();
	public int StasisPods=0;

	void DestroyAllChildren(Transform transform)
	{
		var children = new List<GameObject>();
		foreach (Transform child in transform) children.Add(child.gameObject);
		children.ForEach(child => Destroy(child));		
	}

	void ResetAllChildren(Transform transform)
	{
		var children = new List<GameObject>();
		foreach (Transform child in transform) children.Add(child.gameObject);
		foreach (GameObject child in children) child.transform.position+=transform.position;
	}

	bool AttachChildBuilding(int iKind, GameObject TileRefreshed)
	{
		string sReadout=iniRead("buildingtypes","MATERIAL_FILENAME",iKind);
		float fYCorrect=0f;
		if(iPersMouseOverX==TileRefreshed.GetComponent<TileLocation>().xCoord & iPersMouseOverZ==TileRefreshed.GetComponent<TileLocation>().zCoord)
		{
			string sReadoutMouseOver=iniRead("buildingtypes","MOUSEOVER_FILENAME",iKind);
			if(sReadoutMouseOver!="")
			{
				sReadout=sReadoutMouseOver;
				fYCorrect+=iniReadfloat("buildingtypes","MOUSEOVERY",iKind);
			}
		}
		//Debug.Log("Creating Child "+ sReadout);
		try //Some buildings have placeholder children
		{
			//Debug.Log ("Loading "+"Buildings/"+sReadout);
			GameObject TileTerrain=Instantiate (Resources.Load("Buildings/"+sReadout, typeof(GameObject)),new Vector3(0f,0f,0f),Quaternion.identity) as GameObject;
			if(TileTerrain==null) {return false;}
			TileTerrain.transform.parent = TileRefreshed.transform;
			ResetAllChildren (TileTerrain.transform);
			Vector3 ChildPosition=new Vector3(TileRefreshed.transform.position.x+TileTerrain.transform.position.x,0f+TileTerrain.transform.position.y,TileRefreshed.transform.position.z+TileTerrain.transform.position.z);
			float fZOffSet=iniReadfloat ("buildingtypes","OFFSETZ",iKind);
			ChildPosition.z+=fZOffSet;
			float fXOffSet=iniReadfloat ("buildingtypes","OFFSETX",iKind);
			ChildPosition.x+=fXOffSet;
			ChildPosition.y+=iniReadfloat("buildingtypes","OFFSETY",iKind)+fYCorrect;
			TileTerrain.transform.position=ChildPosition;
			float fScale=iniReadfloat ("buildingtypes","MATSCALE",iKind);
			TileTerrain.transform.localScale=new Vector3(fScale,fScale,fScale);
			try
			{
				if(GetIsHighlightedBuilding(TileRefreshed.GetComponent<TileLocation>().xCoord,TileRefreshed.GetComponent<TileLocation>().zCoord))
				TileTerrain.renderer.material.shader = Shader.Find("Self-Illumin/Outlined Diffuse");	
				else TileTerrain.renderer.material.shader = Shader.Find("Diffuse");
			}
			catch{}
			return true;
		}
		catch {return false;}
	}

	bool GetIsHighlightedBuilding(int iXCoord,int iZCoord)
	{
		if(iXCoord==TILE_SELECTED_X&&iZCoord==TILE_SELECTED_Z) return true;
		return false;
	}

	bool GetIsHighlightedCreature(int iIndx)
	{
		if(iIndx==SELECTED_COLONIST)
		{
			int s=selectedMenuNode;
			if(s==MENU_NODE_COLONIST_ATTACKTARGET|
			s==MENU_NODE_COLONIST_RENAME|
			s==MENU_NODE_COLONIST_CHOOSETECH|
			s==MENU_NODE_COLONIST_WORKSHOPPICK|
			s==MENU_NODE_COLONIST_VIEWSKILLS|
			s==MENU_NODE_COLONIST_COMBATDEPLOY|
			s==MENU_NODE_COLONIST_CHOOSESTRUCTURE|
			(s==MENU_NODE_COLONIST_CHOOSEJOB&bStructFirst==false)|
			s==MENU_NODE_COLONIST_VIEWHEALTH|
			s==MENU_NODE_COLONIST_CHOOSETILE|
			s==MENU_NODE_COLONIST_CHOOSELEARN|
			(s==MENU_NODE_COLONIST_CHOOSECONSTRUCTION&bStructFirst==false)|
			s==MENU_NODE_COLONIST_VIEWHEALTH) return true;
		}
		return false;
	}

	bool AttachChildCreature(int iSpecies,GameObject TileRefreshed,int iCreatureIndex)
	{
		string sReadout=iniRead("species","MODELFILE",iSpecies);
		//Debug.Log("Creating Child "+ sReadout);
		try
		{
			GameObject CreatureCreated=Instantiate (Resources.Load("Creatures/"+sReadout, typeof(GameObject)),new Vector3(0f,0f,0f),Quaternion.identity) as GameObject;
			if(CreatureCreated==null) return false;
			float fScale=iniReadfloat ("species","MODELSCALE",iSpecies);
			CreatureCreated.transform.localScale=new Vector3(fScale,fScale,fScale);
			Vector3 ChildPosition=new Vector3(TileRefreshed.transform.position.x+CreatureCreated.transform.position.x,0f+CreatureCreated.transform.position.y,TileRefreshed.transform.position.z+CreatureCreated.transform.position.z);
			CreatureCreated.transform.parent = TileRefreshed.transform;
			ChildPosition.y+=0.5f+iniReadfloat ("species","MODY",iSpecies);
			ResetAllChildren(CreatureCreated.transform);
			CreatureCreated.transform.position=ChildPosition;
			if(GetIsHighlightedCreature(iCreatureIndex))
			{
				CreatureCreated.renderer.material.shader = Shader.Find("Self-Illumin/Outlined Diffuse");
				//Debug.Log ("Applied self-illumin shader to creature");
			}
			else
			{
				CreatureCreated.renderer.material.shader = Shader.Find("Diffuse");
				//Debug.Log ("Applied diffuse shader to creature");
			}
			try
			{
				//Debug.Log("Creating Child controller "+ sReadout);
				RuntimeAnimatorController ThisController = Instantiate(Resources.Load("Creatures/Controllers/"+sReadout,typeof(RuntimeAnimatorController))) as RuntimeAnimatorController;
				CreatureCreated.GetComponent<Animator>().runtimeAnimatorController = ThisController;
				CreatureCreated.GetComponent<Animator>().applyRootMotion=false;
			}
			catch
			{
				sReadout="Humanoid";
				//Debug.Log("Creating Child controller "+ sReadout);
				RuntimeAnimatorController ThisController = Instantiate(Resources.Load("Creatures/Controllers/"+sReadout,typeof(RuntimeAnimatorController))) as RuntimeAnimatorController;
				CreatureCreated.GetComponent<Animator>().runtimeAnimatorController = ThisController;
				CreatureCreated.GetComponent<Animator>().applyRootMotion=false;
			}
			Debug.Log("Successfully created creature.");
			return true;
		}
		catch {return false;}
	}

	bool GetIsTargetingPlayerCreature(INHABITANT_TEMPLATE itCheck)
	{
		TILE_TEMPLATE TargetTile=AreaList[itCheck.AreaIndex].TileList[itCheck.MyJob.xCoordinateTarget,itCheck.MyJob.zCoordinateTarget];
		if(TargetTile.ColonistHere<0) return false;
		if(InhabitantList[TargetTile.ColonistHere].EnemyType==0) return true;
		return false;
	}
	
	void RefreshTile(GameObject TileRefreshed)
	{
		if(TileRefreshed==null) return;
		DestroyAllChildren(TileRefreshed.transform);
		int xCoord=TileRefreshed.GetComponent<TileLocation>().xCoord;//Better to be passing around "pointers" to the tile than making extra copies of the tile itself, for obvious reasons.
		int zCoord=TileRefreshed.GetComponent<TileLocation>().zCoord;
		//if(iPersMouseOverX==TileRefreshed.GetComponent<TileLocation>().xCoord & iPersMouseOverZ==TileRefreshed.GetComponent<TileLocation>().zCoord)
		//TileRefreshed.renderer.material.shader = Shader.Find("Self-Illumin/Outlined Diffuse");
		//else TileRefreshed.renderer.material.shader = Shader.Find("Diffuse");
		TILE_TEMPLATE TargetTemplate=AreaList[VIEWED_AREA_INDEX].TileList[xCoord,zCoord];
		int buildingKindConstant=TargetTemplate.thisTemplate.building_kind;
		bool fogOfWar=TargetTemplate.fogOfWar;
		int tileStatusConstant=TargetTemplate.tileStatusConstant;
		Material TargetMaterial=new Material(MATERIAL_DEFAULT);		
		if(fogOfWar==true&tileStatusConstant==TILE_STATUS_UNWORKED)
		{
			string sReadout=iniRead("buildingtypes","MATERIAL_FILENAME",BUILDING_KIND_FOG_TILE_MATERIAL);
			TargetMaterial=Resources.Load ("Buildings/"+sReadout,typeof(Material)) as Material;
		}
		if(fogOfWar==true&tileStatusConstant==TILE_STATUS_WORKED|tileStatusConstant==TILE_STATUS_COMBATANT)
		{
			string sReadout=iniRead("buildingtypes","MATERIAL_FILENAME",BUILDING_KIND_BEING_SCOUTED);
			TargetMaterial=Resources.Load ("Buildings/"+sReadout,typeof(Material)) as Material;
		}
		if(fogOfWar==false&tileStatusConstant==TILE_STATUS_UNWORKED&buildingKindConstant==BUILDING_KIND_NONE)
		{
			int iKind=BUILDING_KIND_NONE;
			AttachChildBuilding(iKind,TileRefreshed);
			//string sReadout=iniRead("buildingtypes","MATERIAL_FILENAME",BUILDING_KIND_NONE);
			//TargetMaterial=Resources.Load (sReadout,typeof(Material)) as Material;
		}
		if(fogOfWar==false&(tileStatusConstant==TILE_STATUS_WORKED|tileStatusConstant==TILE_STATUS_COMBATANT)&buildingKindConstant==BUILDING_KIND_NONE)
		{
			int iKind=BUILDING_KIND_WORKED_TILE_MATERIAL;
			AttachChildBuilding(iKind,TileRefreshed);
			//string sReadout=iniRead("buildingtypes","MATERIAL_FILENAME",BUILDING_KIND_WORKED_TILE_MATERIAL);
			//TargetMaterial=Resources.Load (sReadout,typeof(Material)) as Material;
		}
		if(fogOfWar==false&buildingKindConstant!=BUILDING_KIND_NONE)
		{
			string sReadout=iniRead("buildingtypes","MATERIAL_FILENAME",buildingKindConstant);
			if(AttachChildBuilding(buildingKindConstant,TileRefreshed))
			{
				if(tileStatusConstant==TILE_STATUS_WORKED){AttachChildBuilding(BUILDING_KIND_WORKED_TILE_MATERIAL,TileRefreshed);}
				if(tileStatusConstant==TILE_STATUS_UNWORKED)  AttachChildBuilding(BUILDING_KIND_NONE,TileRefreshed);
			}
			else TargetMaterial=Resources.Load ("Buildings/"+sReadout,typeof(Material)) as Material;
		}
		if(TargetTemplate.ColonistHere>=0)
		{
			if(InhabitantList[TargetTemplate.ColonistHere].Dead==true) {TargetTemplate.ColonistHere=-1;TargetTemplate.tileStatusConstant=TILE_STATUS_UNWORKED;}
			else {	
				if(fogOfWar==false|InhabitantList[TargetTemplate.ColonistHere].EnemyType==0|GetIsTargetingPlayerCreature(InhabitantList[TargetTemplate.ColonistHere]))//if it is an explored tile or a friendly creature or targeting a friendly creature
				{AttachChildCreature(InhabitantList[TargetTemplate.ColonistHere].Species,TileRefreshed,TargetTemplate.ColonistHere);}//then we display the creature
				bool bFriendly=InhabitantList[TargetTemplate.ColonistHere].EnemyType==0;
				if(fogOfWar==false&buildingKindConstant==BUILDING_KIND_NONE&tileStatusConstant==TILE_STATUS_COMBATANT&bFriendly)
				{
					//string sReadout=iniRead("buildingtypes","MATERIAL_FILENAME",BUILDING_KIND_COMBATANT);
					//TargetMaterial=Resources.Load (sReadout,typeof(Material)) as Material;
				}
				if(fogOfWar==false&buildingKindConstant==BUILDING_KIND_NONE&tileStatusConstant==TILE_STATUS_COMBATANT&bFriendly==false)
				{
					string sReadout=iniRead("buildingtypes","MATERIAL_FILENAME",BUILDING_KIND_ENEMY);
					TargetMaterial=Resources.Load ("Buildings/"+sReadout,typeof(Material)) as Material;
				}
			}
		}
		TileRefreshed.GetComponent<MeshFilter>().renderer.material=TargetMaterial;		
	}

	GameObject GetTileObj(int xCoord,int zCoord,int area_indx)
	{
		if(xCoord<0|zCoord<0) return null;
		TILE_TEMPLATE TileInQuestion=AreaList[area_indx].TileList[xCoord,zCoord];
		//return TileInQuestion.AssociatedGameObject;
		GameObject[] TileList=new GameObject[TILE_X_SIZE*TILE_Z_SIZE];
		TileList=GameObject.FindGameObjectsWithTag("Tile");
		foreach (GameObject Tile in TileList)
		{
			if(Tile.GetComponent<TileLocation>().xCoord==xCoord&Tile.GetComponent<TileLocation>().zCoord==zCoord) return Tile;
		}
		return null;
	}

    void SetMusic(int iMusType)
    {
        GetComponent<MusicManager>().iMusState = iMusType;
    }

	void SetJob(JOB_TEMPLATE Job,int JobType)
	{
		Job.job_name=iniRead("jobtypes","TRIVIAL_NAME",JobType);
		Job.job_type=JobType;
	}

	[Serializable] public class JOB_TEMPLATE
	{
		public int job_type=JOB_TYPE_IDLE;
		public string job_name="Idle";
		public int job_param1;//for construction, this means building type
		public int xCoordinateStructure;
		public int zCoordinateStructure;
		public int xCoordinateTarget;
		public int zCoordinateTarget;
	}

	int GetBaseSkillValue(int iMax)	{return UnityEngine.Random.Range (0,iMax+1);}//0-iMax

	bool GiveXP(int iAward,INHABITANT_TEMPLATE iColonist)
	{
		if(iAward<=0) return false;
		iColonist.xp+=iAward;
		if(iColonist.xp>=iColonist.GetNextLevelRequiredXP(iColonist.level))
		{
			iColonist.skillpoints=iColonist.skillpoints+iniReadint ("species","SKILLPOINTSPERLEVEL",iColonist.Species);
			iColonist.level++;
			return true;
		}
		return false;
	}

	public bool AwardXP(int iJobDC,INHABITANT_TEMPLATE iColonist)
	{
		int iAward=(iJobDC-(iColonist.level+5))*5;//so if you're level 1, completing a DC 6 task grants no xp, DC 11 grants 25 xp, DC 16 grants 50, DC 21 grants 75 xp
		if(GetIsTargetCombatSkill(iniReadint("jobtypes","SPECIAL_FLAG",iColonist.MyJob.job_type))) iAward+=25;//Combat is more valuable for xp
		return GiveXP(iAward,iColonist);
	}

	[Serializable] public class INHABITANT_TEMPLATE
	{
		//returns the amount of xp required for next level if current level is iLevel (so level 1 returns 100 xp)
		public int GetNextLevelRequiredXP(int iLevel)
		{
			int iReturn=0;
			while(iLevel>0)
			{
				iReturn=iReturn+iLevel*100;
				iLevel--;
			}
			return iReturn;
		}
		public int EnemyType=0;
		public int age=0;
		public bool bOldEnoughToWork=false;
		public int indx=0;
		public int HP=1;
		public int MAXHP=1;
		public int xp=0;
		public int level=1;
		public int skillpoints=0;
		public int[] EventInts=new int[AveFileReaders.MAX_2DA_SIZE];
		public bool AttemptSpendSkillPoint(int iSkill)
		{
			if(skillpoints>0)
			{
				SkillsList[iSkill]++;
				skillpoints--;
				return true;
			}
			return false;
		}		
		public EndTurnJobReport LAST_TURN_JOB=new EndTurnJobReport();
		public int AreaIndex;
		public int Species=SPECIES_HUMAN;
		public JOB_TEMPLATE MyJob=new JOB_TEMPLATE();
		public string Name;
		public bool Dead=false;
		public int LongDead=0;
		public void KillColonist()
		{
			MyJob.job_name="Dead";
			Dead=true;
		}
		public List<int> SkillsList=new List<int>();
		public int[] GearSlots=new int[4];
		public string[] GearNames=new string[4];
		public INHABITANT_TEMPLATE(){}
		public INHABITANT_TEMPLATE(string AssignedName,int iLevel)
		{
			Dead=false;
			Name=AssignedName;
			Species=SPECIES_HUMAN;
		}
	}

	[Serializable] public class BUILDING_TEMPLATE
	{
		public int building_kind=BUILDING_KIND_NONE;
		public bool bHostile=false;
	}

	[Serializable] public class TILE_TEMPLATE
	{
		public List<int> TileResources=new List<int>();
		public int xCoord;
		public int zCoord;
		public BUILDING_TEMPLATE thisTemplate=new BUILDING_TEMPLATE();
		public int tileStatusConstant;
		public bool fogOfWar;
		public int ColonistHere=-1;//Has to be a pointer to the colonist working here.
		//public GameObject AssociatedGameObject;//Can't serialize pointers to GameObjects; therefore this must be removed from the data structure in order for TILE_TEMPLATE to be part of savegames

		public TILE_TEMPLATE()//this constructor generates a blank tile
		{
		}

		public TILE_TEMPLATE(int xCoordConstructor,int zCoordConstructor,int TERRAIN_TYPE)//This constructor generates a random tile
		{
			xCoord = xCoordConstructor;
			zCoord = zCoordConstructor;
			tileStatusConstant=TILE_STATUS_UNWORKED;
			fogOfWar=true;
		}
	}
	
	public const int TILE_X_SIZE=15;
	public const int TILE_Z_SIZE=15;

	[Serializable] public class AREA_TEMPLATE
	{
		public int TERRAIN_TYPE=AREA_TERRAIN_PLAINS;
		public TILE_TEMPLATE[,] TileList=new TILE_TEMPLATE[TILE_X_SIZE,TILE_Z_SIZE];
	}
	
	TILE_TEMPLATE DoTileResources(TILE_TEMPLATE ModifiedTile)
	{
		ModifiedTile.TileResources.Add(0);//Make it zero-based
		string sName=" ";
		int iMax=0;
		int iMin=0;
		int iLoop=1;
		while(iLoop<AveFileReaders.MAX_2DA_SIZE&&sName!="")
		{
			iMin=iniReadint("tileresources","MIN_AMOUNT_DEFAULT",iLoop);
			iMax=iniReadint("tileresources","MAX_AMOUNT_DEFAULT",iLoop);
			ModifiedTile.TileResources.Add(UnityEngine.Random.Range (iMin,iMax+1));
			iLoop++;
			sName=iniRead ("tileresources","RESOURCENAME",iLoop);
		}
		return ModifiedTile;
	}

	AREA_TEMPLATE CreateNewRandomArea()
	{
		AREA_TEMPLATE ValueToReturn=new AREA_TEMPLATE();
		int zLoop=0;
		int xLoop=0;
		while(zLoop<TILE_Z_SIZE)
		{
			while(xLoop<TILE_X_SIZE)
			{
				ValueToReturn.TileList[xLoop,zLoop]=new TILE_TEMPLATE(xLoop,zLoop,ValueToReturn.TERRAIN_TYPE);
				ValueToReturn.TileList[xLoop,zLoop]=DoTileResources(ValueToReturn.TileList[xLoop,zLoop]);
				xLoop=xLoop+1;
			}
			xLoop=0;
			zLoop=zLoop+1;
		}
		return ValueToReturn;
	}

	void UnloadAreaTemplate()
	{
		GameObject[] TileList=new GameObject[TILE_X_SIZE*TILE_Z_SIZE];
		TileList=GameObject.FindGameObjectsWithTag("Tile");
		foreach (GameObject Tile in TileList)
		{
			DestroyObject (Tile);
		}
	}

	void LoadAreaTemplate(AREA_TEMPLATE thisTemplate)
	{
		TILE_TEMPLATE thisTile=new TILE_TEMPLATE();
		int zLoop=0;
		int xLoop=0;
		while(zLoop<TILE_Z_SIZE)
		{
			while(xLoop<TILE_X_SIZE)
			{
				thisTile=thisTemplate.TileList[xLoop,zLoop];
				Quaternion qTarget=new Quaternion(0f,180f,0f,0f);
				GameObject CreatedTile=Instantiate (TILE_PREFAB,new Vector3(xLoop,0f,zLoop),qTarget) as GameObject;
				//thisTile.AssociatedGameObject=CreatedTile;
				CreatedTile.GetComponent<TileLocation>().xCoord=thisTile.xCoord;//Better to be passing around "pointers" to the tile than making extra copies of the tile itself, for obvious reasons.
				CreatedTile.GetComponent<TileLocation>().zCoord=thisTile.zCoord;
				RefreshTile (CreatedTile);		
				xLoop=xLoop+1;
			}
			xLoop=0;
			zLoop=zLoop+1;
		}
	}

	AREA_TEMPLATE AddFinishedStructureToArea(AREA_TEMPLATE targetArea,int xCoord,int zCoord,int BuildingKind,bool bHostile)
	{
		//targetArea.TileList[xCoord,zCoord].fogOfWar=false;
		targetArea.TileList[xCoord,zCoord].thisTemplate.building_kind=BuildingKind;
		targetArea.TileList[xCoord,zCoord].thisTemplate.bHostile=bHostile;
		return targetArea;
	}
	
	void ExploreTile(AREA_TEMPLATE targetArea,int xCoord,int zCoord) {targetArea.TileList[xCoord,zCoord].fogOfWar=false;}

	void ExploreSeveralTiles(int iAreaIndx,int xCoordMin,int zCoordMin,int xCoordMax,int zCoordMax)
	{
		int xCoordLoop=xCoordMin;
		int zCoordLoop=zCoordMin;
		while(xCoordLoop<=xCoordMax)
		{
			while(zCoordLoop<=zCoordMax)
			{
				ExploreTile(AreaList[iAreaIndx],xCoordLoop,zCoordLoop);
				RefreshTile(GetTileObj(xCoordLoop,zCoordLoop,iAreaIndx));
				zCoordLoop=zCoordLoop+1;
			}
			zCoordLoop=zCoordMin;
			xCoordLoop=xCoordLoop+1;
		}
	}
	
	public int VIEWED_AREA_INDEX=0;	
	//public ArrayList AreaList=new ArrayList();
	public List<AREA_TEMPLATE> AreaList=new List<AREA_TEMPLATE>();
	public List<int> resourceStockpiles=new List<int>();
	public List<string> resourceNames=new List<string>();
	public List<bool> TechsResearched=new List<bool>();
	public List<bool> TypesCloned=new List<bool>();

	public bool GetKnowsTech(int iTechIndx)
	{
		if(iTechIndx==1) return true;
		if(iTechIndx==2) return false;
		return TechsResearched[iTechIndx];
	}

	void SetupResourceLists()
	{
		resourceStockpiles.Add(0);//To make it one-based
		resourceNames.Add ("0");
		string sName=" ";
		int iLoop=0;
		while(iLoop<AveFileReaders.MAX_2DA_SIZE&sName!="")
		{
			iLoop++;
			resourceStockpiles.Add(iniReadint("tileresources","STARTER_AMOUNT",iLoop));
			sName=iniRead("tileresources","RESOURCENAME",iLoop);
			resourceNames.Add(sName);
		}		
	}
	
	void ConfigureStartingTechs()
	{
		int iLoop=0;
		while(iLoop<AveFileReaders.MAX_2DA_SIZE)
		{
			TechsResearched.Add (false);
			iLoop++;
		}	
	}


	void ConfigureStartingCloneLists()
	{
		int iLoop=0;
		while(iLoop<AveFileReaders.MAX_2DA_SIZE)
		{
			TypesCloned.Add (false);
			iLoop++;
		}
	}

	void ConfigureStartingHuts(int AREA_INDEX)
	{
		List<int> liBuildingCache=new List<int>();
		liBuildingCache.Add(0);
		int i2DALoop=1;
		while(i2DALoop<AveFileReaders.MAX_2DA_SIZE)
		{
			liBuildingCache.Add(iniReadint ("buildingtypes","WILDRATE",i2DALoop));
			i2DALoop++;
		}
		int iXLoop=0;
		int iZLoop=0;
		while(iXLoop<TILE_X_SIZE)
		{
			iZLoop=0;
			while(iZLoop<TILE_Z_SIZE)
			{
				i2DALoop=1;
				while(i2DALoop<liBuildingCache.Count)
				{
					if(liBuildingCache[i2DALoop]>0)
					{
						if(UnityEngine.Random.Range (1,liBuildingCache[i2DALoop]+1)==1)
						{
							AddFinishedStructureToArea(AreaList[AREA_INDEX],iXLoop,iZLoop,i2DALoop,true);
						}
					}
					i2DALoop++;
				}
				iZLoop++;
			}
			iXLoop++;
		}
	}

	// Use this for initialization
	void Start()
	{
		ConfigureStartingCloneLists();
		configureStartingGearStockpiles();
		ConfigureStartingTechs();
		configureStartingEventFlags ();
		SetupResourceLists();
		AreaList.Add(CreateNewRandomArea());
		ConfigureStartingHuts(VIEWED_AREA_INDEX);
		AreaList[VIEWED_AREA_INDEX]=AddFinishedStructureToArea(AreaList[VIEWED_AREA_INDEX],7,7,BUILDING_KIND_COMMAND_CENTER,false);
		ExploreSeveralTiles(VIEWED_AREA_INDEX,5,5,9,9);
		LoadAreaTemplate(AreaList[VIEWED_AREA_INDEX]);
		configureStartingColonists();
		OrderSkillsForMenu();
		ColListSort (-1);
	}

	public delegate void ButtonClickHandler(int Receipt);

	public delegate void TextEntryHandler(string Receipt);

	public class MenuButton
	{
		public int InputIndex;
		public ButtonClickHandler ThisButtonHandler;
		public TextEntryHandler ThisTextHandler;
		public bool IsTextField=false;
		public string InputText="";
		public string TextDisplay;
		public string MouseOverText;
		public MenuButton()
		{
		}
		public MenuButton(string txt,TextEntryHandler handler,string sDefault,string sToolTip)
		{
			ThisTextHandler=handler;
			TextDisplay=txt;
			MouseOverText=sToolTip;
			IsTextField=true;
			InputText=sDefault;
		}
		public MenuButton(string txt,ButtonClickHandler handler,int indx,string sToolTip)
		{
			InputIndex=indx;
			ThisButtonHandler=handler;
			TextDisplay=txt;
			MouseOverText=sToolTip;
			IsTextField=false;
		}
	}

	//public MenuButton[] ButtonList=new MenuButton[size should be changeable];
	//public ArrayList ButtonList=new ArrayList();
	public List<MenuButton> ButtonList=new List<MenuButton>();
	public string ButtonListHeaderText="";

	void DrawMenuFromButtonList()
	{
		scrollPosition = GUI.BeginScrollView(new Rect(10, 10, 225, 1000), scrollPosition, new Rect(0, 0, 225, 250));
		int yCoord=25;
		if(ButtonListHeaderText!="")
		{
			GUI.Label(new Rect(25,yCoord,200,100),ButtonListHeaderText);
			yCoord=yCoord+100;
		}
		bool bOnce=true;
		int iLoop=0;
		MenuButton ThisButton=new MenuButton();
		while(iLoop<ButtonList.Count)
		{
			ThisButton=ButtonList[iLoop];
			if(ThisButton.IsTextField==false)
			{
				if(GUI.Button(new Rect(25,yCoord,200,20),new GUIContent(ThisButton.TextDisplay,ThisButton.MouseOverText)))
				{
					ThisButton.ThisButtonHandler(ThisButton.InputIndex);
				}
			}
			else
			{
				ThisButton.InputText=GUI.TextField(new Rect(25,yCoord,200,20),ThisButton.InputText,40);
				ThisButton.ThisTextHandler(ThisButton.InputText);
			}
			if(bOnce) {GUI.Label(new Rect(25,500,200,120), GUI.tooltip);}
			yCoord=yCoord+25;
			iLoop++;
		}
		GUI.EndScrollView();
		if(selectedMenuNode==MENU_NODE_VIEW_ALLSTATS) ReviewColonyStats();
	}

	[Serializable] public class EndTurnJobReport
	{
		public bool SuccessFail;
		public int JobType;
		public int DC;
		public int Skill;
		public int Roll;
		public string InvalidReason;
	}

	int GetDCOfJob(INHABITANT_TEMPLATE ProcessedColonist,TILE_TEMPLATE TargetTile)
	{
		if(ProcessedColonist.Dead) {FireColonistFromJob (ProcessedColonist); return -1;}
		int JobNum=ProcessedColonist.MyJob.job_type;
		int SpecialFlag=iniReadint("jobtypes","SPECIAL_FLAG",JobNum);
		int JobDC=0;
		JobDC=JobDC-iniReadint("areatypes","BONUS_"+iniRead("jobtypes","TRIVIAL_NAME",JobNum),AreaList[ProcessedColonist.AreaIndex].TERRAIN_TYPE);//Terrain DC bonus
		if(SpecialFlag==0)//Gathering
		{
			int iResType=iniReadint("jobtypes","RESOURCE_GATHERED",JobNum);
			JobDC=JobDC+iniReadint("tileresources","DEFAULT_DIFFICULTY",iResType);//Base DC for harvesting
		}
		if(SpecialFlag==1)//Construction
		{
			int iConstructType=ProcessedColonist.MyJob.job_param1;
			JobDC=JobDC+iniReadint ("buildingtypes","BUILD_DIFFICULTY",iConstructType);
		}
		if(SpecialFlag==4)//Tech
		{
			int iTechType=ProcessedColonist.MyJob.job_param1;
			JobDC=JobDC+iniReadint ("techtree","Difficulty",iTechType);
		}
		if(SpecialFlag==7) {JobDC=JobDC+10;}//Destasising
		if(SpecialFlag==5) {JobDC=-10;}//Learning
		int iTarget=TargetTile.ColonistHere;
		if(SpecialFlag==8)//Combat
		{
			if(iTarget<0) return -1;
			JobDC+=InhabitantList[iTarget].level;
			int iRange=GetTileDistance (ProcessedColonist.MyJob.xCoordinateStructure,ProcessedColonist.MyJob.zCoordinateStructure,ProcessedColonist.MyJob.xCoordinateTarget,ProcessedColonist.MyJob.zCoordinateTarget);
			int iRangeMod=0;
			int iLoop=1;
			while(iLoop<4)
			{
				iRangeMod+=iniReadint("geartypes","RANGE"+iRange,ProcessedColonist.GearSlots[iLoop]);
				iLoop++;
			}
			JobDC-=iRangeMod;
		}
		if(SpecialFlag==9)//craftsman
		{
			int iGear=ProcessedColonist.MyJob.job_param1;
			JobDC=JobDC+iniReadint ("geartypes","BUILDDC",iGear);
		}
		if(SpecialFlag==10)//demolitions
		{
			int iKind=TargetTile.thisTemplate.building_kind;
			JobDC=JobDC+(iniReadint("buildingtypes","BUILD_DIFFICULTY",iKind)/2);
		}
		if(SpecialFlag==11)//Propaganda
		{
			if(iTarget<0) return -1;
			JobDC+=(InhabitantList[iTarget].HP/3);
		}
		if(SpecialFlag==12)//Healing
		{}//At present, there are no modifiers to healing.
		return JobDC+10;
	}

	bool GetIsTargetCombatSkill(int iFlag)//Returns true for combat-related skills such as attacking, propaganda, and healing
	{
		if(iFlag==8|iFlag==11|iFlag==12) return true;
		return false;
	}

	bool GetIsNoSkillJob(int iSpecialFlag)
	{
		if(iSpecialFlag==3|iSpecialFlag==6|iSpecialFlag==5) return true;
		return false;
	}

	void ProcessColonistEndTurn(INHABITANT_TEMPLATE ProcessedColonist)
	{
		if(ProcessedColonist.Dead) {FireColonistFromJob (ProcessedColonist); return;}
		if(ProcessedColonist.MyJob.job_type==JOB_TYPE_IDLE) return;
		int xTar=ProcessedColonist.MyJob.xCoordinateTarget;
		int zTar=ProcessedColonist.MyJob.zCoordinateTarget;
		TILE_TEMPLATE TargetTile=AreaList[ProcessedColonist.AreaIndex].TileList[xTar,zTar];
		TILE_TEMPLATE OriginTile=AreaList[ProcessedColonist.AreaIndex].TileList[ProcessedColonist.MyJob.xCoordinateStructure,ProcessedColonist.MyJob.zCoordinateStructure];
		int iDC=GetDCOfJob(ProcessedColonist,TargetTile);
		int JobNum=ProcessedColonist.MyJob.job_type;
		int SpecialFlag=iniReadint("jobtypes","SPECIAL_FLAG",JobNum);
		int SkillAtJob=ProcessedColonist.SkillsList[JobNum];
		int iLoop=0;
		while(iLoop<4)
		{
			if(iniReadint("geartypes","SKILL_TYPE",ProcessedColonist.GearSlots[iLoop])==JobNum) SkillAtJob+=iniReadint("geartypes","SKILL_BONUS",ProcessedColonist.GearSlots[iLoop]);
			iLoop++;
		}
		if(GetIsNoSkillJob(SpecialFlag)) SkillAtJob=0;//No need to have skill at learning
		int iRoll=UnityEngine.Random.Range (1,21);		
		bool SuccessFail=(iRoll+SkillAtJob>=iDC);
		if(DEBUGMODE==1) SuccessFail=true;
		ProcessedColonist.LAST_TURN_JOB.JobType=JobNum;
		if(SpecialFlag==3) {ProcessedColonist.LAST_TURN_JOB.InvalidReason="No job";return;}//idle
		ProcessedColonist.LAST_TURN_JOB.Roll=iRoll;
		ProcessedColonist.LAST_TURN_JOB.DC=iDC;
		ProcessedColonist.LAST_TURN_JOB.Skill=SkillAtJob;
		ProcessedColonist.LAST_TURN_JOB.SuccessFail=SuccessFail;
		if(SuccessFail)
		{
			if(SpecialFlag==0)//Normal gathering tasks
			{
				int ResourceType=iniReadint("jobtypes","RESOURCE_GATHERED",JobNum);
				int ResourceGain=TargetTile.TileResources[ResourceType];
				int nBonus=iniReadint("buildingtypes","PROD_BONUS",OriginTile.thisTemplate.building_kind);
				resourceStockpiles[ResourceType]+=ResourceGain+nBonus;
			}
			if(SpecialFlag==1)//Construction
			{
				int iCostType=iniReadint ("buildingtypes","COST_TYPE",ProcessedColonist.MyJob.job_param1);
				int iCostAmount=iniReadint ("buildingtypes","COST_AMOUNT",ProcessedColonist.MyJob.job_param1);
				if(resourceStockpiles[iCostType]<iCostAmount)
				{
					ProcessedColonist.LAST_TURN_JOB.InvalidReason="Insufficient resources."; return;
				}
				else
				{
					resourceStockpiles[iCostType]-=iCostAmount;
					AddFinishedStructureToArea(AreaList[ProcessedColonist.AreaIndex],xTar,zTar,ProcessedColonist.MyJob.job_param1,false);
					RefreshTile(GetTileObj(xTar,zTar,ProcessedColonist.AreaIndex));
				}
				FireColonistFromJob (ProcessedColonist);
			}
			if(SpecialFlag==2)//Scouting
			{
				ExploreSeveralTiles(ProcessedColonist.AreaIndex,TargetTile.xCoord-1,TargetTile.zCoord-1,TargetTile.xCoord+1,TargetTile.zCoord+1);
				//TargetTile.fogOfWar=false;
				FireColonistFromJob (ProcessedColonist);
			}
			if(SpecialFlag==4)//research
			{
				TechsResearched[ProcessedColonist.MyJob.job_param1]=true;
				FireColonistFromJob (ProcessedColonist);
			}
			if(SpecialFlag==5)//learning
			{
				ProcessedColonist.AttemptSpendSkillPoint(ProcessedColonist.MyJob.job_param1);
				FireColonistFromJob (ProcessedColonist);
			}
			if(SpecialFlag==10)//Demolitions
			{
				int iBuildingKind=TargetTile.thisTemplate.building_kind;
				int iDemolishSkill=iniReadint ("buildingtypes","DEMOLISHABLE",iBuildingKind);
				if(iDemolishSkill==JobNum)
				{
					TargetTile.thisTemplate.building_kind=BUILDING_KIND_NONE;
					//Destruction event code
					PersistAreaLoop=ProcessedColonist.AreaIndex;
					PersistTileLoopX=xTar;
					PersistTileLoopZ=zTar;
					int[] ParamArray=new int[4];
					int iEventLoop=1;
					while(iEventLoop<MAX_EVENT_OPTS)
					{
						ParamArray[iEventLoop]=iniReadint("buildingtypes","DESTROYPARAM"+iEventLoop,iBuildingKind);//Gather vars
						Debug.Log("Parameter "+iEventLoop+" is "+ParamArray[iEventLoop]);
						iEventLoop++;
					}
					PersistInhabitantLoop=ProcessedColonist.indx;
					DoAnEventOutcome(iniRead("buildingtypes","DESTROYEVENT",iBuildingKind),ParamArray);
					//Destruction event code ends
					RefreshTile(GetTileObj(xTar,zTar,ProcessedColonist.AreaIndex));
					FireColonistFromJob (ProcessedColonist);
				}
				else
				{
					ProcessedColonist.LAST_TURN_JOB.InvalidReason="Wrong skill. Use "+iniRead ("jobtypes","TRIVIAL_NAME",iDemolishSkill)+" instead.";
					FireColonistFromJob (ProcessedColonist);
				}
			}
			if(SpecialFlag==9)//craftsman
			{
				int iCostType=iniReadint ("geartypes","COST_TYPE",ProcessedColonist.MyJob.job_param1);
				int iCostAmount=iniReadint ("geartypes","COST_AMOUNT",ProcessedColonist.MyJob.job_param1);
				if(resourceStockpiles[iCostType]<iCostAmount)
				{
					ProcessedColonist.LAST_TURN_JOB.InvalidReason="Insufficient resources."; return;
				}	
				else
				{
					resourceStockpiles[iCostType]-=iCostAmount;
					FireColonistFromJob (ProcessedColonist);
					GEARSTOCKPILES[ProcessedColonist.MyJob.job_param1]++;
				}
			}
			if(SpecialFlag==7)//Destasising
			{
				FireColonistFromJob (ProcessedColonist);
				if(StasisPods<1)
				{
					ProcessedColonist.LAST_TURN_JOB.InvalidReason="Insufficient stasis pods."; return;
				}
				else
				{
					StasisPods--;
					int iMinAge=GetAdultAge (SPECIES_HUMAN);
					CreateNewColonists(1,SPECIES_HUMAN,iMinAge+UnityEngine.Random.Range (0,iMinAge+1),0,ProcessedColonist.AreaIndex);
				}
			}
			if(GetIsTargetCombatSkill(SpecialFlag))//Combat-related roles
			{
				int iTarget=TargetTile.ColonistHere;
				int iDist=GetTileDistance (xTar,zTar,ProcessedColonist.MyJob.xCoordinateStructure,ProcessedColonist.MyJob.zCoordinateStructure);
				if(SpecialFlag==8)//Attacking
				{
					int iWeapon=ProcessedColonist.GearSlots[1];
					int iArmor=ProcessedColonist.GearSlots[2];
					int iAccessory=ProcessedColonist.GearSlots[3];
					int iRange=iniReadint("jobtypes","RANGE",iniReadint("geartypes","COMBATSKILL",iWeapon));
					if(iTarget==-1||iDist>iRange){ProcessedColonist.LAST_TURN_JOB.InvalidReason="No target.";return;}
					int iDamage=UnityEngine.Random.Range(iniReadint ("geartypes","DAMMIN",iWeapon),iniReadint("geartypes","DAMMAX",iWeapon)+1);
					iDamage+=UnityEngine.Random.Range(iniReadint ("geartypes","DAMMIN",iWeapon),iniReadint("geartypes","DAMMAX",iArmor)+1);
					iDamage+=UnityEngine.Random.Range(iniReadint ("geartypes","DAMMIN",iWeapon),iniReadint("geartypes","DAMMAX",iAccessory)+1);
					iDamage-=iniReadint("geartypes","DEF",InhabitantList[iTarget].GearSlots[1]);
					iDamage-=iniReadint("geartypes","DEF",InhabitantList[iTarget].GearSlots[2]);
					iDamage-=iniReadint("geartypes","DEF",InhabitantList[iTarget].GearSlots[3]);
					InflictDamage(InhabitantList[iTarget],iDamage);
					RefreshTile(GetTileObj(xTar,zTar,ProcessedColonist.AreaIndex));
				}
				if(SpecialFlag==11)//Propaganda
				{
					if(iTarget==-1){ProcessedColonist.LAST_TURN_JOB.InvalidReason="No target.";FireColonistFromJob (ProcessedColonist);return;}
					if(InhabitantList[iTarget].EnemyType==0){ProcessedColonist.LAST_TURN_JOB.InvalidReason="You cannot convert a unit that is already friendly.";return;}
					InhabitantList[iTarget].EnemyType=0;
					AddCapturedGear(InhabitantList[iTarget].GearSlots[1]);AddCapturedGear(InhabitantList[iTarget].GearSlots[2]);AddCapturedGear(InhabitantList[iTarget].GearSlots[3]);
					InhabitantList[iTarget].GearSlots[1]=GEAR_TYPE_HAND_NONE;InhabitantList[iTarget].GearSlots[2]=GEAR_TYPE_ARMOR_NONE;InhabitantList[iTarget].GearSlots[3]=GEAR_TYPE_ACCESSORY_NONE;
				}
				if(SpecialFlag==12)//Healing
				{
					if(iTarget==-1){ProcessedColonist.LAST_TURN_JOB.InvalidReason="No target.";return;}
					if(HealDamage(InhabitantList[iTarget],iniReadint("buildingtypes","PROD_BONUS",OriginTile.thisTemplate.building_kind))==false)
					{ProcessedColonist.LAST_TURN_JOB.InvalidReason="Invalid target for healing."; return;}
					//FireColonistFromJob (ProcessedColonist);
				}
				if(SpecialFlag==13)//Cloning
				{
					TypesCloned[ProcessedColonist.Species]=true;
				}
			}
			AwardXP(iDC,ProcessedColonist);
		}
	}

	string GetRandomColonistName(int iSpecies)
	{
		string sName="";
		string sIni=iniRead ("species","NAMEFILE",iSpecies);
		int iNumSyllables=iniReadint (sIni,"MAX_SYLLABLES",1);
		int iNumEntries=iniReadint (sIni,"NUMBER_ENTRIES",1);
		int iLoop=0;
		while(iLoop<iNumSyllables)
		{
			iLoop++;
			sName+=iniRead(sIni,"SYLLABLE"+iLoop,UnityEngine.Random.Range (1,iNumEntries+1));
		}
		return sName;
	}

	int GetWorkingAge(int iSpecies){return iniReadint("species","AGE_WORK",iSpecies);}
	int GetAdultAge(int iSpecies){return iniReadint ("species","AGE_ADULT",iSpecies);}
	int GetOldAge(int iSpecies){return iniReadint ("species","AGE_OLD",iSpecies);}
	int GetHPGain(int iSpecies){return UnityEngine.Random.Range(0,1+iniReadint ("species","HITPOINT_GAIN",iSpecies));}
	int GetHPLose(int iSpecies){return UnityEngine.Random.Range(0,1+iniReadint ("species","HITPOINT_LOSE",iSpecies));}

	bool HealDamage(INHABITANT_TEMPLATE itHealed,int iHeal)
	{
		if(itHealed.HP>0)//The dead cannot be healed.
		{
			if(itHealed.HP>=itHealed.MAXHP) return false;//Do nothing if already at max health
			itHealed.HP+=iHeal;
			if(itHealed.HP>itHealed.MAXHP) itHealed.HP=itHealed.MAXHP;
			return true;
		}
		return false;
	}

	void AddCapturedGear(int iType)
	{
		int iOutCome=iniReadint("geartypes","ONCAPTURE",iType);
		if(iOutCome==-1) return;
		if(iOutCome==0) {GEARSTOCKPILES[iType]++; return;}
		GEARSTOCKPILES[iOutCome]++;
	}	

	void InflictDamage(INHABITANT_TEMPLATE iDamaged,int iDamage)
	{
		iDamaged.HP-=iDamage;
		if(iDamaged.HP<=0)
		{
			FireColonistFromJob(iDamaged);
			iDamaged.KillColonist();
			AddCapturedGear(iDamaged.GearSlots[1]); AddCapturedGear(iDamaged.GearSlots[2]); AddCapturedGear(iDamaged.GearSlots[3]);
			iDamaged.GearSlots[1]=GEAR_TYPE_HAND_NONE; iDamaged.GearSlots[2]=GEAR_TYPE_ARMOR_NONE; iDamaged.GearSlots[3]=GEAR_TYPE_ACCESSORY_NONE;
			iDamaged.LAST_TURN_JOB.InvalidReason="Died.";
		}
	}

	void AgeColonist(int Years,INHABITANT_TEMPLATE Aged)
	{
		int iGain=0;
		int iLose=0;
		int iSpecies=Aged.Species;
		while(Years>0)
		{
			if(Aged.age>=GetWorkingAge(iSpecies)&Aged.age<GetAdultAge(iSpecies))
			{
				iGain=GetHPGain(iSpecies);
				Aged.HP+=iGain;
				Aged.MAXHP+=iGain;
			}
			if(Aged.age>GetOldAge(iSpecies))
			{
				iLose=GetHPLose(iSpecies);
				Aged.MAXHP-=iLose;
				InflictDamage(Aged,iLose);
			}
			Aged.age++;
			if(Aged.age>=GetWorkingAge (iSpecies))
			{
				Aged.bOldEnoughToWork=true;
			}
			Years--;
		}
	}

	int[] GetRandomEdgePosition()
	{
		int iEdge=UnityEngine.Random.Range (1,4+1);
		int[] iReturn=new int[2];
		if(iEdge==1){iReturn[0]=UnityEngine.Random.Range (2,15+1);iReturn[1]=1;}
		if(iEdge==2){iReturn[0]=UnityEngine.Random.Range (1,14+1);iReturn[1]=15;}
		if(iEdge==3){iReturn[0]=1;iReturn[1]=UnityEngine.Random.Range (1,14+1);}
		if(iEdge==4){iReturn[0]=15;UnityEngine.Random.Range (2,15+1);}
		return iReturn;
	}


	void CreateNewColonists(int Number,int Species,int iAge,int iHostileType,int AreaIndx)
	{
		int iLoop=0;
		int iGear=0;
		while(iLoop<Number)
		{
			InhabitantList.Add (new INHABITANT_TEMPLATE(GetRandomColonistName(Species),1));
			InhabitantList[InhabitantList.Count-1].indx=InhabitantList.Count-1;
			InhabitantList[InhabitantList.Count-1].AreaIndex=AreaIndx;
			InhabitantList[InhabitantList.Count-1].Species=Species;
			int iLoopLoop=1;
			while(iLoopLoop<4)
			{
				if(iHostileType==0) iGear=iLoopLoop;
				else iGear=iniReadint ("enemytypes","EQUIP"+iLoopLoop,iHostileType);
				InhabitantList[InhabitantList.Count-1].GearSlots[iLoopLoop]=iGear;
				InhabitantList[InhabitantList.Count-1].GearNames[iLoopLoop]=iniRead("geartypes","NAME",iGear);
				iLoopLoop++;
			}
			if(iHostileType>0)
			{
				InhabitantList[InhabitantList.Count-1].level=iniReadint ("enemytypes","LEVEL",iHostileType);
				int[]Coords=new int[2];
				Coords=GetRandomEdgePosition();
				InhabitantList[InhabitantList.Count-1].MyJob.xCoordinateStructure=Coords[0];
				InhabitantList[InhabitantList.Count-1].MyJob.zCoordinateStructure=Coords[1];
			}
			if(iAge>1) ConfigureStartingSkills(InhabitantList.Count-1,InhabitantList[InhabitantList.Count-1].level,false);
			else ConfigureStartingSkills(InhabitantList.Count-1,InhabitantList[InhabitantList.Count-1].level,true);
			AgeColonist(iAge,InhabitantList[InhabitantList.Count-1]);
			InhabitantList[InhabitantList.Count-1].MyJob.job_type=JOB_TYPE_IDLE;
			iLoop++;
		}
	}

	void EatFood(INHABITANT_TEMPLATE Eater)
	{
		int iFoodType=iniReadint ("species","EAT_TYPE",Eater.Species);
		int iFoodAmount=iniReadint ("species","EAT_AMOUNT",Eater.Species);
		if(resourceStockpiles[iFoodType]<iFoodAmount)
		{
			int iDamage=iniReadint ("species","STARVE_DAMAGE",Eater.Species);
			Eater.LAST_TURN_JOB.InvalidReason="Starving";
			InflictDamage (Eater,iDamage);
		}
		else resourceStockpiles[iFoodType]-=iFoodAmount;
	}

	void CreateHostileAlien(int iType,int iAreaIndx)
	{
		Debug.Log ("Creating Alien of type "+iType);
		int iSpecies=iniReadint ("enemytypes","SPECI",iType);
		int iAgeMin=iniReadint ("enemytypes","AGEMIN",iType);
		int iAgeMax=iniReadint ("enemytypes","AGEMAX",iType);
		CreateNewColonists (1,iSpecies,UnityEngine.Random.Range (iAgeMin,iAgeMax+1),iType,iAreaIndx);
		InhabitantList[InhabitantList.Count-1].EnemyType=iType;
	}

	void CreateHostileAliens()//call this once per turn
	{
		int[] iaArray=new int[4];
		int MinTurn=0;
		int MaxTurn=0;
		int iLoop=1;
		while(iLoop<AveFileReaders.MAX_2DA_SIZE)
		{
			MinTurn=iniReadint ("enemytypes","SPAWN_START",iLoop);
			if(ThisYear>=MinTurn)
			{
				MaxTurn=iniReadint ("enemytypes","SPAWN_END",iLoop);
				if(MaxTurn==-1|ThisYear<MaxTurn)
				{
					int iLoopLoop=0;
					while(iLoopLoop<AreaList.Count)
					{
						int iMTTH=iniReadint ("enemytypes","SPAWN_MTTH",iLoop);
						if(iMTTH>0)
						{
							iaArray[1]=iniReadint("enemytypes","IFVAR1",iLoop);
							iaArray[2]=iniReadint("enemytypes","IFVAR2",iLoop);
							if(GetConditionMet(iniRead("enemytypes","IF",iLoop),iaArray))
							{
								if(UnityEngine.Random.Range (1,iMTTH+1)==1)
								{
									CreateHostileAlien(iLoop,iLoopLoop);
								}
							}
						}
						iLoopLoop++;
					}
				}
			}
			iLoop++;
		}
	}

	int[] AICheckForTarget(int iX,int iZ,int iRange,bool bFriendly,int iAreaIndx,out bool bDidFind)
	{
		List<int> liTargetListX=new List<int>();
		List<int> liTargetListZ=new List<int>();
		int iLoopX=iX-iRange;
		int iLoopZ=iZ-iRange;
		int iZCapped;
		int iXCapped;
		while(iLoopX<=iX+iRange)
		{
			iXCapped=Mathf.Clamp (iLoopX,0,TILE_X_SIZE-1);
			iLoopZ=iZ-iRange;
			while(iLoopZ<=iZ+iRange)
			{
				iZCapped=Mathf.Clamp(iLoopZ,0,TILE_Z_SIZE-1);
				if(bFriendly)//We are checking for player-controlled characters
				{				
					int iColonist=AreaList[iAreaIndx].TileList[iXCapped,iZCapped].ColonistHere;
					//Debug.Log ("Checking for a victim at "+iXCapped+" "+iZCapped+" and found "+iColonist);
					if(iColonist>-1)
					{
						if(InhabitantList[iColonist].EnemyType==0)
						{
							//Debug.Log ("Found a victim at "+iXCapped+", "+iZCapped+" my coords are "+iX+", "+iZ+" range is "+iRange+" iLoopX is "+iLoopX+" iLoopZ is "+iLoopZ+" iXCapped is "+iXCapped+" iZCapped is "+iZCapped);
							liTargetListX.Add (iXCapped);
							liTargetListZ.Add (iZCapped);
						}
					}
				}
				else//Does the tile contain a player-controlled building?
				{
					BUILDING_TEMPLATE btCheck=AreaList[iAreaIndx].TileList[iXCapped,iZCapped].thisTemplate;
					if(btCheck.bHostile==false&btCheck.building_kind!=BUILDING_KIND_NONE)
					{
						liTargetListX.Add (iXCapped);
						liTargetListZ.Add (iZCapped);
					}
				}
				iLoopZ++;
			}
			iLoopX++;
		}
		int[] iReturn=new int[2];
		if(liTargetListX.Count>0)
		{
			int iIndx=UnityEngine.Random.Range(0,liTargetListX.Count);//This is the index for both X and Z, since it is one victim.
			iReturn[0]=liTargetListX[iIndx];
			iReturn[1]=liTargetListZ[iIndx];
			bDidFind=true;
		}
		else{iReturn[0]=-1;iReturn[1]=-1;bDidFind=false;}
		return iReturn;
	}

	int AIProcessMove(int iIndx,int iMovesLeft)
	{
		INHABITANT_TEMPLATE itEnemy=InhabitantList[iIndx];
		int[] iaArray=new int[4];
		int iType=itEnemy.EnemyType;
		iaArray[1]=iniReadint("enemytypes","IFVAR1",iType);
		iaArray[2]=iniReadint("enemytypes","IFVAR2",iType);
		if(!GetConditionMet(iniRead("enemytypes","IF",iType),iaArray))
		{
			FireColonistFromJob(itEnemy);
			itEnemy.KillColonist();
		}
		if(itEnemy.Dead) return -1;//just a failsafe
		itEnemy.MyJob.xCoordinateStructure=Mathf.Clamp (itEnemy.MyJob.xCoordinateStructure,0,TILE_X_SIZE-1);
		itEnemy.MyJob.zCoordinateStructure=Mathf.Clamp (itEnemy.MyJob.zCoordinateStructure,0,TILE_Z_SIZE-1);
		int iAreaIndx=itEnemy.AreaIndex;
		int TargetRange=iniReadint ("enemytypes","TARGETRANGE",iType);
		int iWeapon=itEnemy.GearSlots[GEAR_TYPE_HAND_NONE];
		int iSkill=iniReadint ("geartypes","COMBATSKILL",iWeapon);
		int iAttackRange=iniReadint ("jobtypes","RANGE",iSkill);
		bool bDidFind;
		int[] Target=AICheckForTarget(itEnemy.MyJob.xCoordinateStructure,itEnemy.MyJob.zCoordinateStructure,iAttackRange,true,itEnemy.AreaIndex,out bDidFind);
		if(bDidFind)//Found a juicy target! KILL IT!
		{
			int iPreserveX=itEnemy.MyJob.xCoordinateStructure;
			int iPreserveZ=itEnemy.MyJob.zCoordinateStructure;
			FireColonistFromJob(InhabitantList[iIndx]);
			InhabitantList[iIndx].MyJob.xCoordinateStructure=iPreserveX;
			InhabitantList[iIndx].MyJob.zCoordinateStructure=iPreserveZ;
			InhabitantList[iIndx].MyJob.xCoordinateTarget=Target[0];
			InhabitantList[iIndx].MyJob.zCoordinateTarget=Target[1];			
			SetJob(InhabitantList[iIndx].MyJob,iSkill);
			//Debug.Log ("Kill found. Job is "+InhabitantList[iIndx].MyJob.job_type+" "+iSkill+" and range is "+GetTileDistance (Target[0],Target[1],itEnemy.MyJob.xCoordinateStructure,itEnemy.MyJob.zCoordinateStructure));
			//Debug.Log ("Target coords are "+Target[0]+", "+Target[1]+" and my coords are "+itEnemy.MyJob.xCoordinateStructure+", "+itEnemy.MyJob.zCoordinateStructure);
			AreaList[iAreaIndx].TileList[itEnemy.MyJob.xCoordinateStructure,itEnemy.MyJob.zCoordinateStructure].tileStatusConstant=TILE_STATUS_COMBATANT;
			AreaList[iAreaIndx].TileList[itEnemy.MyJob.xCoordinateStructure,itEnemy.MyJob.zCoordinateStructure].ColonistHere=iIndx;
			RefreshTile (GetTileObj (itEnemy.MyJob.xCoordinateStructure,itEnemy.MyJob.zCoordinateStructure,iAreaIndx));
			SetMusic(MUS_STATE_WAR);
			return 0;
		}
		else
		{
			Target=AICheckForTarget(itEnemy.MyJob.xCoordinateStructure,itEnemy.MyJob.zCoordinateStructure,TargetRange,true,itEnemy.AreaIndex,out bDidFind);
			if(bDidFind)//Found a target unit, move closer to engage
			{
				int iOldX=InhabitantList[iIndx].MyJob.xCoordinateStructure;
				int iOldZ=InhabitantList[iIndx].MyJob.zCoordinateStructure;
				int iNewX=InhabitantList[iIndx].MyJob.xCoordinateStructure;
				int iNewZ=InhabitantList[iIndx].MyJob.zCoordinateStructure;
				if(Target[0]<iNewX)iNewX--;
				else if(Target[0]>iNewX)iNewX++;
				if(Target[1]<iNewZ)iNewZ--;
				else if(Target[1]>iNewZ)iNewZ++;
				if(AreaList[iAreaIndx].TileList[iNewX,iNewZ].ColonistHere<0)
				{
					FireColonistFromJob (itEnemy);
					itEnemy.MyJob.xCoordinateTarget=0;
					itEnemy.MyJob.zCoordinateTarget=0;
					itEnemy.MyJob.xCoordinateStructure=iNewX;
					itEnemy.MyJob.zCoordinateStructure=iNewZ;
					AreaList[iAreaIndx].TileList[itEnemy.MyJob.xCoordinateStructure,itEnemy.MyJob.zCoordinateStructure].tileStatusConstant=TILE_STATUS_COMBATANT;
					AreaList[iAreaIndx].TileList[itEnemy.MyJob.xCoordinateStructure,itEnemy.MyJob.zCoordinateStructure].ColonistHere=iIndx;
					RefreshTile (GetTileObj (itEnemy.MyJob.xCoordinateStructure,itEnemy.MyJob.zCoordinateStructure,iAreaIndx));
					AreaList[iAreaIndx].TileList[iOldX,iOldZ].tileStatusConstant=TILE_STATUS_UNWORKED;
					AreaList[iAreaIndx].TileList[iOldX,iOldZ].ColonistHere=-1;
					RefreshTile (GetTileObj (iOldX,iOldZ,iAreaIndx));
				}
			}
			else//nothing found. Random walk.
			{
				int iOldX=InhabitantList[iIndx].MyJob.xCoordinateStructure;
				int iOldZ=InhabitantList[iIndx].MyJob.zCoordinateStructure;
				int iNewX=InhabitantList[iIndx].MyJob.xCoordinateStructure+UnityEngine.Random.Range (-1,1+1);
				int iNewZ=InhabitantList[iIndx].MyJob.zCoordinateStructure+UnityEngine.Random.Range (-1,1+1);
				iNewX=Mathf.Clamp (iNewX,0,TILE_X_SIZE-1);
				iNewZ=Mathf.Clamp (iNewZ,0,TILE_Z_SIZE-1);
				if(AreaList[iAreaIndx].TileList[iNewX,iNewZ].ColonistHere<0)
				{
					FireColonistFromJob (itEnemy);
					itEnemy.MyJob.xCoordinateTarget=0;
					itEnemy.MyJob.zCoordinateTarget=0;
					itEnemy.MyJob.xCoordinateStructure=iNewX;
					itEnemy.MyJob.zCoordinateStructure=iNewZ;
					AreaList[iAreaIndx].TileList[itEnemy.MyJob.xCoordinateStructure,itEnemy.MyJob.zCoordinateStructure].tileStatusConstant=TILE_STATUS_COMBATANT;
					AreaList[iAreaIndx].TileList[itEnemy.MyJob.xCoordinateStructure,itEnemy.MyJob.zCoordinateStructure].ColonistHere=iIndx;
					RefreshTile (GetTileObj (itEnemy.MyJob.xCoordinateStructure,itEnemy.MyJob.zCoordinateStructure,iAreaIndx));
					AreaList[iAreaIndx].TileList[iOldX,iOldZ].tileStatusConstant=TILE_STATUS_UNWORKED;
					AreaList[iAreaIndx].TileList[iOldX,iOldZ].ColonistHere=-1;
					RefreshTile (GetTileObj (iOldX,iOldZ,iAreaIndx));
				}
			}
		}		
		iMovesLeft--;
		return iMovesLeft;
	}
	
	void AIHandleMoves()
	{
		int iLoop=0;
		while(iLoop<InhabitantList.Count)
		{
			if(InhabitantList[iLoop].EnemyType>0&InhabitantList[iLoop].Dead==false)
			{
				int iMovesLeft=iniReadint ("enemytypes","SPEED",InhabitantList[iLoop].EnemyType);
				while(iMovesLeft>0)
				{
					iMovesLeft=AIProcessMove(iLoop,iMovesLeft);
				}
			}
			iLoop++;
		}
	}
	const string SEASON_SPRING="Spring";const string SEASON_SUMMER="Summer";const string SEASON_FALL="Fall";const string SEASON_WINTER="Winter";
	public string ThisSeason=SEASON_SPRING;
	public int ThisYear=1;
	bool bColHold=false;
	int iInhabitantEndTurnLoop=0;
	bool bHostiles=false;

	//if beHostiles is true, processes hostiles. Otherwise, proceses friendlies moves.
	void ProcessInhabitants()
	{
		int iMax=InhabitantList.Count;
		INHABITANT_TEMPLATE Inhabitant=InhabitantList[0];
		while(iInhabitantEndTurnLoop<iMax)
		{
			Inhabitant=InhabitantList[iInhabitantEndTurnLoop];
			if((Inhabitant.EnemyType>0&bHostiles==true)|(Inhabitant.EnemyType==0&bHostiles==false))
			{
				Inhabitant.LAST_TURN_JOB.InvalidReason="";
				if(Inhabitant.LongDead==1) Inhabitant.LongDead=2;
				if(Inhabitant.Dead==true&Inhabitant.LongDead==0) Inhabitant.LongDead=1;
				if(Inhabitant.Dead==false&&ThisSeason==SEASON_SPRING) AgeColonist(1,Inhabitant);
				if(Inhabitant.Dead==false&Inhabitant.EnemyType==0) EatFood(Inhabitant);
				if(Inhabitant.Dead==false) ProcessColonistEndTurn(Inhabitant);
				if(bHold==true)
				{
					bColHold=true;
					return;
				}
			}
			iInhabitantEndTurnLoop++;
		}
		if(bHostiles==false)
		{
			if(InhabitantList.Count>9)
			{
				SetMusic (MUS_STATE_LATE);
			}
			else
			{
				SetMusic (MUS_STATE_EARLY);
			}
			DoHostileAI();
		}
		else
		{
			PersistAreaLoop=0;PersistEventKindLoop=0;NextEvent(1);//Then births are handled
		}
	}

	int CountFertileInArea(int iArea,int iSpecies)
	{
		int iCount=0;
		INHABITANT_TEMPLATE itCheck;
		int iColonist=0;
		while(iColonist<InhabitantList.Count)
		{
			itCheck=InhabitantList[iColonist];
			if(itCheck.EnemyType==0&itCheck.Species==iSpecies&GetAdultAge(itCheck.Species)<=itCheck.age&itCheck.Dead==false&itCheck.AreaIndex==iArea) iCount++;
			iColonist++;
		}
		return iCount;
	}

	const int MAX_EVENT_OPTS=3;

	void NextEvent(int iReceipt)
	{
		if(iReceipt==0) ProcessEvents(iReceipt);
		if(iReceipt==1) ProcessBirths(iReceipt);
	}

	void DoAnEventOutcome(string sDo,int[] ParamArray)
	{
		Debug.Log ("Event type is "+sDo);
		int iAreaIndx=PersistAreaLoop;
		int iTarX=PersistTileLoopX;
		int iTarZ=PersistTileLoopZ;
		if(sType=="IND")
		{
			iAreaIndx=InhabitantList[PersistInhabitantLoop].AreaIndex;
			iTarX=InhabitantList[PersistInhabitantLoop].MyJob.xCoordinateTarget;
			iTarZ=InhabitantList[PersistInhabitantLoop].MyJob.zCoordinateTarget;
		}
		if(sDo=="DAMME")
		{
			int iDamMin=ParamArray[1];
			int iDamMax=ParamArray[2]+1;
			if(sType=="IND") InflictDamage (InhabitantList[PersistInhabitantLoop],UnityEngine.Random.Range (iDamMin,iDamMax));
		}
		if(sDo=="FIRECOL")
		{
			FireColonistFromJob(InhabitantList[PersistInhabitantLoop]);
		}
		if(sDo=="DAMHALF")
		{
			int iColLoop=0;
			int iDamMin=ParamArray[1];
			int iDamMax=ParamArray[2]+1;
			while(iColLoop<InhabitantList.Count-1)
			{
				if(InhabitantList[iColLoop].EnemyType==0&UnityEngine.Random.Range(0,1+1)==1&InhabitantList[iColLoop].Dead==false)
				{
					InflictDamage(InhabitantList[iColLoop],UnityEngine.Random.Range(iDamMin,iDamMax));
				}
				iColLoop++;
			}
		}
		if(sDo=="CUT_RES")
		{
			int iResType=ParamArray[1];
			int iPart=ParamArray[2];
			resourceStockpiles[iResType]-=resourceStockpiles[iResType]/iPart;
		}
		if(sDo=="BUILDSTRUCT")
		{
			AddFinishedStructureToArea(AreaList[PersistAreaLoop],iTarX,iTarZ,ParamArray[1],false);
			RefreshTile(GetTileObj(iTarX,iTarZ,PersistAreaLoop));
		}
		if(sDo=="VAR=")
		{
			if(sType=="IND")
			{
				InhabitantList[PersistInhabitantLoop].EventInts[ParamArray[1]]=ParamArray[2];
			}
			else EVENTFLAGS[ParamArray[1]]=ParamArray[2];
			Debug.Log ("Setting event flag "+ParamArray[1]+" to value "+ParamArray[2]);
		}
		if(sDo=="VAR-")
		{
			if(sType=="IND")
			{
				InhabitantList[PersistInhabitantLoop].EventInts[ParamArray[1]]-=ParamArray[2];
			}
			else EVENTFLAGS[ParamArray[1]]-=ParamArray[2];
			Debug.Log ("Subtracting event flag "+ParamArray[1]+" by value "+ParamArray[2]);
		}
		if(sDo=="VAR+")
		{
			if(sType=="IND")
			{
				InhabitantList[PersistInhabitantLoop].EventInts[ParamArray[1]]+=ParamArray[2];
			}
			else EVENTFLAGS[ParamArray[1]]+=ParamArray[2];
			Debug.Log ("Adding event flag "+ParamArray[1]+" by value "+ParamArray[2]);
		}
		if(sDo=="CREATEGEAR")
		{
			GEARSTOCKPILES[ParamArray[1]]++;
		}
		if(sDo=="ADDXP")
		{
			GiveXP(ParamArray[1],InhabitantList[PersistInhabitantLoop]);
		}
		if(sDo=="EVENT")
		{
			RandomEventOverride=ParamArray[1];
			EventMenu(PersistInhabitantLoop,ParamArray[1]);
			bHold=true;//Don't proceed to the next event until this one is resolved.
		}
		if(sDo=="OUTCOME")
		{
			EventMenu(PersistInhabitantLoop,GetRandomEventOutcome(ParamArray[1]));
			bHold=true;//Don't proceed to the next event until this one is resolved.
		}
		if(sDo=="CREATEPERSON")
		{
			CreateHostileAlien(ParamArray[1],iAreaIndx);
			if(ParamArray[2]==1) InhabitantList[InhabitantList.Count-1].EnemyType=0;
		}
		if(sDo=="TILERESOURCE+")
		{
			TILE_TEMPLATE ttTargetTile=AreaList[iAreaIndx].TileList[iTarX,iTarZ];
			ttTargetTile.TileResources[ParamArray[1]]+=ParamArray[2];
		}
		if(sDo=="SET_ENEMY_TYPE")
		{
			InhabitantList[PersistInhabitantLoop].EnemyType=ParamArray[1];
		}
	}

	public int RandomEventOverride=0;
	int GetRandomEventOutcome(int iNode)
	{ 
		int nMax=iniReadint("eventoutcome_"+iNode,"NUMBER_ENTRIES",1);
		int iReturn=iniReadint ("eventoutcome_"+iNode,"INDX",UnityEngine.Random.Range (1,nMax+1));
		RandomEventOverride=iReturn;
		Debug.Log ("Selected event "+iReturn);
		return iReturn;
	}

	bool bHold=false;
	
	void EventOpt(int iOptNum)
	{
		int[] ParamArray=new int[MAX_EVENT_OPTS+1];
		int iUse=PersistEventKindLoop;
		if(RandomEventOverride>0)
		{
			iUse=RandomEventOverride;
			RandomEventOverride=0;
		}
		int iLoop=1;
		int iLoopLoop=1;
		string sDo="";
		while(iLoop<MAX_EVENT_OPTS)
		{
			iLoopLoop=1;
			while(iLoopLoop<MAX_EVENT_OPTS)
			{
				ParamArray[iLoopLoop]=iniReadint("events","OPT_"+iOptNum+"_DO_"+iLoop+"_V"+iLoopLoop,iUse);//Gather vars
				iLoopLoop++;
			}
			sDo=iniRead("events","OPT_"+iOptNum+"_DO"+iLoop,iUse);
			Debug.Log ("Doing outcome of "+"OPT_"+iOptNum+"_DO"+iLoop+" for event kind "+iUse);
			DoAnEventOutcome(sDo,ParamArray);
			iLoop++;
		}
		PersistEventKindLoop++;
		if(bHold==false&bColHold==false) NextEvent(0);
		else if(bHold==false&bColHold==true)
		{
			bColHold=false;
			ProcessInhabitants();//Ok to continue processing
		}
		else bHold=false;
	}

	bool GetNearStruct(int iType,int iDist,int iInhabitant)
	{
		INHABITANT_TEMPLATE itCol=InhabitantList[iInhabitant];
		if(itCol.MyJob.job_type==JOB_TYPE_IDLE) return false;
		int iX=0;
		int iZ=0;
		if(GetIsTargetCombatSkill(itCol.MyJob.job_type))
		{
			iX=itCol.MyJob.xCoordinateStructure;
			iZ=itCol.MyJob.zCoordinateStructure;
		}
		else
		{
			iX=itCol.MyJob.xCoordinateTarget;
			iZ=itCol.MyJob.zCoordinateTarget;
		}
		int iXLoop=iX-iDist;
		int iZLoop=iZ-iDist;
		while(iXLoop<=iX+iDist)
		{
			while(iZLoop<=iZ+iDist)
			{
				if(iZLoop>=0&iZLoop<TILE_Z_SIZE&iXLoop>=0&iXLoop<TILE_X_SIZE)
				{
					if(AreaList[itCol.AreaIndex].TileList[iXLoop,iZLoop].thisTemplate.building_kind==iType) return true;
				}
				iZLoop++;
			}
			iXLoop++;
		}
		return false;
	}

	int CountFriendliesAliveOfSpecies(int iSpecies)
	{
		int iCount=0;
		int iLoop=0;
		while(iLoop<InhabitantList.Count-1)
		{
			if(InhabitantList[iLoop].Dead==false&&InhabitantList[iLoop].EnemyType==0&&InhabitantList[iLoop].Species==iSpecies) iCount++;
			iLoop++;
		}
		return iCount;
	}

	bool GetConditionMet(string sCond,int[] ParamArray)
	{
		bool bReturn=true;
		bool bRev=false;
		if(sCond=="") return bReturn;
		if(sCond.First()=='!')
		{
			bRev=true;
			//Debug.Log("sCond is "+sCond);
			sCond=sCond.Substring(1, sCond.Length-1);
			//Debug.Log("sCond is "+sCond);
		}
		if(sCond=="TECH") bReturn=GetKnowsTech(ParamArray[1]);
		if(sCond=="VAR==")
		{
			if(sType=="IND") bReturn=InhabitantList[PersistInhabitantLoop].EventInts[ParamArray[1]]==ParamArray[2];
			else bReturn=EVENTFLAGS[ParamArray[1]]==ParamArray[2];
		}
		if(sCond=="VAR>")
		{
			if(sType=="IND") bReturn=InhabitantList[PersistInhabitantLoop].EventInts[ParamArray[1]]<ParamArray[2];
			else bReturn=EVENTFLAGS[ParamArray[1]]<ParamArray[2];
		}
		if(sCond=="VAR<")
		{
			if(sType=="IND") bReturn=InhabitantList[PersistInhabitantLoop].EventInts[ParamArray[1]]>ParamArray[2];
			else bReturn=EVENTFLAGS[ParamArray[1]]>ParamArray[2];
		}
		if(sCond=="YEAR>") bReturn=ThisYear>ParamArray[1];
		if(sCond=="YEAR<") bReturn=ThisYear<ParamArray[1];
		if(sCond=="YEAR==") bReturn=ThisYear==ParamArray[1];
		if(sCond=="NEVER") bReturn=false;
		if(sCond=="NEARSTRUCT")
		{
			bReturn=GetNearStruct(ParamArray[1],ParamArray[2],PersistInhabitantLoop);
		}
		if(sCond=="ALIVE_OF_SPECI")
		{
			if(CountFriendliesAliveOfSpecies(ParamArray[1])==ParamArray[2]) bReturn=true;
			else bReturn=false;
		}
		if(bRev) return !bReturn;
		return bReturn;
	}

	bool EventIf(int iInhabitant,int iEvent)
	{
		INHABITANT_TEMPLATE itCheck=InhabitantList[iInhabitant];
		if(itCheck.Dead) return false;
		if(itCheck.EnemyType>0) return false;
		int iLoop=1;
		int[] ParamArray=new int[MAX_EVENT_OPTS+1];
		while(iLoop<MAX_EVENT_OPTS)
		{
			string sCond=iniRead("events","IF_"+iLoop,iEvent);
			int iLoopLoop=1;
			while(iLoopLoop<MAX_EVENT_OPTS)
			{
				ParamArray[iLoopLoop]=iniReadint("events","IF"+iLoop+"_V"+iLoopLoop,iEvent);//Gather vars
				iLoopLoop++;
			}
			iLoop++;
			if(!GetConditionMet(sCond,ParamArray)) return false;
		}
		return true;
	}

	void EventMenu(int iInhabitant,int iEvent)
	{
		ButtonListHeaderText=iniRead ("events","HEADER",iEvent);
		ButtonClickHandler ThisButtonHandler=new ButtonClickHandler(EventOpt);
		string sToolTip="";
		string sText="";
		ButtonList.Clear();
		int iLoop=1;
		while(iLoop<=MAX_EVENT_OPTS)
		{
			sText=iniRead ("events","OPT_"+iLoop,iEvent);
			sToolTip=iniRead ("events","OPTT_"+iLoop,iEvent);
			if(sText!="") ButtonList.Add(new MenuButton(sText,ThisButtonHandler,iLoop,sToolTip));
			iLoop++;
		}
	}

	void ProcessBirths(int Receipt)
	{
		ButtonClickHandler BirthButtonHandler=new ButtonClickHandler(NextEvent);
		int iMTTH=0;
		ButtonListHeaderText="";
		ButtonList.Clear();
		while(PersistAreaLoop<AreaList.Count)
		{
			while(PersistEventKindLoop<AveFileReaders.MAX_2DA_SIZE)
			{
				iMTTH=iniReadint ("species","REP_MTTH",PersistEventKindLoop);
				if(iMTTH>0)
				{
					if(TypesCloned[PersistEventKindLoop]==true) iMTTH=iMTTH/2;//Halves the MTTH if there was a cloning roll success for this species
					if(UnityEngine.Random.Range(1,iMTTH+1)==1)
					{
						if(CountFertileInArea(PersistAreaLoop,PersistEventKindLoop)>=iniReadint("species","NUMBER_SEXES",PersistEventKindLoop))
						{
							BirthButtonHandler=NextEvent;
							ButtonListHeaderText="A new "+iniRead("species","SPECIES_NAME",PersistEventKindLoop)+" has been born in your colony.";
							ButtonList.Add (new MenuButton("OK",BirthButtonHandler,1,"."));
							CreateNewColonists (1,PersistEventKindLoop,0,0,PersistAreaLoop);
							PersistEventKindLoop++;
							return;
						}
					}
				}
				PersistEventKindLoop++;
			}
			PersistEventKindLoop=0;
			PersistAreaLoop++;
		}
		BeginNewTurn();
	}
	
	int PersistAreaLoop=0;
	public int PersistEventKindLoop=0;
	public int PersistInhabitantLoop=0;
	public int PersistTileLoopX=0;
	public int PersistTileLoopZ=0;
	public string sType="IND";
	void ProcessEvents(int Receipt)
	{
		while(PersistAreaLoop<AreaList.Count)
		{
			ButtonClickHandler InhabitantButtonHandler=new ButtonClickHandler(NextEvent);
			ButtonListHeaderText="";
			ButtonList.Clear();
			while(PersistEventKindLoop<AveFileReaders.MAX_2DA_SIZE)
			{
				sType=iniRead ("events","TYPE",PersistEventKindLoop);
				int iMTTH=iniReadint("events","MTTH",PersistEventKindLoop);
				if(iMTTH>0)
				{
					if(UnityEngine.Random.Range(1,iMTTH+1)==1&sType=="AREA")
					{
						if(EventIf(PersistAreaLoop,PersistEventKindLoop))
						{
							EventMenu(PersistAreaLoop,PersistEventKindLoop);
							return;
						}
					}
				}
				PersistEventKindLoop++;
			}
			PersistEventKindLoop=0;
			while(PersistTileLoopX<TILE_X_SIZE)
			{
				while(PersistTileLoopZ<TILE_Z_SIZE)
				{
					while(PersistEventKindLoop<AveFileReaders.MAX_2DA_SIZE)
					{
						sType=iniRead ("events","TYPE",PersistEventKindLoop);
						int iMTTH=iniReadint("events","MTTH",PersistEventKindLoop);
						if(iMTTH>0)
						{
							if(UnityEngine.Random.Range(1,iMTTH+1)==1&sType=="TILE")
							{
								if(EventIf(PersistAreaLoop,PersistEventKindLoop))
								{
									EventMenu(PersistAreaLoop,PersistEventKindLoop);//Tile-related events
									return;
								}
							}
						}
						PersistEventKindLoop++;
					}
					PersistTileLoopZ++;
				}
				PersistTileLoopX++;
			}
			PersistEventKindLoop=0;
			PersistAreaLoop++;	
		}
		while(PersistInhabitantLoop<InhabitantList.Count)
		{
			ButtonClickHandler InhabitantButtonHandler=new ButtonClickHandler(NextEvent);
			ButtonListHeaderText="";
			ButtonList.Clear();
			while(PersistEventKindLoop<AveFileReaders.MAX_2DA_SIZE)
			{
				sType=iniRead ("events","TYPE",PersistEventKindLoop);
				int iMTTH=iniReadint("events","MTTH",PersistEventKindLoop);
				if(iMTTH>0)
				{
					if(UnityEngine.Random.Range(1,iMTTH+1)==1&sType=="IND")
					{
						if(EventIf(PersistInhabitantLoop,PersistEventKindLoop))
						{
							EventMenu(PersistInhabitantLoop,PersistEventKindLoop);
							return;
						}
					}
				}
				PersistEventKindLoop++;
			}
			PersistEventKindLoop=0;
			PersistInhabitantLoop++;
		}
		ButtonListHeaderText="";
		PostEventEnd();
	}

	void ResetClones()
	{
		int iCount=TypesCloned.Count;
		while(iCount>0)
		{
			iCount--;
			TypesCloned[iCount]=false;
		}
	}

	void EndTurn(int Receipt)
	{
		UnloadAreaTemplate();
		ResetClones();
		MainMenuRefresh=false;//blocks normal menu refreshing.
		if(ThisSeason==SEASON_SPRING)ThisSeason=SEASON_SUMMER;else if(ThisSeason==SEASON_SUMMER)ThisSeason=SEASON_FALL;else if(ThisSeason==SEASON_FALL)ThisSeason=SEASON_WINTER;
		else if(ThisSeason==SEASON_WINTER){ThisYear++;ThisSeason=SEASON_SPRING;}
		PersistAreaLoop=0;
		PersistEventKindLoop=0;
		PersistInhabitantLoop=0;
		PersistTileLoopX=0;
		PersistTileLoopZ=0;
		NextEvent(0);//First process
	}
	
	void PostEventEnd()//Call this function after all event processing is concluded.
	{
		CreateHostileAliens();
		if(UnityEngine.Random.Range (1,25)==1) StasisPods++;//Occasionally you get a new stasis pod (MTTH 24)
		iInhabitantEndTurnLoop=0;
		bHostiles=false;
		ProcessInhabitants();//Friendlies go first		
	}
	
	void DoHostileAI()
	{
		AIHandleMoves();//Then AIs plan their moves
		iInhabitantEndTurnLoop=0;
		bHostiles=true;
		ProcessInhabitants();//Then AIs go
	}

	void BeginNewTurn()//After births have been processed, this begins a new turn
	{
		MainMenuRefresh=true;//resumes normal menu refreshing
		OrderSkillsForMenu();
		ColListSort (-1);
		selectedMenuNode=MENU_NODE_VIEW_ALLSTATS;
	}

	void EnterPersonMenu(int Receipt)
	{
		bStructFirst=false;
		selectedMenuNode=MENU_NODE_COLONIST_SELECT;
	}

	void SaveToSlot(int Receipt)
	{
		SaveGame("save"+Receipt);
		selectedMenuNode=MENU_NODE_MAIN;
	}

	void LoadFromSlot(int Receipt)
	{
		Debug.Log ("Loading game "+Receipt);
		LoadGame("save"+Receipt);
		selectedMenuNode=MENU_NODE_MAIN;
	}

	const int PRESKILL_PAGES=3;
	public int NUMPAGES=3;
	const int ENTRIES_PER_PAGE=6;
	public List<int> SkillBar=new List<int>();
	public List<string> SkillBarNames=new List<string>();
	void OrderSkillsForMenu()
	{
		SkillBarNames.Clear();
		SkillBar.Clear();
		int iLoop=1;
		int iCount=0;
		string sJobName=" ";
		while(iLoop<AveFileReaders.MAX_2DA_SIZE&sJobName!="")
		{
			if(GetKnowsTech(iniReadint("jobtypes","TECH_UNLOCK",iLoop))&!GetIsNoSkillJob(iniReadint("jobtypes","SPECIAL_FLAG",iLoop)))
			{
				SkillBar.Add (iLoop);
				sJobName=iniRead("jobtypes","TRIVIAL_NAME",iLoop);
				SkillBarNames.Add (sJobName);
				iCount++;
			}
			iLoop++;
			sJobName=iniRead("jobtypes","TRIVIAL_NAME",iLoop);
		}
		NUMPAGES=Mathf.CeilToInt((float)PRESKILL_PAGES+((float)iCount/(float)ENTRIES_PER_PAGE));
	}

	string GetTableEntry(int iMult,INHABITANT_TEMPLATE toShow,int iInhabitantIndx)
	{
		if(iMult==5) return toShow.Name;
		if(STATSPAGE==1)
		{
			if(iMult==7) return toShow.HP+"/"+toShow.MAXHP;
			if(iMult==8) return toShow.age.ToString();
			if(iMult==9) return toShow.skillpoints.ToString ();
			if(iMult==10) return toShow.level.ToString ();
			if(iMult==11) return toShow.xp+"/"+toShow.GetNextLevelRequiredXP(toShow.level);
			if(iMult==12) return Mathf.Max(toShow.SkillsList.ToArray()).ToString();
		}
		else if(STATSPAGE==2)
		{
			if(iMult==7) return toShow.MyJob.job_name;
			if(iMult==8) return toShow.SkillsList[toShow.MyJob.job_type].ToString();
			if(iMult==9) return toShow.LAST_TURN_JOB.DC.ToString();
			if(iMult==10) return toShow.LAST_TURN_JOB.Roll.ToString();
			if(iMult==11) return Mathf.Clamp(105+5*(toShow.SkillsList[toShow.MyJob.job_type]-toShow.LAST_TURN_JOB.DC),0,100).ToString();
			if(iMult==12) return toShow.LAST_TURN_JOB.InvalidReason;
		}
		else if(STATSPAGE==3)
		{
			if(iMult==7) return toShow.GearNames[1];
			if(iMult==8) return toShow.GearNames[2];
			if(iMult==9) return toShow.GearNames[3];
			if(iMult==10)
			{
				int iGearLoop=1;
				int iBonus=toShow.LAST_TURN_JOB.Skill-toShow.SkillsList[toShow.MyJob.job_type];
				while(iGearLoop<4)
				{
					iBonus+=iniReadint("geartypes","SKILL_BONUS",toShow.GearSlots[iGearLoop]);
					iGearLoop++;
				}
				return iBonus.ToString();
			}
			if(iMult==11) return "";
			if(iMult==12) return "";
		}
		int iStatsIndx=(iMult-7)+((STATSPAGE-(PRESKILL_PAGES+1))*ENTRIES_PER_PAGE);
		return toShow.SkillsList[SkillBar[iStatsIndx]].ToString();
	}

	private void drawsingleline (int pos, INHABITANT_TEMPLATE toShow,int Width,int ColonistINDX)
	{
		int Mult=4;
		if (GUI.Button(new Rect(Width*Mult++,pos*32,Width*2,32),GetTableEntry(Mult,toShow,ColonistINDX)))
		{
			if(InhabitantList[ColonistINDX].age<GetWorkingAge (toShow.Species))
			{
				MessageText ("That person is too young to work.");
			}
			else if(toShow.Dead==false)
			{
				bStructFirst=false;
				LoadAreaTemplate(AreaList[VIEWED_AREA_INDEX]);
				if(STATSPAGE==3) selectedMenuNode=MENU_NODE_COLONIST_VIEWINVENTORY;
				else selectedMenuNode=MENU_NODE_COLONIST_CHOOSESTRUCTURE;
				SELECTED_COLONIST=ColonistINDX;
			}
		}
		Mult++;
		int iStatsIndx;
		int iLoop=0;
		while(iLoop<ENTRIES_PER_PAGE)
		{
			if(STATSPAGE>PRESKILL_PAGES)
			{
				iStatsIndx=iLoop+(STATSPAGE-(PRESKILL_PAGES+1))*ENTRIES_PER_PAGE;
				if(iStatsIndx<SkillBarNames.Count)//.count is zero-based
				GUI.Label(new Rect(Width*Mult++,pos*32,Width,32),GetTableEntry(Mult,toShow,ColonistINDX));
			}
			else GUI.Label(new Rect(Width*Mult++,pos*32,Width,32),GetTableEntry(Mult,toShow,ColonistINDX));
			iLoop++;
		}
	}

	List<int> GetSortIndexes(List<string> input)
	{
		string sDemolished="";
		int nLoop;
		List<int> Indexes=new List<int>();
		List<string> lDemolish=new List<string>(input);
		lDemolish.Sort();
		while(lDemolish.Count>0)
		{
			sDemolished=lDemolish[lDemolish.Count-1];
			while(lDemolish.Count>0&&lDemolish[lDemolish.Count-1]==sDemolished) lDemolish.Remove(sDemolished);
			//Debug.Log ("Checking for string "+sDemolished);
			nLoop=0;
			while(nLoop<input.Count)
			{
				//Debug.Log ("nLoop is "+nLoop+". Comparing string "+input[nLoop]);
				if(input[nLoop]==sDemolished) Indexes.Add (nLoop);
				nLoop++;
			}
		}
		return Indexes;
	}

	void ColListSort(int iSortBy)
	{
		ColSorter.Clear();
		if(iSortBy==-1)
		{
			int iIndx=0;
			while(ColSorter.Count<InhabitantList.Count)
			{
				ColSorter.Add(iIndx);
				iIndx++;
			}
		}
		else
		{
			int iIndx=0;
			List<string> VarsList=new List<string>();
			while(iIndx<InhabitantList.Count)
			{
				VarsList.Add(GetTableEntry(iSortBy,InhabitantList[iIndx],iIndx));
				iIndx++;
			}
			ColSorter=GetSortIndexes(VarsList);
		}
	}
	
	public List<int> ColSorter=new List<int>();

	void ReviewColonyStats()
	{
		int pos=1;
		int Width=64;
		int Mult=4;
		if (GUI.Button(new Rect(Width*Mult++,pos*32,Width,64),"Name")) ColListSort(Mult);
		Mult++;
		if(STATSPAGE==1)
		{
			if (GUI.Button(new Rect(Width*Mult++,pos*32,Width,64),"Health")) ColListSort(Mult);
			if (GUI.Button(new Rect(Width*Mult++,pos*32,Width,64),"Age")) ColListSort(Mult);
			if (GUI.Button(new Rect(Width*Mult++,pos*32,Width,64),"Skill Points")) ColListSort(Mult);
			if (GUI.Button(new Rect(Width*Mult++,pos*32,Width,64),"Level")) ColListSort(Mult);
			if (GUI.Button(new Rect(Width*Mult++,pos*32,Width,64),"XP")) ColListSort(Mult);
			if (GUI.Button(new Rect(Width*Mult++,pos*32,Width,64),"Best Skill")) ColListSort(Mult);
		}
		else if(STATSPAGE==2)
		{
			if (GUI.Button(new Rect(Width*Mult++,pos*32,Width,64),"Job")) ColListSort(Mult);
			if (GUI.Button(new Rect(Width*Mult++,pos*32,Width,64),"Job Skill")) ColListSort(Mult);
			if (GUI.Button(new Rect(Width*Mult++,pos*32,Width,64),"Last DC")) ColListSort(Mult);
			if (GUI.Button(new Rect(Width*Mult++,pos*32,Width,64),"Last Roll")) ColListSort(Mult);
			if (GUI.Button(new Rect(Width*Mult++,pos*32,Width,64),"Last %")) ColListSort(Mult);
			if (GUI.Button(new Rect(Width*Mult++,pos*32,Width,64),"Last Alarms")) ColListSort(Mult);
		}
		else if(STATSPAGE==3)
		{
			if (GUI.Button(new Rect(Width*Mult++,pos*32,Width,64),"Hands")) ColListSort(Mult);
			if (GUI.Button(new Rect(Width*Mult++,pos*32,Width,64),"Armor")) ColListSort(Mult);
			if (GUI.Button(new Rect(Width*Mult++,pos*32,Width,64),"Accessory")) ColListSort(Mult);
			if (GUI.Button(new Rect(Width*Mult++,pos*32,Width,64),"Job Bonus")) ColListSort(Mult);
		}
		else
		{
			int iStatsIndx;
			int iLoop=0;
			while(iLoop<ENTRIES_PER_PAGE)
			{
				iStatsIndx=iLoop+(STATSPAGE-(PRESKILL_PAGES+1))*ENTRIES_PER_PAGE;
				if(iStatsIndx<SkillBarNames.Count)//.count is zero-based
					if (GUI.Button(new Rect(Width*Mult++,pos*32,Width,64),SkillBarNames[iStatsIndx])) ColListSort(Mult);
				iLoop++;
			}
		}
		
		pos++;
		int i=0;
		while(i<InhabitantList.Count)
		{
			if(InhabitantList[ColSorter[i]].EnemyType==0)
			{
				if(InhabitantList[ColSorter[i]].LongDead<2||bHideLongDead==false)
				{
					if(InhabitantList[ColSorter[i]].MyJob.job_type==JOB_TYPE_IDLE||bHideWorkers==false)
					{
						if(InhabitantList[ColSorter[i]].bOldEnoughToWork==true||bHideChildren==false)
						{
							pos++;
							drawsingleline(pos,InhabitantList[ColSorter[i]],Width,ColSorter[i]);
						}
					}
				}
			}
			i++;
		}
	}

	public int STATSPAGE=2;

	void SetStatsPage(int Receipt)
	{
		STATSPAGE=Receipt;
		/*if(Receipt==0)
		{
			if(STATSPAGE==NUMPAGES)	STATSPAGE=1;
			else STATSPAGE++;
		}
		else
		{
			if(STATSPAGE==1) STATSPAGE=NUMPAGES;
			else STATSPAGE--;
		}*/
	}

	bool bHideLongDead=true;
	bool bHideChildren=false;
	bool bHideWorkers=false;
	void StatsHideToggle(int iReceipt)
	{
		if(iReceipt==1) bHideLongDead=!bHideLongDead;
		if(iReceipt==2) bHideChildren=!bHideChildren;
		if(iReceipt==3) bHideWorkers=!bHideWorkers;
	}

	void SeeColonyStats(int Receipt)
	{
		UnloadAreaTemplate();
		selectedMenuNode=MENU_NODE_VIEW_ALLSTATS;
	}
	
	void EnterHelpMenu(int Receipt)
	{
		selectedMenuNode=MENU_NODE_TUTORIAL;
	}

	void EnterSaveGameMenu(int receipt)
	{
		selectedMenuNode=MENU_NODE_SAVE;
	}

	void EnterLoadGameMenu(int receipt)
	{
		selectedMenuNode=MENU_NODE_LOAD;
	}

	void EnterSystemMenu(int receipt)
	{
		selectedMenuNode=MENU_NODE_SYSTEM;
	}

	void RenameSelectedColonist(string sNewName)
	{
		InhabitantList[SELECTED_COLONIST].Name=sNewName;
	}

	void EnterRenameMenu(int iReceipt)
	{
		selectedMenuNode=MENU_NODE_COLONIST_RENAME;
	}

	void QuitGame(int Receipt)
	{
		Application.Quit();
	}

	void BackToMain(int Receipt)
	{
		if(Receipt==1) LoadAreaTemplate(AreaList[VIEWED_AREA_INDEX]);
		selectedMenuNode=MENU_NODE_MAIN;
		RefreshColonistTile(SELECTED_COLONIST);
		int TileToRefreshX=TILE_SELECTED_X;
		int TileToRefreshZ=TILE_SELECTED_Z;
		TILE_SELECTED_X=-1;
		TILE_SELECTED_Z=-1;
		RefreshTile(GetTileObj(TileToRefreshX,TileToRefreshZ,VIEWED_AREA_INDEX));
	}

	void NextTutorialState(int Receipt)
	{
		TUTORIAL_STATE++;
		selectedMenuNode=MENU_NODE_MAIN;
	}

	void RefreshColonistTile(int iColonist)
	{
		if(InhabitantList[iColonist].MyJob.job_type==JOB_TYPE_IDLE) return;
		int xTar=InhabitantList[iColonist].MyJob.xCoordinateTarget;
		int zTar=InhabitantList[iColonist].MyJob.zCoordinateTarget;
		if(GetIsTargetCombatSkill(iniReadint("jobtypes","SPECIAL_FLAG",InhabitantList[iColonist].MyJob.job_type)))//Combat
		{
			xTar=InhabitantList[iColonist].MyJob.xCoordinateStructure;
			zTar=InhabitantList[iColonist].MyJob.zCoordinateStructure;
		}
		RefreshTile(GetTileObj(xTar,zTar,VIEWED_AREA_INDEX));
	}

	void SelectColonist(int Receipt)
	{
		int OldSelected=SELECTED_COLONIST;
		SELECTED_COLONIST=Receipt-1;//To remind myself what the reciept value should represent for this handler. A colonist has just been clicked, this is the index of that colonist.
		//also, InhabitantList is 0-based
		if(bStructFirst&iniReadint ("jobtypes","SPECIAL_FLAG",JOB_SELECTED)==5) selectedMenuNode=MENU_NODE_COLONIST_CHOOSELEARN;
		else if(bStructFirst) selectedMenuNode=MENU_NODE_COLONIST_CHOOSETILE;
		else selectedMenuNode=MENU_NODE_COLONIST_CHOOSESTRUCTURE;
		RefreshColonistTile(OldSelected);
		RefreshColonistTile(SELECTED_COLONIST);
	}

	void SelectJob(int Receipt)
	{
		JOB_SELECTED=Receipt;
		if(Receipt>0&&iniRead ("jobtypes","SPECIAL_FLAG",Receipt)=="1")//construction job flag
		{
			selectedMenuNode=MENU_NODE_COLONIST_CHOOSECONSTRUCTION;
			return;
		}
		if(Receipt>0&&iniRead ("jobtypes","SPECIAL_FLAG",Receipt)=="4")//Tech research job flag
		{
			selectedMenuNode=MENU_NODE_COLONIST_CHOOSETECH;
			return;
		}
		if(Receipt>0&&iniRead ("jobtypes","SPECIAL_FLAG",Receipt)=="5")//Learning job flag
		{
			if(bStructFirst) selectedMenuNode=MENU_NODE_COLONIST_SELECT;
			else selectedMenuNode=MENU_NODE_COLONIST_CHOOSELEARN;
			return;
		}
		if(Receipt>0&&iniRead ("jobtypes","SPECIAL_FLAG",Receipt)=="9")//Craftsman job flag
		{
			selectedMenuNode=MENU_NODE_COLONIST_WORKSHOPPICK;
			return;
		}
		if(bStructFirst) selectedMenuNode=MENU_NODE_COLONIST_SELECT;
		else selectedMenuNode=MENU_NODE_COLONIST_CHOOSETILE;
	}

	void SelectResearchSubJob(int Receipt)
	{
		JOB_SELECTED_PARAM1=Receipt;
		if(bStructFirst) selectedMenuNode=MENU_NODE_COLONIST_SELECT;
		else selectedMenuNode=MENU_NODE_COLONIST_CHOOSETILE;
	}

	void SelectConstructionSubJob(int Receipt)
	{
		JOB_SELECTED_PARAM1=Receipt;
		if(bStructFirst) selectedMenuNode=MENU_NODE_COLONIST_SELECT;
		else selectedMenuNode=MENU_NODE_COLONIST_CHOOSETILE;
	}

	void ViewColonistStats(int Receipt)	{selectedMenuNode=MENU_NODE_COLONIST_VIEWSKILLS;}

	void PickSkill(int Receipt)
	{
		JOB_SELECTED_PARAM1=Receipt;	
		selectedMenuNode=MENU_NODE_COLONIST_CHOOSETILE;	
	}

	void PickGearBuild(int Receipt)
	{
		JOB_SELECTED_PARAM1=Receipt;
		if(bStructFirst) selectedMenuNode=MENU_NODE_COLONIST_SELECT;
		else selectedMenuNode=MENU_NODE_COLONIST_CHOOSETILE;
	}
	
	void SwapGear(int Receipt)
	{
		GEAR_SWAP_TYPE=Receipt;
		selectedMenuNode=MENU_NODE_COLONIST_SWAPGEAR;
	}

	void CombatReady(int Receipt)
	{
		JOB_SELECTED=iniReadint ("geartypes","COMBATSKILL",InhabitantList[Receipt].GearSlots[GEAR_TYPE_HAND_NONE]);
		selectedMenuNode=MENU_NODE_COLONIST_COMBATDEPLOY;
	}
	
	void ConfirmEquip(int Receipt)
	{
		int iOldGear=InhabitantList[SELECTED_COLONIST].GearSlots[GEAR_SWAP_TYPE];
		if(iOldGear>3) GEARSTOCKPILES[iOldGear]++;
		GEARSTOCKPILES[Receipt]--;
		BackToMain (0);
		InhabitantList[SELECTED_COLONIST].GearSlots[GEAR_SWAP_TYPE]=Receipt;
		InhabitantList[SELECTED_COLONIST].GearNames[GEAR_SWAP_TYPE]=iniRead("geartypes","NAME",Receipt);
	}

	void ViewColonistHealth(int Receipt) {selectedMenuNode=MENU_NODE_COLONIST_VIEWHEALTH;}
	void ViewColonistInventory(int Receipt) {selectedMenuNode=MENU_NODE_COLONIST_VIEWINVENTORY;}


	
	void ConfigureStartingSkills(int Inhabitant_index,int iLevel,bool bChild)
	{
		InhabitantList[Inhabitant_index].SkillsList.Add(0);//To make SkillsList be one-based
		int iLoop=1;
		while(iLoop<AveFileReaders.MAX_2DA_SIZE&iniRead("jobtypes","TRIVIAL_NAME",iLoop)!="")
		{
			iLoop++;
			if(iLoop==JOB_TYPE_IDLE|bChild) InhabitantList[Inhabitant_index].SkillsList.Add(0);
			else InhabitantList[Inhabitant_index].SkillsList.Add(GetBaseSkillValue(iLevel));
		}
		if(bChild) InhabitantList[InhabitantList.Count-1].skillpoints=iniReadint ("species","SKILLPOINTSPERLEVEL",InhabitantList[Inhabitant_index].Species);
	}

	void configureStartingGearStockpiles()
	{
		GEARSTOCKPILES.Add (0);
		int iLoop=0;
		while (iLoop<AveFileReaders.MAX_2DA_SIZE)
		{
			iLoop++;
			GEARSTOCKPILES.Add (iniReadint ("geartypes","STARTING_AMOUNT",iLoop));
		}
	}

	void configureStartingEventFlags()
	{
		EVENTFLAGS.Add (0);
		int iLoop=0;
		while (iLoop<AveFileReaders.MAX_2DA_SIZE)
		{
			iLoop++;
			EVENTFLAGS.Add (0);
		}
	}

	void configureStartingColonists()
	{
		int i=3; int Age=0;
		while(i>0)
		{
			Age=UnityEngine.Random.Range(GetAdultAge(SPECIES_HUMAN),GetOldAge(SPECIES_HUMAN));
			CreateNewColonists(1,SPECIES_HUMAN,Age,0,VIEWED_AREA_INDEX);
			i--;
		}
	}

	int GetTileDistance(int xCoord1,int zCoord1,int xCoord2,int zCoord2)
	{
		int xDist=Mathf.Abs (xCoord1-xCoord2);
		int zDist=Mathf.Abs (zCoord1-zCoord2);
		if(xDist>zDist) return xDist;
		return zDist;
	}

	public Vector2 scrollPosition = Vector2.zero;

	//Fires a colonist from his or her current job. Call this when they find a new job, or they die, or they go idle, or sick, etc.
	void FireColonistFromJob(INHABITANT_TEMPLATE ColonistWorker)
	{
		if(ColonistWorker.MyJob.job_type==JOB_TYPE_IDLE) return;
		int xTar=ColonistWorker.MyJob.xCoordinateTarget;
		int zTar=ColonistWorker.MyJob.zCoordinateTarget;
		if(GetIsTargetCombatSkill(iniReadint("jobtypes","SPECIAL_FLAG",ColonistWorker.MyJob.job_type)))//Combat
		{
			xTar=ColonistWorker.MyJob.xCoordinateStructure;
			zTar=ColonistWorker.MyJob.zCoordinateStructure;
		}
		int AreaIndx=ColonistWorker.AreaIndex;
		AreaList[AreaIndx].TileList[xTar,zTar].tileStatusConstant=TILE_STATUS_UNWORKED;
		AreaList[AreaIndx].TileList[xTar,zTar].ColonistHere=-1;
		ColonistWorker.MyJob.xCoordinateStructure=-1;
		ColonistWorker.MyJob.zCoordinateStructure=-1;
		ColonistWorker.MyJob.xCoordinateTarget=-1;
		ColonistWorker.MyJob.zCoordinateTarget=-1;
		SetJob(ColonistWorker.MyJob,JOB_TYPE_IDLE);
		RefreshTile(GetTileObj(xTar,zTar,ColonistWorker.AreaIndex));
	}

	bool CheckTileAvailability(TILE_TEMPLATE MousedTile,int JobSpecialFlag)
	{
		//Check to see if the tile is not already worked, within range, and surveyed
		if(MousedTile.tileStatusConstant==TILE_STATUS_WORKED)
		{
			MessageText("This tile is already worked. Select another tile.");
			BackToMain(0); return false;
		}
		if(MousedTile.tileStatusConstant==TILE_STATUS_COMBATANT)
		{
			MessageText("This tile already contains a combatant. Select another tile.");
			BackToMain(0); return false;
		}
		if(MousedTile.fogOfWar==true&JobSpecialFlag!=2)//You can't work an unsurveyed tile, unless your job is to survey (flag 2).
		{
			MessageText("This tile is unsurveyed. Select a surveyed tile.");
			BackToMain(0); return false;
		}
		if(MousedTile.fogOfWar==false&JobSpecialFlag==2)
		{
			MessageText("You cannot survey a tile that has already been surveyed");
			BackToMain(0); return false;	
		}
		if(AreaList[VIEWED_AREA_INDEX].TileList[MousedTile.xCoord,MousedTile.zCoord].thisTemplate.building_kind!=BUILDING_KIND_NONE&JobSpecialFlag==1)//construction flag
		{
			MessageText ("You cannot build a structure where there is already a structure. Try building on an empty tile instead.");
			return false;
		}
		return true;
	}

	void ColonistClickExecution(int xCoord,int zCoord)//You can right click a colonist on the map to select him/her/it
	{
		TILE_TEMPLATE MousedTile=AreaList[VIEWED_AREA_INDEX].TileList[xCoord,zCoord];
		int nColonist=MousedTile.ColonistHere;
		if(nColonist>-1&&InhabitantList[nColonist].EnemyType==0)
		{
			bStructFirst=false;
			SelectColonist(nColonist+1);
			//SELECTED_COLONIST=nColonist;
			//selectedMenuNode=MENU_NODE_COLONIST_CHOOSESTRUCTURE;
		}
	}

	int GetJobRange(int iJob,int iBuilding){return iniReadint("jobtypes","RANGE",iJob)+iniReadint("buildingtypes","BONUSRANGE",iBuilding);}

	void TileClickExecution(int xCoord,int zCoord)
	{
		TILE_TEMPLATE MousedTile=AreaList[VIEWED_AREA_INDEX].TileList[xCoord,zCoord];
		int BuildingKindSelected=MousedTile.thisTemplate.building_kind;
		if(selectedMenuNode==MENU_NODE_MAIN|selectedMenuNode==MENU_NODE_COLONIST_SELECT)
		{
			if(BuildingKindSelected!=BUILDING_KIND_NONE&MousedTile.thisTemplate.bHostile==false)
			{
				TILE_SELECTED_X=xCoord;//Preserve the coordinates, which are a pointer to the structure itself.
				TILE_SELECTED_Z=zCoord;//Preserve the coordinates, which are a pointer to the structure itself.
				RefreshTile(GetTileObj(TILE_SELECTED_X,TILE_SELECTED_Z,VIEWED_AREA_INDEX));
				selectedMenuNode=MENU_NODE_COLONIST_CHOOSEJOB;
				bStructFirst=true;
			}
		}
		int JobSpecialFlag=iniReadint("jobtypes","SPECIAL_FLAG",JOB_SELECTED);
		if((selectedMenuNode==MENU_NODE_COLONIST_CHOOSESTRUCTURE&BuildingKindSelected!=BUILDING_KIND_NONE)|selectedMenuNode==MENU_NODE_COLONIST_COMBATDEPLOY)
		{
			if(GetIsTargetCombatSkill(JobSpecialFlag))
			{
				if(!CheckTileAvailability(MousedTile,JobSpecialFlag)) return;					
			}//Check to see if the tile is not already worked, within range, and surveyed
			
			TILE_SELECTED_X=xCoord;//Preserve the coordinates, which are a pointer to the structure itself.
			TILE_SELECTED_Z=zCoord;//Preserve the coordinates, which are a pointer to the structure itself.			
			RefreshTile(GetTileObj(TILE_SELECTED_X,TILE_SELECTED_Z,VIEWED_AREA_INDEX));	
			if(selectedMenuNode!=MENU_NODE_COLONIST_COMBATDEPLOY) selectedMenuNode=MENU_NODE_COLONIST_CHOOSEJOB;
			else selectedMenuNode=MENU_NODE_COLONIST_ATTACKTARGET;
			return;
		}
		if(selectedMenuNode==MENU_NODE_COLONIST_CHOOSETILE|selectedMenuNode==MENU_NODE_COLONIST_ATTACKTARGET)
		{
			if(selectedMenuNode==MENU_NODE_COLONIST_ATTACKTARGET|GetIsTargetCombatSkill(JobSpecialFlag))
			{
				TILE_TEMPLATE OriginTile=AreaList[VIEWED_AREA_INDEX].TileList[TILE_SELECTED_X,TILE_SELECTED_Z];
				if(!CheckTileAvailability (OriginTile,JobSpecialFlag)) return; //Check to see if the tile is not already worked, within range, and surveyed
			}
			else if(!CheckTileAvailability (MousedTile,JobSpecialFlag)) return; //Check to see if the tile is not already worked, within range, and surveyed
			int iDist=GetTileDistance(TILE_SELECTED_X,TILE_SELECTED_Z,xCoord,zCoord);
			int iTotRange=GetJobRange(JOB_SELECTED,BuildingKindSelected);
			if(iDist>iTotRange&selectedMenuNode==MENU_NODE_COLONIST_COMBATDEPLOY==false)
			{
				MessageText("This tile is out of range. Select a closer tile.");
				BackToMain(0); return;
			}
			//It checks out! Put the colonist to work!
			INHABITANT_TEMPLATE ColonistWorker=InhabitantList[SELECTED_COLONIST] as INHABITANT_TEMPLATE;
			FireColonistFromJob(ColonistWorker);
			ColonistWorker.MyJob.xCoordinateStructure=TILE_SELECTED_X;
			ColonistWorker.MyJob.zCoordinateStructure=TILE_SELECTED_Z;
			ColonistWorker.MyJob.xCoordinateTarget=xCoord;
			ColonistWorker.MyJob.zCoordinateTarget=zCoord;
			SetJob(ColonistWorker.MyJob,JOB_SELECTED);
			ColonistWorker.MyJob.job_param1=JOB_SELECTED_PARAM1;
			ColonistWorker.AreaIndex=VIEWED_AREA_INDEX;
			if(selectedMenuNode==MENU_NODE_COLONIST_ATTACKTARGET|GetIsTargetCombatSkill (JobSpecialFlag))//Fighting, propoganda, and healing target other units, therefore they must occupy the origin tile
			{
				//Debug.Log("Performing combat-related action. Menu node is "+selectedMenuNode+" and special job flag is "+JobSpecialFlag);
				AreaList[VIEWED_AREA_INDEX].TileList[TILE_SELECTED_X,TILE_SELECTED_Z].tileStatusConstant=TILE_STATUS_COMBATANT;
				AreaList[VIEWED_AREA_INDEX].TileList[TILE_SELECTED_X,TILE_SELECTED_Z].ColonistHere=SELECTED_COLONIST;
				RefreshTile (GetTileObj (TILE_SELECTED_X,TILE_SELECTED_Z,VIEWED_AREA_INDEX));
			}
			else
			{
				//Debug.Log("Performing non-combat action. Menu node is "+selectedMenuNode+" and special job flag is "+JobSpecialFlag);
				AreaList[VIEWED_AREA_INDEX].TileList[xCoord,zCoord].tileStatusConstant=TILE_STATUS_WORKED;
				AreaList[VIEWED_AREA_INDEX].TileList[xCoord,zCoord].ColonistHere=SELECTED_COLONIST;
				RefreshTile (GetTileObj (xCoord,zCoord,VIEWED_AREA_INDEX));
			}
			BackToMain(0);
			MessageText("You have successfully put the colonist to work.");
		}
	}
	
	bool bStructFirst=false;
	int KillDuplicateRun=0;//Creates a brief timer to prevent double-runs when the user only single mouse clicks
	void DoMouseUpChecks(int nButton)
	{
		if(KillDuplicateRun>0) return;
		KillDuplicateRun=30;
		RaycastHit hit=new RaycastHit();
		Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
		if (Physics.Raycast(ray,out hit))
		{
			int xCoord=hit.collider.GetComponent<TileLocation>().xCoord;
			int zCoord=hit.collider.GetComponent<TileLocation>().zCoord;
			if(nButton==0) TileClickExecution(xCoord,zCoord);
			else if(nButton==1) ColonistClickExecution(xCoord,zCoord);
		}
	}

	string GetColonistSummary(int iInhabitantIndex)
	{
		INHABITANT_TEMPLATE itCheck=InhabitantList[iInhabitantIndex];
		string sReturn=itCheck.Name;
		sReturn+=". Job: "+itCheck.MyJob.job_name+". Level: "+itCheck.level+". HP: "+itCheck.HP+"/"+itCheck.MAXHP;
		return sReturn;
	}

	public string MessageDisplay;
	public const float DisplayTime=10f;
	public float MessageTextTimer=0f;

	void MessageTextDisplayer()
	{
		if(MessageTextTimer<DisplayTime)
		{
			GUI.Label(new Rect(Mathf.RoundToInt (Screen.width*0.82f),Mathf.RoundToInt (Screen.height*0.05f),150,200),MessageDisplay);
			MessageTextTimer+=Time.deltaTime;
		}
	}

	//MessageText displays in the upper right and lingers for several seconds after being called
	void MessageText(string sMessage)
	{
		MessageDisplay=sMessage;
		MessageTextTimer=0f;
	}
	
	//InfoText displays in the lower right and lingers only as long as it keeps being called (eg onmouseover)
	void InfoText(string sInfo){GUI.Label(new Rect(Mathf.RoundToInt (Screen.width*0.82f),Mathf.RoundToInt (Screen.height*0.7f),150,200),sInfo);}
	
	int iPersMouseOverX=0;
	int iPersMouseOverZ=0;
	string sMouseOverTextCache="";

	void DoMouseOverCheck()
	{
		RaycastHit hit=new RaycastHit();
		Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
		if (Physics.Raycast(ray,out hit))
		{
			int xCoord=hit.collider.GetComponent<TileLocation>().xCoord;
			int zCoord=hit.collider.GetComponent<TileLocation>().zCoord;
			if(iPersMouseOverX!=xCoord|iPersMouseOverZ!=zCoord)
			{
				int iXHolder=iPersMouseOverX;
				int iZHolder=iPersMouseOverZ;
				iPersMouseOverX=xCoord;
				iPersMouseOverZ=zCoord;
				RefreshTile(GetTileObj(iXHolder,iZHolder,VIEWED_AREA_INDEX));//Update the old moused over tile object
				RefreshTile(GetTileObj(xCoord,zCoord,VIEWED_AREA_INDEX));//Update the new moused over tile object

				TILE_TEMPLATE MousedTile=AreaList[VIEWED_AREA_INDEX].TileList[xCoord,zCoord];
				string sBuildingName=iniRead("buildingtypes","BUILDINGNAME",MousedTile.thisTemplate.building_kind);
				INHABITANT_TEMPLATE workerHere=new INHABITANT_TEMPLATE();
				if(MousedTile.ColonistHere>-1) workerHere=InhabitantList[MousedTile.ColonistHere];
				string sMouseMessage="";
				if(MousedTile.fogOfWar==true) sMouseMessage+="You have moused over a tile. It is unexplored. ";
				else
				{
					sMouseMessage+="You have moused over a tile. It has ";
					int iLoop=1;
					while(iLoop<resourceNames.Count-1)
					{
						if(GetKnowsTech (iniReadint ("tileresources","TECH_TO_SEE",iLoop))) sMouseMessage+=MousedTile.TileResources[iLoop]+" "+resourceNames[iLoop]+", ";
						iLoop++;
					}
					sMouseMessage+="and contains "+sBuildingName+". ";

					string sHostile="";
					if(workerHere.EnemyType>0) sHostile=" (enemy)";
					if(MousedTile.tileStatusConstant==TILE_STATUS_WORKED) sMouseMessage+=workerHere.Name+" is working here on "+workerHere.MyJob.job_name+".";
					if(MousedTile.tileStatusConstant==TILE_STATUS_COMBATANT) sMouseMessage+=workerHere.Name+sHostile+" is based here, fighting.";
					if(MousedTile.tileStatusConstant==TILE_STATUS_COMBATANT|MousedTile.tileStatusConstant==TILE_STATUS_WORKED)
					{
						float fHP=workerHere.HP; float fMax=workerHere.MAXHP; float fPercent=(fHP/fMax)*100f; int iPercent=Mathf.RoundToInt(fPercent); sMouseMessage+=" Health: "+iPercent+"%.";
					}
				}
				sMouseOverTextCache=sMouseMessage;
			}
			InfoText(sMouseOverTextCache);
		}
	}

	void DisplayResourceCounts()
	{
		string sDisplay="";
		sDisplay+=ThisSeason+", year "+ThisYear;
		sDisplay+=System.Environment.NewLine;
		int iLoop=1;
		while(iLoop<resourceNames.Count-1)
		{
			if(GetKnowsTech(iniReadint("tileresources","TECH_TO_SEE",iLoop)))
			{
				sDisplay+=resourceNames[iLoop];
				sDisplay+=": ";
				sDisplay+=resourceStockpiles[iLoop];
				sDisplay+=System.Environment.NewLine;
			}
			iLoop++;
		}
		sDisplay=sDisplay+"Accessible stasis pods: "+StasisPods;
		GUI.Label(new Rect(Mathf.RoundToInt (Screen.width*0.82f),Mathf.RoundToInt (Screen.height*0.3f),150,200),sDisplay);
	}

	string GetResourceName(int iResource) {return iniRead ("tileresources","RESOURCENAME",iResource);}
	bool GetCanResearchTech(int iTechNum, int iLabType)
	{
		if(GetKnowsTech (iTechNum)) return false;
		int iTechReq1=iniReadint ("techtree","Prereq_tech1",iTechNum);
		int iTechReq2=iniReadint ("techtree","Prereq_tech2",iTechNum);
		int iTechReqItem=iniReadint ("techtree","Prereq_item",iTechNum);
		if(iTechReqItem>0&&GEARSTOCKPILES[iTechReqItem]==0) return false;
		int iTechType=iniReadint ("techtree","CATEGORY",iTechNum);
		return (GetKnowsTech (iTechReq1)&GetKnowsTech (iTechReq2)&iTechType==iLabType);
	}

	bool GetCanLearnSkill(int iSkill,int iLearnType,INHABITANT_TEMPLATE inhabitant)
	{
		if(inhabitant.skillpoints<1) return false;
		if(iniReadint ("jobtypes","LEARN_JOB",iSkill)!=iLearnType) return false;
		if(!GetKnowsTech(iniReadint ("jobtypes","TECH_UNLOCK",iSkill))) return false;
		int iCurrentSkill=inhabitant.SkillsList[iSkill];
		int iMax=inhabitant.level;
		if(iCurrentSkill>=iMax) return false;
		return true;
	}

	int CountColonistsWithJob(int iJob)
	{
		int iCounter=0;
		int iLoop=0;
		while(iLoop<InhabitantList.Count)
		{
			if(InhabitantList[iLoop].MyJob.job_type==iJob) iCounter++;
			iLoop++;
		}
		return iCounter;
	}

	int CountBuildingsOfType(int iType)
	{
		int iCount=0;
		int iX=0;
		int iZ=0;
		while(iX<TILE_X_SIZE)
		{
			iZ=0;
			while(iZ<TILE_Z_SIZE)
			{
				if(AreaList[VIEWED_AREA_INDEX].TileList[iX,iZ].thisTemplate.building_kind==iType) iCount++;
				iZ++;
			}
			iX++;
		}
		return iCount;
	}

	string GetTutorialHint()
	{
		if(TUTORIAL_STATE==0)
		return "Welcome to the world map! You can scroll the map by moving the mouse to the edge of the screen. Give it a try!";
		if(TUTORIAL_STATE==1)
		return "You can also rotate the camera by holding down the right mouse button. This lets you view your colony from different angles.";
		if(TUTORIAL_STATE==2)
		return "Finally, you can zoom in and out using the 'z' and 'a' keys on your keyboard.";
		if(TUTORIAL_STATE==3)
			return "You will need food. Left-click your starting command center and then select 'farming'. Then pick a colonist and click a tile in the game world to begin farming there. More farmers means more food.";
		if(TUTORIAL_STATE>3)
		{
			if(CountColonistsWithJob(2)<1)
			{
				return "You will need food. Left-click your starting command center and then select 'farming'. Then pick a colonist and click a tile in the game world to begin farming there. More farmers means more food.";				
			}
			if(CountBuildingsOfType(10)==0)
			{
				if(CountColonistsWithJob(3)<1) return "Now assign a colonist to constructing a lab. This works like farming, except now you click 'construction' and 'laboratory'. More construction skill is better.";								
				return "Now click 'end turn' to advance time and continue to do so until the lab is built. This may take a few turns.";	
			}
			if(CountBuildingsOfType(10)>0)
			{
				if(CountColonistsWithJob(6)==0&&GetKnowsTech(9)==false)
				{
					return "Now click on your lab to begin researching something there. Let's start with foraging, which will help us expand our food and wood production. More tech skill is better.";													
				}
				if(GetKnowsTech(9)==true)
				{
					if(CountBuildingsOfType(3)==0&CountBuildingsOfType(4)==0)
					{
						if(CountColonistsWithJob(10)==0)
						{
							return "Now that we have researched a new tech, it unlocks new build options. In this case, we can build a new farm or woodmill. Do so!";																			
						}
						else
						{
							return "Now click 'end turn' to advance time and continue to do so until the lab is built. This may take a few turns.";							
						}
					}
					else return "You have completed the tutorial. You will now have to decide what your next priority is. Good luck - the survival of your colonists is in your hands!";
				}
				else
				{
					return "Now click 'end turn' to advance time and continue to do so until the research is finished. This may take a few turns.";						
				}
			}
													
		}
		return "You have completed the tutorial";
	}

	public bool MainMenuRefresh=true;
	public int OldSelectedMenuNode=-1;
	void OnGUI()
	{
		GUI.skin.button.wordWrap = true;
		if(KillDuplicateRun>0) KillDuplicateRun-=1;
		MessageTextDisplayer();
		DoMouseOverCheck();
		if (Input.GetMouseButtonUp(0)) DoMouseUpChecks(0);
		if(Input.GetMouseButtonUp (1)) DoMouseUpChecks(1);
		DisplayResourceCounts();
		ButtonClickHandler ThisButtonHandler=new ButtonClickHandler(EndTurn);
		TextEntryHandler ThisTextHandler=new TextEntryHandler(RenameSelectedColonist);
		if(MainMenuRefresh&selectedMenuNode!=OldSelectedMenuNode)
		{
			ButtonListHeaderText="";
			ButtonList.Clear();
			OldSelectedMenuNode=selectedMenuNode;
			if(selectedMenuNode==MENU_NODE_TUTORIAL)
			{
				ButtonListHeaderText=GetTutorialHint();
				ThisButtonHandler=NextTutorialState;
				ButtonList.Add(new MenuButton("OK.",ThisButtonHandler,0,""));
			}
			else if(selectedMenuNode==MENU_NODE_MAIN)
			{
				ButtonList.Add(new MenuButton("End turn",ThisButtonHandler,0,"Begins a new turn. Click this after you have assigned jobs to all your colonists."));
				ThisButtonHandler=EnterPersonMenu;
				ButtonList.Add (new MenuButton("Select a person",ThisButtonHandler,0,"Select a colonist. You will need to enter this menu to give work to colonists!"));
				ThisButtonHandler=SeeColonyStats;
				ButtonList.Add (new MenuButton("View colony statistics",ThisButtonHandler,0,"View general information about your colony"));
				ThisButtonHandler=EnterHelpMenu;
				ButtonList.Add (new MenuButton("View a tutorial hint",ThisButtonHandler,0,"Get tutorial help for starting the game"));
				ThisButtonHandler=EnterSystemMenu;
				ButtonList.Add (new MenuButton("System Menu",ThisButtonHandler,0,"Save, load, or quit game"));
			}
			else if(selectedMenuNode==MENU_NODE_SYSTEM)
			{
				ThisButtonHandler=BackToMain;
				ButtonList.Add(new MenuButton("Back to main menu",ThisButtonHandler,0,""));
				ThisButtonHandler=EnterSaveGameMenu;
				ButtonList.Add (new MenuButton("Save Game",ThisButtonHandler,0,"Save the current game state"));
				ThisButtonHandler=EnterLoadGameMenu;
				ButtonList.Add (new MenuButton("Load Game",ThisButtonHandler,0,"Load a previously saved game"));
				ThisButtonHandler=QuitGame;
				ButtonList.Add (new MenuButton("Quit game",ThisButtonHandler,0,"Quit to desktop"));
			}
			else if(selectedMenuNode==MENU_NODE_SAVE)
			{
				int iSlots=10;
				int iLoop=1;
				while(iLoop<=iSlots)
				{
					ThisButtonHandler=SaveToSlot;
					ButtonList.Add(new MenuButton("Slot "+iLoop,ThisButtonHandler,iLoop,"Save to slot "+iLoop));
					iLoop++;
				}
				ThisButtonHandler=BackToMain;
				ButtonList.Add(new MenuButton("Back to main menu",ThisButtonHandler,0,""));
			}
			else if(selectedMenuNode==MENU_NODE_LOAD)
			{
				int iSlots=10;
				int iLoop=1;
				while(iLoop<=iSlots)
				{
					ThisButtonHandler=LoadFromSlot;
					ButtonList.Add(new MenuButton("Slot "+iLoop,ThisButtonHandler,iLoop,"Load from slot "+iLoop));
					iLoop++;
				}
				ThisButtonHandler=BackToMain;
				ButtonList.Add(new MenuButton("Back to main menu",ThisButtonHandler,0,""));
			}
			else if(selectedMenuNode==MENU_NODE_COLONIST_SELECT)
			{
				ThisButtonHandler=BackToMain;
				ButtonList.Add(new MenuButton("Back to main menu",ThisButtonHandler,0,""));
				int ColonistCounter=0;
				string sHelperText;
				foreach(INHABITANT_TEMPLATE SelectedColonist in InhabitantList)
				{
					ColonistCounter++;
					if(SelectedColonist.Dead==false&&SelectedColonist.EnemyType==0&&SelectedColonist.age>=GetWorkingAge(SelectedColonist.Species))//A colonist must still be alive in order to receive orders
					{
						sHelperText="";
						if(bStructFirst&iniReadint ("jobtypes","SPECIAL_FLAG",JOB_SELECTED)!=5) sHelperText=", skill "+SelectedColonist.SkillsList[JOB_SELECTED];
						if(SelectedColonist.MyJob.job_type==JOB_TYPE_IDLE) sHelperText+=" (idle)";
						ThisButtonHandler=SelectColonist;
						ButtonList.Add (new MenuButton(SelectedColonist.Name+sHelperText,ThisButtonHandler,ColonistCounter,GetColonistSummary(ColonistCounter-1)));
					}
				}
			}
			else if(selectedMenuNode==MENU_NODE_COLONIST_CHOOSESTRUCTURE)
			{
				INHABITANT_TEMPLATE UnderExamination=InhabitantList[SELECTED_COLONIST];
				MessageText("Click a building in the game world to assign the colonist to work there!");
				ThisButtonHandler=BackToMain;
				ButtonList.Add (new MenuButton("Back to main",ThisButtonHandler,0,""));
				ThisButtonHandler=ViewColonistStats;
				ButtonList.Add (new MenuButton("View Colonist skills",ThisButtonHandler,SELECTED_COLONIST,"View the skills of "+UnderExamination.Name));
				ThisButtonHandler=ViewColonistHealth;
				ButtonList.Add (new MenuButton("View Colonist health",ThisButtonHandler,SELECTED_COLONIST,"View the hit points and age of "+UnderExamination.Name));
				ThisButtonHandler=ViewColonistInventory;
				ButtonList.Add (new MenuButton("View Colonist inventory",ThisButtonHandler,SELECTED_COLONIST,"View the inventory of "+UnderExamination.Name));
				ThisButtonHandler=CombatReady;
				ButtonList.Add (new MenuButton("Combat Deploy",ThisButtonHandler,SELECTED_COLONIST,"Deploy "+UnderExamination.Name+" as combatant"));	
				ThisButtonHandler=EnterRenameMenu;
				ButtonList.Add (new MenuButton("Rename Colonist",ThisButtonHandler,SELECTED_COLONIST,"Select a new name for "+UnderExamination.Name));
			}
			else if(selectedMenuNode==MENU_NODE_COLONIST_CHOOSEJOB)
			{
				int buildingselected=AreaList[VIEWED_AREA_INDEX].TileList[TILE_SELECTED_X,TILE_SELECTED_Z].thisTemplate.building_kind;				
				int job=0;
				int iCount=0;//Only used for single-option streamlining.
				int iJobIndx=0;//Only used for single-option streamlining.
				int iLoop=1;
				while(iLoop<5)
				{
					job=iniReadint("buildingtypes","JOB"+iLoop,buildingselected);
					if(job>0&GetKnowsTech(iniReadint("jobtypes","TECH_UNLOCK",job)))
					{
						ThisButtonHandler=SelectJob;
						ButtonList.Add(new MenuButton(iniRead("jobtypes","TRIVIAL_NAME",job),ThisButtonHandler,job,iniRead ("jobtypes","TOOLTIP",job)));
						iCount++;
						iJobIndx=job;
					}
					iLoop++;
				}
				if(iCount==1) {SelectJob(iJobIndx); return;}//Automatically pick the job if there's only one option in the list.
				ThisButtonHandler=BackToMain;
				ButtonList.Add (new MenuButton("Back to main menu",ThisButtonHandler,0,""));
			}
			else if(selectedMenuNode==MENU_NODE_COLONIST_CHOOSECONSTRUCTION)
			{
				int iLoop=1;
				int iTechReq=0;
				while(iLoop<AveFileReaders.MAX_2DA_SIZE)
				{
					iTechReq=iniReadint("buildingtypes","TECH_PREREQ",iLoop);
					if(GetKnowsTech (iTechReq))//if there is no prerequisite
					{
						ThisButtonHandler=SelectConstructionSubJob;
						ButtonList.Add(new MenuButton(iniRead ("buildingtypes","BUILDINGNAME",iLoop)+" "+iniRead("buildingtypes","COST_AMOUNT",iLoop)+" "+GetResourceName(iniReadint("buildingtypes","COST_TYPE",iLoop)),ThisButtonHandler,iLoop,iniRead ("buildingtypes","TOOLTIP",iLoop)));	
					}
					iLoop++;
				}
				ThisButtonHandler=BackToMain;
				ButtonList.Add (new MenuButton("Back to main menu",ThisButtonHandler,0,""));
			}
			else if(selectedMenuNode==MENU_NODE_COLONIST_CHOOSETILE)
			{
				if(GetJobRange(JOB_SELECTED,AreaList[VIEWED_AREA_INDEX].TileList[TILE_SELECTED_X,TILE_SELECTED_Z].thisTemplate.building_kind)==0) TileClickExecution(TILE_SELECTED_X,TILE_SELECTED_Z);//If job has range 0, just auto-work on building tile
				ThisButtonHandler=BackToMain;
				ButtonList.Add(new MenuButton("Pick a tile",ThisButtonHandler,0,"Click a tile in the main game to assign the colonist, or click this button to cancel the assignment."));
			}
			else if(selectedMenuNode==MENU_NODE_COLONIST_CHOOSETECH)
			{
				ThisButtonHandler=SelectResearchSubJob;
				int iLoop=1;
				string sName=" ";
				while(iLoop<AveFileReaders.MAX_2DA_SIZE&sName!="")
				{
					if(GetCanResearchTech(iLoop,JOB_SELECTED)) ButtonList.Add(new MenuButton(sName,ThisButtonHandler,iLoop,iniRead ("techtree","TOOLTIP",iLoop)));
					iLoop++;
					sName=iniRead ("techtree","TECHNAME",iLoop);
				}
				ThisButtonHandler=BackToMain;
				ButtonList.Add (new MenuButton("Back to main menu",ThisButtonHandler,0,""));
			}
			else if(selectedMenuNode==MENU_NODE_COLONIST_CHOOSELEARN)
			{
				ThisButtonHandler=PickSkill;
				int iLoop=1;
				string sName=" ";
				while(iLoop<AveFileReaders.MAX_2DA_SIZE&sName!="")
				{
					sName=iniRead ("jobtypes","TRIVIAL_NAME",iLoop);
					if(GetCanLearnSkill(iLoop,JOB_SELECTED,InhabitantList[SELECTED_COLONIST])) ButtonList.Add (new MenuButton(sName,ThisButtonHandler,iLoop,iniRead ("jobtypes","TOOLTIP",iLoop)));
					iLoop++;
				}
				ThisButtonHandler=BackToMain;
				ButtonList.Add (new MenuButton("Back to main menu",ThisButtonHandler,0,""));
			}
			else if(selectedMenuNode==MENU_NODE_COLONIST_VIEWSKILLS)
			{
				INHABITANT_TEMPLATE UnderExamination=InhabitantList[SELECTED_COLONIST];
				ThisButtonHandler=BackToMain;
				ButtonList.Add(new MenuButton("Level is "+UnderExamination.level,ThisButtonHandler,0,"Levels are gained with more experience. Gaining levels gives you extra skill points, and makes you harder to hit in combat."));
				ButtonList.Add(new MenuButton("XP is "+UnderExamination.xp+"/"+UnderExamination.GetNextLevelRequiredXP(UnderExamination.level),ThisButtonHandler,0,"This is how much experience you have, and how much experience you need to gain the next level."));
				ButtonList.Add(new MenuButton("Free skill points: "+UnderExamination.skillpoints,ThisButtonHandler,0,"Unspent skill points. You get these by gaining levels. If you have extra, you should spend them at a library to improve your colonist!"));
				int iLoop=1;
				string TrivialName=" ";
				while(iLoop<AveFileReaders.MAX_2DA_SIZE)
				{
					if(iniReadint("jobtypes","SPECIAL_FLAG",iLoop)!=3&iniReadint("jobtypes","SPECIAL_FLAG",iLoop)!=5&iniReadint("jobtypes","SPECIAL_FLAG",iLoop)!=6)//Being imprisoned, learning and idleness are not skills
					{
						if(GetKnowsTech(iniReadint("jobtypes","TECH_UNLOCK",iLoop)))//Some jobs require a specific tech to unlock
						{
							TrivialName=iniRead("jobtypes","TRIVIAL_NAME",iLoop);
							ThisButtonHandler=BackToMain;
							ButtonList.Add (new MenuButton(TrivialName+": "+UnderExamination.SkillsList[iLoop],ThisButtonHandler,iLoop,iniRead ("jobtypes","TOOLTIP",iLoop)));
						}
					}
					iLoop++;
				}
				ThisButtonHandler=BackToMain;
				ButtonList.Add(new MenuButton("Back to main menu",ThisButtonHandler,0,""));
			}
			else if(selectedMenuNode==MENU_NODE_COLONIST_WORKSHOPPICK)
			{
				int iLoop=1;
				string sTrivialName=" ";
				while(iLoop<AveFileReaders.MAX_2DA_SIZE&sTrivialName!="")
				{
					if(GetKnowsTech(iniReadint("geartypes","TECH_BUILD",iLoop))&JOB_SELECTED==iniReadint("geartypes","WORKSHOP_TYPE",iLoop))//Some jobs require a specific tech to unlock
					{
						sTrivialName=iniRead("geartypes","NAME",iLoop);
						ThisButtonHandler=PickGearBuild;
						string sCost=" ("+iniRead ("geartypes","COST_AMOUNT",iLoop)+" "+iniRead ("tileresources","RESOURCENAME",iniReadint ("geartypes","COST_TYPE",iLoop))+")";
						ButtonList.Add (new MenuButton(sTrivialName,ThisButtonHandler,iLoop,iniRead ("geartypes","TOOLTIP",iLoop)+sCost));						
					}
					iLoop++;
					sTrivialName=iniRead("geartypes","NAME",iLoop);
				}
				ThisButtonHandler=BackToMain;
				ButtonList.Add(new MenuButton("Back to main menu",ThisButtonHandler,0,""));
			}
			else if(selectedMenuNode==MENU_NODE_COLONIST_VIEWHEALTH)
			{
				INHABITANT_TEMPLATE UnderExamination=InhabitantList[SELECTED_COLONIST];
				ThisButtonHandler=BackToMain;
				ButtonList.Add(new MenuButton("Health is "+UnderExamination.HP+" of "+UnderExamination.MAXHP,ThisButtonHandler,0,"This is your current and maximum health. If your current health falls below zero, you die."));
				ButtonList.Add(new MenuButton("Age is "+UnderExamination.age,ThisButtonHandler,0,"Different colonists have different ages. People in their prime tend to have higher maximum health than children or the elderly."));
				ButtonList.Add(new MenuButton("Species is "+iniRead("species","SPECIES_NAME",UnderExamination.Species),ThisButtonHandler,0,iniRead ("species","TOOLTIP",UnderExamination.Species)));
				ButtonList.Add(new MenuButton("Back to main menu",ThisButtonHandler,0,""));
			}
			else if(selectedMenuNode==MENU_NODE_COLONIST_VIEWINVENTORY)
			{
				ThisButtonHandler=SwapGear;
				ButtonList.Add(new MenuButton("Change hands gear slot",ThisButtonHandler,1,""));
				ButtonList.Add(new MenuButton("Change armor gear slot",ThisButtonHandler,2,""));
				ButtonList.Add(new MenuButton("Change accessory gear slot",ThisButtonHandler,3,""));
				ThisButtonHandler=BackToMain;
				ButtonList.Add(new MenuButton("Back to main menu",ThisButtonHandler,0,""));
			}
			else if(selectedMenuNode==MENU_NODE_COLONIST_SWAPGEAR)
			{
				int iLoop=1;
				int iAmount=0;
				string sAmount="";
				string TrivialName=" ";
				while(iLoop<AveFileReaders.MAX_2DA_SIZE&TrivialName!="")
				{
					sAmount="";
					if(iniReadint("geartypes","INVENTORY_SLOT",iLoop)==GEAR_SWAP_TYPE)
					{
						iAmount=GEARSTOCKPILES[iLoop];
						if(InhabitantList[SELECTED_COLONIST].GearSlots[GEAR_SWAP_TYPE]==iLoop)
						{
							sAmount="used, ";
							iAmount++;
						}
						if(iAmount>0|iLoop<4)
						{
							ThisButtonHandler=ConfirmEquip;
							if(iLoop<4) sAmount+="inf";
							else sAmount+=iAmount.ToString();
							ButtonList.Add(new MenuButton(iniRead("geartypes","NAME",iLoop)+" ("+sAmount+")",ThisButtonHandler,iLoop,iniRead ("geartypes","TOOLTIP",iLoop)));
						}
					}
					iLoop++;
					TrivialName=iniRead("geartypes","NAME",iLoop);
				}
				ThisButtonHandler=BackToMain;
				ButtonList.Add(new MenuButton("Back to main menu",ThisButtonHandler,0,""));
			}
			else if(selectedMenuNode==MENU_NODE_VIEW_ALLSTATS)
			{
				ThisButtonHandler=BackToMain;
				ButtonList.Add(new MenuButton("Back to main menu",ThisButtonHandler,1,""));
				ThisButtonHandler=SetStatsPage;
				ButtonList.Add(new MenuButton("Demographics",ThisButtonHandler,1,""));
				ThisButtonHandler=SetStatsPage;
				ButtonList.Add(new MenuButton("Jobs",ThisButtonHandler,2,""));
				ThisButtonHandler=SetStatsPage;
				ButtonList.Add(new MenuButton("Gear",ThisButtonHandler,3,""));
				int iCount=4;
				while(iCount<=NUMPAGES)
				{
					ThisButtonHandler=SetStatsPage;
					ButtonList.Add(new MenuButton("Skills "+(iCount-3),ThisButtonHandler,iCount,""));
					iCount++;
				}
				ThisButtonHandler=StatsHideToggle;
				ButtonList.Add(new MenuButton("Toggle hide long dead ",ThisButtonHandler,1,""));
				ThisButtonHandler=StatsHideToggle;
				ButtonList.Add(new MenuButton("Toggle hide children ",ThisButtonHandler,2,""));
				ThisButtonHandler=StatsHideToggle;
				ButtonList.Add(new MenuButton("Toggle hide non-idle ",ThisButtonHandler,3,""));
			}
			else if(selectedMenuNode==MENU_NODE_COLONIST_COMBATDEPLOY)
			{
				ThisButtonHandler=BackToMain;
				ButtonList.Add(new MenuButton("Pick a tile to deploy to",ThisButtonHandler,0,"Click a tile in the main game to pick a deployment target, or click this button to cancel the assignment."));				
			}
			else if(selectedMenuNode==MENU_NODE_COLONIST_ATTACKTARGET)
			{
				ThisButtonHandler=BackToMain;
				ButtonList.Add(new MenuButton("Pick a target to attack",ThisButtonHandler,0,"Click a tile in the main game to pick an attack target, or click this button to cancel the assignment."));				
			}
			else if(selectedMenuNode==MENU_NODE_COLONIST_RENAME)
			{
				ThisTextHandler=RenameSelectedColonist;
				ButtonList.Add(new MenuButton("",ThisTextHandler,InhabitantList[SELECTED_COLONIST].Name,""));			
				ThisButtonHandler=BackToMain;
				ButtonList.Add(new MenuButton("Return to main menu",ThisButtonHandler,0,""));	
			}
		}
		DrawMenuFromButtonList();
	}

	//Return value on error: ""
	string GetnThDelimitedPart(string StringToDelimit,char cDelimitCharacter,int nNth)
	{
		if(StringToDelimit==null) return "";
		string[] sDelimited=StringToDelimit.Split(cDelimitCharacter);
		if(nNth>=sDelimited.Length) return "";
		return sDelimited[nNth].Trim ();
	}

	public int iniReadint(string iniFileName,string iniVarName,int iniEntry)
	{
		string sReadout=iniRead(iniFileName,iniVarName,iniEntry);
		if(sReadout=="") return 0;
		return int.Parse(sReadout);
	}
	
	public float iniReadfloat(string iniFileName,string iniVarName,int iniEntry)
	{
		string sReadout=iniRead(iniFileName,iniVarName,iniEntry);
		if(sReadout=="") return 0;
		return float.Parse(sReadout);
	}

	//public string sLang="eng";

	private string UnpackSpecialFlags(string sPacked,bool bTlk)
	{
		if(sPacked.IndexOf('*')!=0) return sPacked;//no asterisk
		if(sPacked.StartsWith("*info"))
		{
			if(sType=="IND") return GetColonistSummary(PersistInhabitantLoop);
		}
		if(sPacked.StartsWith("*infot"))
		{
			if(sType=="IND") return GetColonistSummary(InhabitantList.Count-1);
		}
		if(sPacked.StartsWith("*t_")&bTlk==false)
		{
			string[] sParsed=sPacked.Split('_');
			return iniRead ("tlk_"+AveFileReaders.sLang,"Text",int.Parse(sParsed[1]));
		}
		return sPacked;
	}

	public string iniRead(string iniFileName,string iniVarName,int iniEntry)
	{//
		FileStream fs=File.OpenRead(Environment.CurrentDirectory+"/Assets/Resources/rules/"+iniFileName+".txt");
		bool bTlk=false;
		if(iniFileName=="tlk_"+AveFileReaders.sLang) bTlk=true;
		char Delimiter='/';
		string fileContents="";
		using (StreamReader reader = new StreamReader(fs))
		{
			string Header=reader.ReadLine();
			while(iniEntry>0)
			{
				fileContents = reader.ReadLine();
				iniEntry=iniEntry-1;
			}
			int nCounter=0;
			string DelimitedString=GetnThDelimitedPart(Header,Delimiter,nCounter);
			while(DelimitedString!=iniVarName)
			{
				if(nCounter>AveFileReaders.MAX_2DA_SIZE) return "";
				DelimitedString=GetnThDelimitedPart(Header,Delimiter,nCounter);
				nCounter++;
			}
			return UnpackSpecialFlags(GetnThDelimitedPart(fileContents,Delimiter,nCounter-1),bTlk);
		}
	}	
}
