using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Xml;
using UnityEngine;
using UnityEngine.VR;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.Networking;
using System.Runtime.Serialization.Formatters.Binary;


public class LocationManager : MonoBehaviour {

	public GameObject proj_sphere;
	public GameObject Player;
	public static GameObject currentLocation; // at what location is the user at this point
	public Canvas mycanvas;
	public Text debugtext;
	public Text debugtext2;
	public GvrVideoPlayerTexture videostream;

	GameObject startLocation; // Start Location of the user --> for now it will be camera position (0 , 0 , 0 ) TODO:change  
	List<VideoClass> possibleLocations;// list of the next possible locations in area of currentLocation
	string currentVideo;
	float num_near_positions; //number of nearby possitions
	List<VideoClass> locationPositions; //List of camera locations
	string MyScene;
	string xmlName;
	GameObject camera;
	bool startmoving = false;
	bool not_start = false;
	Vector3 next_location;

	// Use this for initialization
	void Start () {
		//
		/*MyScene = SceneManager.GetArguments();
	    switch (MyScene) {
		case "Greece":
			xmlName = "PositionLocations";
			break;
		case "Classroom":
			xmlName = "ClassRoomDemo";
			break;

		}*/
		//VideoStream.videoURL = "https://storage.googleapis.com/daydream-deveng.appspot.com/japan360/dash/japan_day06_eagle2_shot0005-2880px_40000kbps.mpd";
		camera = GameObject.FindWithTag("MainCamera");
		xmlName = "VideoPositions";
		videostream = proj_sphere.GetComponent<GvrVideoPlayerTexture>();
		//Debug.Log (videostream);
		locationPositions = new List<VideoClass> ();
		possibleLocations = new List<VideoClass> ();
		startLocation = (GameObject)Instantiate (Resources.Load ("Prefabs/CLocation"), new Vector3 (0f, 0f, 0f), Quaternion.Euler (0, 0, 0));
		GetLocationPositions(ref locationPositions, xmlName, ref currentVideo); // get all the available locations / only done once
		Navigate(startLocation);
		Debug.Log ("start video = " + currentVideo);
		changeVideo(currentVideo);
 	}

	// Update is called once per frame
	void Update () {

		videostream.statusText = debugtext2;

		debugtext.text = 
		//debugtext.text ="PlayerState = " + videostream.PlayerState + "\nurl = "+ videostream.videoURL+ 
			"Head Rotation = "+camera.transform.rotation.eulerAngles;
		//debugtext.text = "Status = " +videostream.statusText.text;
		
	    

		//videostream.UpdateStatusText();
		//Debug.Log (videostream.statusText);
		if (startmoving && next_location !=null) {
			//Debug.Log(Player.transform.position.x +" && " + next_location.x);
			//Debug.Log ("are they equal "+ (Player.transform.position.x == next_location.x));
			if (Player.transform.position.x == next_location.x && Player.transform.position.z == next_location.z) {
				startmoving = false;
				UpdateLocations ();
				changeVideo(currentVideo);

			}else{
				if (Player.transform.position.x < next_location.x && Player.transform.position.x != next_location.x) {
					Player.transform.position = new Vector3 (Player.transform.position.x + 1, Player.transform.position.y, Player.transform.position.z);
				}else if(Player.transform.position.x > next_location.x){
					Player.transform.position = new Vector3 (Player.transform.position.x - 1, Player.transform.position.y, Player.transform.position.z);
				}

				if (Player.transform.position.z < next_location.z && Player.transform.position.z != next_location.z) {
					Player.transform.position = new Vector3 (Player.transform.position.x, Player.transform.position.y, Player.transform.position.z + 1);
				}else if(Player.transform.position.z > next_location.z){
					Player.transform.position = new Vector3 (Player.transform.position.x, Player.transform.position.y, Player.transform.position.z -1);
				}
					
				System.Threading.Thread.Sleep (100);
			}
			}
	}


	public void Navigate(GameObject nextLocation){

		next_location = nextLocation.transform.position;
		startmoving = true;
		currentLocation = nextLocation;
	}
	void UpdateLocations(){
		//remove previous points
		GameObject[] presentGameObjects = GameObject.FindGameObjectsWithTag("Location");
		//Debug.Log ("present gameobjects: " + presentGameObjects.Length );
		foreach (GameObject loc in presentGameObjects) {
			Destroy (loc);
		}
		possibleLocations.Clear ();


		//Player.transform.position = new Vector3(nextLocation.transform.position.x, 0,nextLocation.transform.position.z) ;
		mycanvas.transform.position = new Vector3(Player.transform.position.x, 0,Player.transform.position.z + 10) ;

		proj_sphere.transform.position = next_location;
		//currentLocation = nextLocation;
		CalculateNearby();

		if(possibleLocations != null){
		//TODO : improve this code
		GameObject nearlocation;
			foreach (VideoClass pos in possibleLocations) {
				if (pos.getx () == currentLocation.transform.position.x &&
				    pos.getz () == currentLocation.transform.position.z) {

					currentVideo = pos.getVideo ();
					//nearlocation = (GameObject)Instantiate (Resources.Load ("Prefabs/CLocation"), new Vector3 (pos.getx (), pos.gety (), pos.getz ()), Quaternion.Euler (0, 0, 0));

			
				} else {
					nearlocation = (GameObject)Instantiate (Resources.Load ("Prefabs/CLocation"), new Vector3 (pos.getx (), pos.gety () - 1, pos.getz ()), Quaternion.Euler (0, 0, 0));
					GameObject canvas = nearlocation.transform.GetChild (0).gameObject;
					GameObject mytext = canvas.transform.GetChild (1).gameObject;
					mytext.GetComponent<Text> ().text = "Camera at position = (" + pos.getx () + "," + pos.gety () + "," + pos.getz () + ")";
					//mytext.text = "Real Camera";

					if (Math.Abs (pos.getx () - next_location.x) > 0.5) {
						if (pos.getx () - next_location.x > 0) {
							canvas.transform.rotation = Quaternion.Euler (0, 90, 0);
						} else if (pos.getx () - next_location.x < 0) {
							canvas.transform.rotation = Quaternion.Euler (0, -90, 0);
						}
					}
				
			
					if (pos.getz () - next_location.z > 0.5  && pos.getz () < next_location.z) {
						canvas.transform.rotation = Quaternion.Euler (0, 180, 0);
						Debug.Log (pos.getz () + " == " + next_location.z);
					}
					Debug.Log (mytext);
				}
				}

			}
	}



	void CalculateNearby(){

		foreach (VideoClass video in locationPositions) {
			//Debug.Log (Math.Abs (currentLocation.transform.position.x - video.getx ()));
			//if (Math.Abs (currentLocation.transform.position.x - video.getx ()) < 9 && Math.Abs (currentLocation.transform.position.z - video.getz ())< 9) {
				possibleLocations.Add (video);
			//}
		}
		//Debug.Log( possibleLocations.Count);
	}

/*
 * Function that reads an XML file containing the positions of the camera and stores the location in an object 
 * 
 */

	public static void GetLocationPositions ( ref List<VideoClass> locationPositions , string xmlName, ref string currentVideo ) {
		//PositionLocations
		TextAsset textAsset = (TextAsset)Resources.Load (xmlName, typeof(TextAsset));
		XmlDocument doc = new XmlDocument ();
		doc.LoadXml (textAsset.text);

		XmlNodeList videos = doc.GetElementsByTagName("video");

		//videos
		foreach (XmlNode video in videos) {

			float x = 0, y = 0, z = 0;
			string videoName = "";

			foreach (XmlNode coordinate in video.ChildNodes) {


				switch (coordinate.Name){
				case "x":
					x = float.Parse (coordinate.InnerText,System.Globalization.CultureInfo.InvariantCulture.NumberFormat);
					break;
				case "y":
					y = float.Parse (coordinate.InnerText,System.Globalization.CultureInfo.InvariantCulture.NumberFormat);
					break;
				case "z":
					z = float.Parse (coordinate.InnerText,System.Globalization.CultureInfo.InvariantCulture.NumberFormat);
					break;
				case "name":
					videoName = coordinate.InnerText;
					break;
				}
			}

			if (x == 0 && y == 0 && z == 0) {
				currentVideo = videoName;
			}

			VideoClass position = new VideoClass (video.Attributes["id"].Value, x, y, z,videoName);
			locationPositions.Add (position);

		}

	}




	void changeVideo(string videoName){
		//Debug.Log (videoName);

		videostream.videoURL = videoName;
		videostream.CleanupVideo ();
		videostream.ReInitializeVideo();
		/*videostream.SetOnExceptionCallback(
			(type, message) => {
				debugtext2.text = debugtext2.text + "\nException of type: " + type + ": " + message;
			}
		);*/
		//videostream.RestartVideo ();

	}
		

	public void Return(){
		Debug.Log ("yesssssssssss");
		//SceneManager.LoadScene ("startSceen", "");
	}	 

}
