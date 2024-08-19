using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using rnd = UnityEngine.Random;
using KModkit;
using System.Text.RegularExpressions;

public class fitin : MonoBehaviour { //this entire mod's code is held together by duct tape and prayers I am so sorry, still gonna try to explain as much as I can

    public KMAudio audio;
	private KMAudio.KMAudioRef bgm;
    public KMBombInfo bomb;
    public KMBombModule Module;
	public KMSelectable themoditself;

	public GameObject menu;
	public GameObject everylvl;
	public Material[] colors;

	public KMSelectable leftarrow;
	public KMSelectable rightarrow;
	public KMSelectable diffconfirm;
	public TextMesh diffscreen;
	public TextMesh scoredisplay;

	public GameObject[] pieces;
	public GameObject lastpiecemark;
	public TextMesh time;
	public TextMesh coolmark;
	public TextMesh readygo;
	public TextMesh allclear;
	public TextMesh[] gradestuff;
	int currentPiece;
	int minosFilled;
	int removalconfirmation;
	
	public struct Piece{ //Basic building blocks (haha) of the pieces used
		public bool[,] Layout;
		public int Width;
		public int Height;
		public int ID;
		public Piece(bool[,] layout, int width, int height, int id){
			Layout = layout;
			Width = width;
			Height = height;
			ID = id;
		}
	}

	Piece[][] ThePieces = { //These are the actual pieces. All seven tetrominos and their rotations.
		new Piece[] { //I piece
			new Piece(new bool[,] {{true, true, true, true}}, 4, 1, 0),
			new Piece(new bool[,] {{true}, {true}, {true}, {true}}, 1, 4, 0)
		},
		new Piece[] { //J piece
			new Piece(new bool[,] {{true, true, true}, {false, false, true}}, 3, 2, 1),
			new Piece(new bool[,] {{false, true}, {false, true}, {true, true}}, 2, 3, 1),
			new Piece(new bool[,] {{true, false, false}, {true, true, true}}, 3, 2, 1),
			new Piece(new bool[,] {{true, true}, {true, false}, {true, false}}, 2, 3, 1)
		},
		new Piece[] { //L piece
			new Piece(new bool[,] {{true, true, true}, {true, false, false}}, 3, 2, 2),
			new Piece(new bool[,] {{true, true}, {false, true}, {false, true}}, 2, 3, 2),
			new Piece(new bool[,] {{false, false, true}, {true, true, true}}, 3, 2, 2),
			new Piece(new bool[,] {{true, false}, {true, false}, {true, true}}, 2, 3, 2)
		},
		new Piece[] { //Z piece
			new Piece(new bool[,] {{true, true, false}, {false, true, true}}, 3, 2, 3),
			new Piece(new bool[,] {{false, true}, {true, true}, {true, false}}, 2, 3, 3)
		},
		new Piece[] { //S piece
			new Piece(new bool[,] {{false, true, true}, {true, true, false}}, 3, 2, 4),
			new Piece(new bool[,] {{true, false}, {true, true}, {false, true}}, 2, 3, 4)
		},
		new Piece[] { //T piece
			new Piece(new bool[,] {{false, true, false}, {true, true, true}}, 3, 2, 5),
			new Piece(new bool[,] {{true, false}, {true, true}, {true, false}}, 2, 3, 5),
			new Piece(new bool[,] {{true, true, true}, {false, true, false}}, 3, 2, 5),
			new Piece(new bool[,] {{false, true}, {true, true}, {false, true}}, 2, 3, 5)
		},
		new Piece[] { //O piece
			new Piece(new bool[,] {{true, true}, {true, true}}, 2, 2, 6)
		}
	};

	public struct PlacePiece{ //Pieces in a placed location on the grid
		public Piece LaPiece; //I still don't know why I called it this, I didn't know I'd need to reference it so much
		public int X;
		public int Y;
		public PlacePiece(Piece lapiece, int x, int y){
			LaPiece = lapiece;
			X = x;
			Y = y;
		}
	}

	List<PlacePiece> piecesGenerated = new List<PlacePiece>();
	
	int gridsize = 6; //I intend to actually allow for changing grid sizes, but this requires more selectables and requires some more precise manipulation of the board, which I can't reasonably do without needing to redo a ton of shit

	public GameObject lvl1;
	public GameObject[] gridone;
	public GameObject[] lvl1overlay;
	public KMSelectable[] levelone;
	int[,] levelonegrid = {{0, 0, 0, 0, 0, 0}, {0, 0, 0, 0, 0, 0}, {0, 0, 0, 0, 0, 0}, {0, 0, 0, 0, 0, 0}, {0, 0, 0, 0, 0, 0}, {0, 0, 0, 0, 0, 0}}; //This grid is used to mark the grid for logging and allow for undos
	bool[,] lvl1checks = new bool[6,6]; //A temporary grid of sorts, for when in the middle of placing a piece

	int GamePhase; //0: menu; 1: startup; 2: piece placing; 3: ARE; 4: level clear; 5: timeout
	int Difficulty = 1; //difficulty goes from 1 to 6, detailed further below
	int Score; //36 required to solve
	int BoardsCleared; //for each individual segment
	int RoundsPlayed; //grade calculations

	bool CreditsRoll;
	float InvisDelay = 1000f;
	float credittimer = 55f;

	float timer = 240f; //4 minutes for everything, because I wanted diff 6 to be possible but still a very big challenge
	bool lowtime; //for the countdown thingy

	float flashtimer = 1.5f; //flashing text shouldn't go on for too long, for reasons
	bool flashingtext;

    static int moduleIdCounter = 1;
    int moduleId;
    private bool ModuleSolved;


    void Awake()
    {
        moduleId = moduleIdCounter++;
		leftarrow.OnInteract += delegate{
			NavLeft(); return false;
		};
		rightarrow.OnInteract += delegate{
			NavRight(); return false;
		};
		diffconfirm.OnInteract += delegate{
			ConfirmSelection(); return false;
		};
		for (byte i = 0; i < levelone.Length; i++)
        {
            KMSelectable lvl1inputs = levelone[i];
            lvl1inputs.OnInteract += delegate
            {
                Grid1(lvl1inputs);
                return false;
            };
        }
		themoditself.OnDefocus += delegate { stfu(); };
		themoditself.OnFocus += delegate { musicify(); };
		MenuTransition();
    }

	void Grid1(KMSelectable lvl1inputs){ //Ho boy, this is a doozy
		int press = Array.IndexOf(levelone, lvl1inputs);
        levelone[press].AddInteractionPunch(0.2f);
		if (GamePhase == 2){ //Only allow inputs when ARE is done
		if (levelonegrid[press/6,press%6] == 0){ //check if selected block is not yet filled, if so...
			if (removalconfirmation != 0)
				for (int i=0; i<gridsize; i++)
					for (int j=0; j<gridsize; j++)
						if (levelonegrid[i,j] == removalconfirmation)
							gridone[i*6+j].GetComponent<MeshRenderer>().material.color *= 0.75f;
			removalconfirmation = 0;
			if (!lvl1checks[press/6,press%6]){ //check if that block has not yet been selected for this piece
				gridone[press].GetComponent<Renderer>().material = colors[piecesGenerated[currentPiece-1].LaPiece.ID]; //colors 0-6 correspond to the colors of the respective piece IDs
				minosFilled++;
				lvl1checks[press/6,press%6] = true;
			}
			else{ //if it has already been selected...
				gridone[press].GetComponent<Renderer>().material = colors[7]; //...revert it! (7 is brown)
				minosFilled--;
				lvl1checks[press/6,press%6] = false;
			}
			if (minosFilled == 4){ //Only need to lock-check if 4 blocks are selected
				foreach (Piece checkedpiece in ThePieces[piecesGenerated[currentPiece-1].LaPiece.ID]) //similar validity check as to the grid generation algorithm (detailed below)
					for (int i=0; i<=gridsize-checkedpiece.Width; i++) //checking all the positions
						for (int j=0; j<=gridsize-checkedpiece.Height; j++){
							bool validPlace = true;
							for (int k=0; k<checkedpiece.Width; k++) //narrowing it down to the piece area
								for (int l=0; l<checkedpiece.Height; l++){
									if (checkedpiece.Layout[l,k] != lvl1checks[j+l,i+k]) validPlace = false; //if the 4 selected blocks work, the check grid XNOR the piece layout should be TRUE in every slot
								}
							if (validPlace){
								for (int k=0; k<gridsize; k++)
									for (int l=0; l<gridsize; l++){
										if (lvl1checks[k,l]){
											lvl1checks[k,l] = false; //revert the check grid's positions to false...
											levelonegrid[k,l] = currentPiece; //...set those positions in the lock grid to the corresponding piece in the list...
											StartCoroutine(LockPiece(k*6+l)); //...and play the lock animation.
										}
									}
								minosFilled = 0;
								audio.PlaySoundAtTransform("LOCK", transform);
								GamePhase = 3; //ARE, or entry delay, is what this is doing. Basically just preventing placing blocks out of order
								if (currentPiece == piecesGenerated.Count) //only possible to have all pieces placed if these are equal when a piece is locked
									StartCoroutine(ClearBoardOne());
								else {
									StartCoroutine(SpawnPiece());								
								}
							}
				}

			}
			if (GamePhase == 2) audio.PlaySoundAtTransform("PLACE", transform);					

		}
		else{ //if the selected block already has a piece
			int temp;
			temp = levelonegrid[press/6,press%6]; //the number pulled here is the same as the selected block's corresponding number in the lock grid
			if ((temp == removalconfirmation) || BoardsCleared >= 11){
				audio.PlaySoundAtTransform("EMPTY", transform);
				for (int i=0; i<gridsize; i++)
					for (int j=0; j<gridsize; j++)
						if (levelonegrid[i,j] == temp){
							levelonegrid[i,j] = 0; //0 is considered "no block", basically
							gridone[i*6+j].GetComponent<Renderer>().material = colors[7];
						}
				lastpiecemark.SetActive(false); //not foolproof if the thing is already flashing, but, well, the current piece is no longer the last one, so this shouldn't be on
				piecesGenerated.Add(piecesGenerated[temp-1]); //easier than messing with the entire list, instead we just copy the removed piece and add it to the end of the list
				removalconfirmation = 0;
			}
			else{ //a "double-check" of sorts, to be sure you don't accidentally remove a piece you didn't want to remove
				audio.PlaySoundAtTransform("LOCK", transform);
				if (removalconfirmation != 0)
					for (int i=0; i<gridsize; i++)
						for (int j=0; j<gridsize; j++)
							if (levelonegrid[i,j] == removalconfirmation)
								gridone[i*6+j].GetComponent<MeshRenderer>().material.color *= 0.75f;
				removalconfirmation = temp;
				for (int i=0; i<gridsize; i++)
					for (int j=0; j<gridsize; j++)
						if (levelonegrid[i,j] == temp)
							gridone[i*6+j].GetComponent<MeshRenderer>().material.color *= 1.333333333f;
			}
		}
		}
	}

	void stfu(){ //I just thought it was funny
		if (!CreditsRoll)
			StopSound();
	}

	void musicify(){
		if (GamePhase == 2){
			if (Difficulty <= 3){
				if (BoardsCleared >= 5)
					bgm = audio.PlaySoundAtTransformWithRef("Level 3", transform);
				else if (BoardsCleared >= 3)
					bgm = audio.PlaySoundAtTransformWithRef("Level 2", transform);
				else
					bgm = audio.PlaySoundAtTransformWithRef("Level 1", transform);
			}
			else if (Difficulty <= 3){
				if (BoardsCleared >= 11)
					bgm = audio.PlaySoundAtTransformWithRef("Level 6", transform);
				else if (BoardsCleared >= 7)
					bgm = audio.PlaySoundAtTransformWithRef("Level 5", transform);
				else if (BoardsCleared >= 4)
					bgm = audio.PlaySoundAtTransformWithRef("Level 4", transform);
				else
					bgm = audio.PlaySoundAtTransformWithRef("Level 3", transform);
			}
		}
		if (currentPiece == piecesGenerated.Count){
			switch (Difficulty){
				case 1: break;
				case 2:
					if (BoardsCleared == 2) StopSound();
					break;
				case 3: 
					if (BoardsCleared == 2) StopSound();
					if (BoardsCleared == 4) StopSound();
					break;
				case 4:
					if (BoardsCleared == 3) StopSound();
					break;
				case 5:
					if (BoardsCleared == 3) StopSound();
					if (BoardsCleared == 6) StopSound();
					break;
				case 6:
					if (BoardsCleared == 3) StopSound();
					if (BoardsCleared == 6) StopSound();
					if (BoardsCleared == 11) StopSound();
					break;
				default: break;
			}
		}
	}

	void OnDestroy(){
		StopSound();
	}

	public void StopSound() //KMAudioRefs are weird, man, this is pretty self-explanatory, it's used for the music so that it stops playing said music in some circumstances
    {
        if (bgm == null)
            return;

        bgm.StopSound(); //no, this does not cause an infinite loop or anything.
        bgm = null;
    }

	void NavLeft(){
		audio.PlaySoundAtTransform("select", transform);
		Difficulty--;
		if (Difficulty == 0) Difficulty = 6;
		diffscreen.text = Difficulty.ToString();
		float temp;
		temp = Difficulty;
		if (Difficulty<=3) diffscreen.color = new Color(1.0f-((4f-temp)/3f), 1.0f, 1.0f-((4f-temp)/3f));
		else diffscreen.color = new Color(1.0f, 1.0f-((temp-3f)/3f), 1.0f-((temp-3f)/3f));
	}

	void NavRight(){
		audio.PlaySoundAtTransform("select", transform);
		Difficulty++;
		if (Difficulty == 7) Difficulty = 1;
		diffscreen.text = Difficulty.ToString();
		float temp;
		temp = Difficulty;
		if (Difficulty<=3) diffscreen.color = new Color(1.0f-((4f-temp)/3f), 1.0f, 1.0f-((4f-temp)/3f));
		else diffscreen.color = new Color(1.0f, 1.0f-((temp-3f)/3f), 1.0f-((temp-3f)/3f));
	}
	void ConfirmSelection(){
		if (GamePhase == 0){
			RoundsPlayed++;
			StartCoroutine(TextBlink(diffscreen, false, 7, 1.0f, 0.9f, 0.0f));
			if (Difficulty <= 3)
				StartCoroutine(GeneratePuzzle(6, 6));
			else
				StartCoroutine(GeneratePuzzle(6, 8)); //this would have been nine, but that makes it VERY prone to freezing. it still does sometimes, but it's a lot less common with eight
			StopSound(); //placed before the line below because it would also stop that sound from playing
			audio.PlaySoundAtTransform("diff select", transform);
		}
	}

	void MenuTransition(){
		InvisDelay = 1000f;
		allclear.text = "";
		gradestuff[0].text = "";
		gradestuff[1].text = "";
		float temp;
		temp = Difficulty;
		if (Difficulty<=3) diffscreen.color = new Color(1.0f-((4f-temp)/3f), 1.0f, 1.0f-((4f-temp)/3f));
		else diffscreen.color = new Color(1.0f, 1.0f-((temp-3f)/3f), 1.0f-((temp-3f)/3f));
		for (int i=0; i<gridsize; i++)
			for (int j=0; j<gridsize; j++){
				levelonegrid[i,j] = 0;
				lvl1checks[i,j] = false;
			}
		diffscreen.text = Difficulty.ToString();
		BoardsCleared = 0;
		GamePhase = 0;
		currentPiece = 0;
		minosFilled = 0;
		scoredisplay.text = "Total: " + Score.ToString();
		float tempscorecheck;
		tempscorecheck = Convert.ToSingle(Score)/36f;
		if (tempscorecheck >= 1f) tempscorecheck = 1f;
		scoredisplay.color = new Color(1.0f-tempscorecheck, 1.0f, 1.0f-tempscorecheck);
		everylvl.transform.localScale = new Vector3(0.0001f, 0.0001f, 0.0001f); //instead of moving positions, they're heavily scaled down to become completely invisible
		lvl1.transform.localScale = new Vector3(0.0001f, 0.0001f, 0.0001f); //this comes with the bonus of not needing to take current positions into account
		menu.transform.localScale = new Vector3(1f, 1f, 1f);
		for (int i=0; i<7; i++){
			pieces[i].SetActive(false);
		}
		for (int i=0; i<36; i++){
			lvl1overlay[i].GetComponent<MeshRenderer>().material.color = new Color(1.0f, 1.0f, 1.0f, 0f);
		}
		readygo.text = "";
		coolmark.text = "";
		lastpiecemark.SetActive(false);
		timer = 240f;
		bgm = audio.PlaySoundAtTransformWithRef("Menu", transform);
	}

	IEnumerator LockPiece(int block){
		for (float i=0f; i<2f; i += 1f){
			lvl1overlay[block].GetComponent<MeshRenderer>().material.color = new Color(1.0f, 1.0f, 1.0f, i/2f);
			yield return new WaitForSeconds(0.01f);
		}
		gridone[block].GetComponent<MeshRenderer>().material.color *= 0.5f; //this does cause board clears to have the outline darkened, but I'll take it, it only lasts a second anyway
		for (float i=2f; i>0f; i -= 1f){
			lvl1overlay[block].GetComponent<MeshRenderer>().material.color = new Color(1.0f, 1.0f, 1.0f, i/2f);
			yield return new WaitForSeconds(0.01f);
		}	
		lvl1overlay[block].GetComponent<MeshRenderer>().material.color = new Color(1.0f, 1.0f, 1.0f, 0f);
		if (InvisDelay < 10f){
			int tempthing = BoardsCleared;
			int othertempthing = levelonegrid[block/6,block%6];
			yield return new WaitForSeconds(InvisDelay);
			if ((BoardsCleared == tempthing) && (levelonegrid[block/6,block%6] == othertempthing) && (GamePhase != 4))
				gridone[block].GetComponent<Renderer>().material = colors[7];
		}
	}

	IEnumerator TextBlink(TextMesh thetext, bool fastblink, int blinks, float red, float green, float blue){ //only used for two things but I may add more in the future, so this is heavily customizable
		flashingtext = true;
		for (int i=0; i<blinks; i++){
			if (fastblink) yield return new WaitForSeconds(0.01f);
			else yield return new WaitForSeconds(0.05f);
			thetext.color = new Color(red, green, blue, 1.0f);
			if (fastblink) yield return new WaitForSeconds(0.01f);
			else yield return new WaitForSeconds(0.05f);
			thetext.color = new Color(1.0f, 1.0f, 1.0f, 1.0f);
			if (flashtimer < 0f) i=1000000;
		}
		flashtimer = 1.5f;
		flashingtext = false;
		thetext.text = "";
	}

	IEnumerator SpawnPiece(){
		if (currentPiece != 0) { 
			if (Difficulty >= 4 && BoardsCleared >= 7)
				yield return new WaitForSeconds(0.15f); //this is the actual ARE, it disables the delay after this time (which shortens on higher difficulties)
			else if (Difficulty >= 4 && BoardsCleared >= 4)
				yield return new WaitForSeconds(0.3f);
			else 
				yield return new WaitForSeconds(0.5f);
			pieces[piecesGenerated[currentPiece-1].LaPiece.ID].SetActive(false); 
		}
		GamePhase = 2;
		pieces[piecesGenerated[currentPiece].LaPiece.ID].SetActive(true); //piece display in the upper right
		if (currentPiece+1 == piecesGenerated.Count){
			switch (Difficulty){ //this switch section allows for a slightly smoother transition to new music tracks
				case 1: break;
				case 2:
					if (BoardsCleared == 2) StopSound();
					break;
				case 3: 
					if (BoardsCleared == 2) StopSound();
					if (BoardsCleared == 4) StopSound();
					break;
				case 4:
					if (BoardsCleared == 3) StopSound();
					break;
				case 5:
					if (BoardsCleared == 3) StopSound();
					if (BoardsCleared == 6) StopSound();
					break;
				case 6:
					if (BoardsCleared == 3) StopSound();
					if (BoardsCleared == 6) StopSound();
					if (BoardsCleared == 11) StopSound();
					break;
				default: break;
			}
		}
		switch (piecesGenerated[currentPiece].LaPiece.ID){ //These sound effects, and the music, and pretty much everything, was taken from Tetris: The Grand Master 3 - Terror-Instinct
			case 0: audio.PlaySoundAtTransform("PIECE I", transform); break;
			case 1: audio.PlaySoundAtTransform("PIECE J", transform); break;
			case 2: audio.PlaySoundAtTransform("PIECE L", transform); break;
			case 3: audio.PlaySoundAtTransform("PIECE Z", transform); break;
			case 4: audio.PlaySoundAtTransform("PIECE S", transform); break;
			case 5: audio.PlaySoundAtTransform("PIECE T", transform); break;
			case 6: audio.PlaySoundAtTransform("PIECE O", transform); break;
		}
		currentPiece++;
		if (currentPiece == piecesGenerated.Count){ //I love giving people hope :3
			audio.PlaySoundAtTransform("LAST PIECE", transform);
			for (int i=0; i<4; i++){
				lastpiecemark.SetActive(true);
				yield return new WaitForSeconds(0.05f);
				lastpiecemark.SetActive(false);
				yield return new WaitForSeconds(0.05f);
			}
			lastpiecemark.SetActive(true);
		}
	}

	void Levelstuff(){
		if (GamePhase == 1){
			GamePhase++;
			if (Difficulty<4) bgm = audio.PlaySoundAtTransformWithRef("Level 1", transform); //similar to TGM3 master mode
			else bgm = audio.PlaySoundAtTransformWithRef("Level 3", transform); //...Shirase.
		}
		StartCoroutine(SpawnPiece());
	}

	IEnumerator ClearBoardOne(){
		for (int i=0; i<7; i++){ //To not fuck up the piece display
			pieces[i].SetActive(false);
		}
		BoardsCleared++;
		Debug.LogFormat("[fit IN! #{0}] Board cleared! Your total during this segment is {1}.", moduleId, BoardsCleared);
		for (int i=0; i<gridsize; i++)
			for (int j=0; j<gridsize; j++){
				levelonegrid[i,j] = 0;
				lvl1checks[i,j] = false;
			}
		switch (Difficulty){ //Each difficulty has a different set of music changes, clear requirements, and scores. It's pretty obvious which is which by looking at them.
			case 1: 
				if (BoardsCleared == 3){
				StopSound();
				audio.PlaySoundAtTransform("RANK UP", transform);
				Score++;
				GamePhase = 4;
				Debug.LogFormat("fit IN! #{0}] Level complete! Since the difficulty was 1, you only earned one point.", moduleId);
				}
				break;
			case 2: 
				if (BoardsCleared == 3) {bgm = audio.PlaySoundAtTransformWithRef("Level 2", transform);
				audio.PlaySoundAtTransform("NEW LEVEL", transform); }
				if (BoardsCleared == 5){
					StopSound();
					audio.PlaySoundAtTransform("RANK UP", transform);
					Score += 4;
					GamePhase = 4;
					Debug.LogFormat("fit IN! #{0}] Level complete! A difficulty of 2 means 4 points for you :)", moduleId);
				}
				break;
			case 3: 
				if (BoardsCleared == 3) {bgm = audio.PlaySoundAtTransformWithRef("Level 2", transform);
				audio.PlaySoundAtTransform("NEW LEVEL", transform); }
				if (BoardsCleared == 5) {bgm = audio.PlaySoundAtTransformWithRef("Level 3", transform);
				audio.PlaySoundAtTransform("NEW LEVEL", transform); }
				if (BoardsCleared == 7){
					StopSound();
					audio.PlaySoundAtTransform("RANK UP", transform);
					Score += 9;
					GamePhase = 4;
					Debug.LogFormat("fit IN! #{0}] Level complete! Difficulty 3 is definitely a bit of a step up, so have 9 points.", moduleId);
				}
				break;
			case 4: 
				if (BoardsCleared == 4) {bgm = audio.PlaySoundAtTransformWithRef("Level 4", transform);
				audio.PlaySoundAtTransform("NEW LEVEL", transform); }
				if (BoardsCleared == 7){
					StopSound();
					audio.PlaySoundAtTransform("RANK UP", transform);
					Score += 16;
					GamePhase = 4;
					Debug.LogFormat("fit IN! #{0}] Level complete! And at difficulty level 4, no less! 16 points!", moduleId);
				}
				break;
			case 5: 
				if (BoardsCleared == 4) {bgm = audio.PlaySoundAtTransformWithRef("Level 4", transform);
				audio.PlaySoundAtTransform("NEW LEVEL", transform); }
				if (BoardsCleared == 7) {bgm = audio.PlaySoundAtTransformWithRef("Level 5", transform);
				audio.PlaySoundAtTransform("NEW LEVEL", transform); }
				if (BoardsCleared == 11){
					StopSound();
					audio.PlaySoundAtTransform("RANK UP", transform);
					Score += 25;
					GamePhase = 4;
					Debug.LogFormat("fit IN! #{0}] Level complete! Daaaaaaamn, difficulty 5? Even I struggle with this. Have 25 points.", moduleId);
				}
				break;
			case 6: 
				if (BoardsCleared == 4) {bgm = audio.PlaySoundAtTransformWithRef("Level 4", transform);
				audio.PlaySoundAtTransform("NEW LEVEL", transform); }
				if (BoardsCleared == 7) {bgm = audio.PlaySoundAtTransformWithRef("Level 5", transform);
				audio.PlaySoundAtTransform("NEW LEVEL", transform); }
				if (BoardsCleared == 12) {bgm = audio.PlaySoundAtTransformWithRef("Level 6", transform);
				audio.PlaySoundAtTransform("NEW LEVEL", transform); }
				if (BoardsCleared == 17){
					StopSound();
					audio.PlaySoundAtTransform("FULL CLEAR", transform); //uses a different sound cuz I wanna signal total victory
					Score += 36;
					GamePhase = 4;
					Debug.LogFormat("fit IN! #{0}] Level complete! ...Okay, I thought this was straight up impossible. There's a reason you had to do this 17 times. You don't need to do it anymore. Thank you for playing, here's your free 36.", moduleId);
				}
				break;
			default: break;
		}
		for (int i=0; i<gridone.Length; i++)
			gridone[i].GetComponent<Renderer>().material = colors[8];
		currentPiece = 0;
		if (BoardsCleared%2==0){ //COOL/REGRET thingy
			float timedifference;
			timedifference = BoardsCleared; //your pace
			switch (Difficulty){ //based on how many boards you need to clear
				case 2: timedifference = timedifference/5f; break;
				case 3: timedifference = timedifference/9f; break;
				case 4: timedifference = timedifference/7f; break;
				case 5: timedifference = timedifference/11f; break;
				case 6: timedifference = timedifference/16f; break;
				default: break;
			}
			float timecheck;
			timecheck = (240f-timer)/240f; //the time itself, this is what your pace is compared to
			if (timedifference-timecheck<-0.1f) coolmark.text = "REGRET!"; //pace is 10% slower than if you were to finish with 0 seconds left
			if (timedifference-timecheck>0.1f) {coolmark.text = "COOL!!"; audio.PlaySoundAtTransform("COOL", transform); } //pace is 10% faster, thus warranting a sound
			StartCoroutine(TextBlink(coolmark, true, 100, 1.0f, 0.9f, 0.0f));
			}
		if (GamePhase != 4)
			if (Difficulty <= 3)
				if (BoardsCleared >= 5)
					StartCoroutine(GeneratePuzzle(6, 8));
				else if (BoardsCleared >= 3)
					StartCoroutine(GeneratePuzzle(6, 7));
				else
					StartCoroutine(GeneratePuzzle(6, 6));
			else
				StartCoroutine(GeneratePuzzle(6, 8));
		int temp;
		temp = rnd.Range(1,4);
		audio.PlaySoundAtTransform("CLEAR" + temp.ToString(), transform);
		if (Difficulty >= 4 && BoardsCleared >= 11)
			yield return new WaitForSeconds(0.1f);
		else if (Difficulty >= 4 && BoardsCleared >= 7)
			yield return new WaitForSeconds(0.25f);
		else if (Difficulty >= 4 && BoardsCleared >= 4)
			yield return new WaitForSeconds(0.45f);
		else 
			yield return new WaitForSeconds(0.7f);
		for (int i=0; i<gridone.Length; i++)
			gridone[i].GetComponent<Renderer>().material = colors[7];
		audio.PlaySoundAtTransform("EMPTY", transform);
		if (Difficulty >= 4 && BoardsCleared >= 11)
			yield return new WaitForSeconds(0.1f);
		else if (Difficulty >= 4 && BoardsCleared >= 7)
			yield return new WaitForSeconds(0.25f);
		else if (Difficulty >= 4 && BoardsCleared >= 4)
			yield return new WaitForSeconds(0.4f);
		else 
			yield return new WaitForSeconds(0.5f);
		if (GamePhase != 4) {
			StartCoroutine(SpawnPiece());
			}
		else{
			yield return new WaitForSeconds(3f);
			if (Score >= 36 && !ModuleSolved) {
				Module.HandlePass();
				ModuleSolved = true;
				CreditsRoll = true;
				BoardsCleared = 100;
				GamePhase = 2;
				Debug.LogFormat("[fit IN! #{0}] Congratulations! You've gotten enough points to solve the module! Now doing fading roll...", moduleId);
				Debug.LogFormat("[fit IN! #{0}] Note that clear messages during the fading roll have 100 added to them.", moduleId);
				StartCoroutine(GeneratePuzzle(6, 8));
				StartCoroutine(CreditRoll());
			}
			else MenuTransition();
		}
	}

	IEnumerator CreditRoll(){ //A bonus thing for people who wanna challenge themselves to be better
		yield return new WaitForSeconds(0.5f);
		if (RoundsPlayed == 1)
			bgm = audio.PlaySoundAtTransformWithRef("Ending 2", transform);
		else
			bgm = audio.PlaySoundAtTransformWithRef("Ending 1", transform);
		if (RoundsPlayed >= 6) InvisDelay = 5f;
		else InvisDelay = RoundsPlayed-1;
		currentPiece = 0;
		StartCoroutine(SpawnPiece());
		gradestuff[0].text = "PROGRAMMER";
		readygo.text = "Arc";
		yield return new WaitWhile (() => credittimer > 50f);
		gradestuff[0].text = "TESTER";
		readygo.text = "1254";
		yield return new WaitWhile (() => credittimer > 45f);
		gradestuff[0].text = "GEN. ALG.";
		readygo.text = "Obvi";
		yield return new WaitWhile (() => credittimer > 40f);
		gradestuff[0].text = "GRADING";
		readygo.text = "Arc";
		yield return new WaitWhile (() => credittimer > 35f);
		gradestuff[0].text = "SOUNDS";
		readygo.text = "TGM";
		yield return new WaitWhile (() => credittimer > 30f);
		gradestuff[0].text = "MUSIC";
		readygo.text = "TGM:TI";
		yield return new WaitWhile (() => credittimer > 25f);
		gradestuff[0].text = "TGM BY";
		readygo.text = "ARIKA";
		yield return new WaitWhile (() => credittimer > 20f);
		gradestuff[0].text = "SPECIAL" + System.Environment.NewLine + "THANKS"; //tbh this segment is just for cool people who weighed in some opinions :3
		readygo.text = "Kilo";
		yield return new WaitWhile (() => credittimer > 15f);
		readygo.text = "Maddy";
		yield return new WaitWhile (() => credittimer > 10f);
		readygo.text = "Ghost";
		yield return new WaitWhile (() => credittimer > 5f);
		gradestuff[0].text = "PRESENTED" + System.Environment.NewLine + "BY";
		readygo.text = "AR-C";
		readygo.characterSize = 1.3f;
		yield return new WaitWhile (() => credittimer > 0f);
		gradestuff[0].text = "";
		readygo.text = "";
		readygo.characterSize = 1f;
		GamePhase = 4;
		StopCoroutine(ClearBoardOne());
		for (int i=0; i<gridone.Length; i++)
			if (levelonegrid[i/6, i%6] != 0)
				gridone[i].GetComponent<Renderer>().material = colors[piecesGenerated[levelonegrid[i/6, i%6]-1].LaPiece.ID];
		allclear.text = "EXCELLENT" + System.Environment.NewLine + System.Environment.NewLine + "DIFF " + Difficulty.ToString() + System.Environment.NewLine + "ALL CLEAR";
		for (int i=0; i<29; i++){
			int thingamajig;
			thingamajig = rnd.Range(1,3);
			audio.PlaySoundAtTransform("fireworks" + thingamajig.ToString(), transform);
			yield return new WaitForSeconds(0.13f);
		}
		allclear.text = "";
		yield return new WaitForSeconds(2f);
		for (int i=0; i<gridone.Length; i++)
			gridone[i].GetComponent<Renderer>().material = colors[7];
		yield return new WaitForSeconds(0.25f);
		everylvl.transform.localScale = new Vector3(0.0001f, 0.0001f, 0.0001f);
		yield return new WaitForSeconds(0.25f);
		lvl1.transform.localScale = new Vector3(0.0001f, 0.0001f, 0.0001f);
		yield return new WaitForSeconds(1.5f);
		BoardsCleared -= 100;
		gradestuff[0].text = "GRADE"; //No, even though the calculations are very simple, you will not find out how grades are calculated, nor will you find out what each grade is :3
		CreditsRoll = false;
		MenuTransition();
	}

	IEnumerator GeneratePuzzle(int size, int piececount){ //MASSIVE thanks to obvi for helping with literally everything involving puzzle generation, I still don't understand it entirely myself lmao
		if (BoardsCleared == 0){
			timer = 240f;
			GamePhase = 1;
		}
		gridsize = size;
		piecesGenerated = GeneratePiece(piececount, new bool[gridsize,gridsize]);
		if (piecesGenerated == null) 
			throw new Exception("Pieces failed to generate!");
		int[,] generatedGrid = new int[gridsize, gridsize];
		for (int i=0; i<piecesGenerated.Count; i++)
			for (int j=0; j<piecesGenerated[i].LaPiece.Width; j++)
				for (int k=0; k<piecesGenerated[i].LaPiece.Height; k++)
					if (piecesGenerated[i].LaPiece.Layout[k,j]){
						if (generatedGrid[piecesGenerated[i].Y+k, piecesGenerated[i].X+j] != 0)
							throw new Exception("Overlap!");
						generatedGrid[piecesGenerated[i].Y+k, piecesGenerated[i].X+j] = piececount-i;
					}
		Debug.LogFormat("[fit IN! #{0}] The following grid was generated: {1}", moduleId, Enumerable.Range(0, gridsize).Select(x=>Enumerable.Range(0, gridsize).Select(y=>generatedGrid[x,y]==0?".":generatedGrid[x,y].ToString()).Join("")).Join(","));
		if (BoardsCleared == 0){
		yield return new WaitForSeconds(0.7f);
		everylvl.transform.localScale = new Vector3(1f, 1f, 1f);
		lvl1.transform.localScale = new Vector3(1f, 1f, 1f);
		menu.transform.localScale = new Vector3(0.0001f, 0.0001f, 0.0001f);
		yield return new WaitForSeconds(0.4f);
		readygo.text = "READY";
		audio.PlaySoundAtTransform("ready", transform);
		yield return new WaitForSeconds(1f);
		readygo.text = "";
		yield return new WaitForSeconds(0.1f);
		readygo.text = "GO";
		audio.PlaySoundAtTransform("go", transform);
		yield return new WaitForSeconds(1.1f);
		readygo.text = "";
		Levelstuff();
		}
	}

	List<PlacePiece> GeneratePiece(int piececount, bool[,] coverage){ //Actually generating the pieces, I wish I was told how this actually works
		if (piececount == 0)
		return new List<PlacePiece>();

		List<PlacePiece> candidates = new List<PlacePiece>();

		foreach (Piece[] pieceCategory in ThePieces)
			foreach (Piece pieceType in pieceCategory)
				for (int i=0; i<=gridsize-pieceType.Width; i++)
					for (int j=0; j<=gridsize-pieceType.Height; j++)
					{
						bool canPlace = true;
						for (int k=0; k<pieceType.Width && canPlace; k++)
							for (int l=0; l<pieceType.Height && canPlace; l++)
								canPlace &= !(coverage[j+l,i+k]&&pieceType.Layout[l,k]);
						if (canPlace)
							candidates.Add(new PlacePiece(pieceType, i, j));
					}

		candidates.Shuffle();
		foreach (PlacePiece candidate in candidates){
			bool[,] newCoverage = new bool[gridsize,gridsize];
			for (int i=0; i<gridsize; i++)
				for (int j=0; j<gridsize; j++)
					newCoverage[i,j] = coverage[i,j];
			for (int i=0; i<candidate.LaPiece.Width; i++)
				for (int j=0; j<candidate.LaPiece.Height; j++)
					newCoverage[candidate.Y+j,candidate.X+i] |= candidate.LaPiece.Layout[j,i];
			
			List<PlacePiece> result = GeneratePiece(piececount-1, newCoverage);
			if (result != null){
				result.Add(candidate);
				return result;
			}
		}

		return null;
	}

	IEnumerator TimeCountdown(){ //<10 seconds left, similar to Sakura mode from tgm3
		lowtime = true;
		for (int i=120; i>0; i--){
			if (i%2 == 0)
				time.color = new Color(1.0f, 0f, 0f, 1.0f);
			else
				time.color = new Color(0f, 1.0f, 0f, 1.0f);
			if (i<30){
				if (i%3 == 0)
					audio.PlaySoundAtTransform("TIME LOW", transform);
			}
			else if (i<60){
				if (i%6 == 0)
					audio.PlaySoundAtTransform("TIME LOW", transform);
			}
			else{
				if (i%12 == 0)
					audio.PlaySoundAtTransform("TIME LOW", transform);
			}
			float temp;
			temp = Convert.ToSingle(i-1);
			temp = temp/12;
			yield return new WaitWhile(() => timer > temp);
		}
		GamePhase = 5;
		timer = 0f;
		StopSound();
		StopCoroutine(SpawnPiece());
		StopCoroutine(ClearBoardOne());
		audio.PlaySoundAtTransform("TIME UP", transform);
		readygo.text = "TIME UP";
		if (!ModuleSolved)
			Module.HandleStrike();
		Debug.LogFormat("[fit IN! #{0}] You ran out of time. Failed at difficulty {1} with {2} boards cleared.", moduleId, Difficulty, BoardsCleared);
		for (int i=0; i<gridone.Length; i++)
			gridone[i].GetComponent<Renderer>().material = colors[7];
		yield return new WaitForSeconds(3f);
		readygo.text = "";
		yield return new WaitForSeconds(2f);
		MenuTransition();
	}

	void Update(){ //general timer shenanigans
		if (GamePhase >= 2 && GamePhase < 4 && !CreditsRoll)
			timer -= Time.deltaTime;
		int temp;
		temp = (int) (timer/60f);
		time.text = temp.ToString();
		int temp2;
		temp2 = (int) (timer-temp*60);
		if (temp2 < 10) time.text += ":0" + temp2.ToString();
		else time.text += ":" + temp2.ToString();
		int temp3;
		temp3 = (int) ((timer-temp*60-temp2)*100f);
		if (temp3 < 10) time.text += ".0" + temp3.ToString();
		else time.text += "." + temp3.ToString();
		if (!lowtime && timer<=10f)
			StartCoroutine(TimeCountdown());
		if (flashingtext) flashtimer -= Time.deltaTime;
		if (currentPiece != piecesGenerated.Count)
			lastpiecemark.SetActive (false);
		if (CreditsRoll)
			credittimer -= Time.deltaTime;
	}


}
