using System;
﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;
using System.Text.RegularExpressions;

public class classicalOrderSrcipt : MonoBehaviour
{
	public KMAudio Audio;
	public KMBombInfo bomb;

	public KMSelectable[] buttons;
	public Material[] ledOptions;
	public Color[] fontColors; //0=transparent, 1=white
	public TextMesh[] lables;

	private string allPossibleCharacters = "αεπθψζωμΞδΓσηβξΔκΛφΠΣ"; // all 21 characters in no specific order
	private string[] charactersOnColumn = new string[7]; // characters on chosen column
	private string[] charactersToDelete = new string[5]; // characters not on the buttons on random
	private string displayedCharacters; // characters minus the chosen column to populate keypad

	// overwiev of all columns 0-8 from left to right [col,char]
	private string[, ] column = new string[9, 7] { {"Ξ", "δ", "η", "Λ", "Σ", "α", "ζ"},
	 																			 				 {"φ", "ε", "θ", "ω", "δ", "ξ", "κ"},
																			 	 		 		 {"π", "ψ", "μ", "δ", "β", "Δ", "Π"},
																			   		 		 {"Γ", "η", "Δ", "Π", "α", "θ", "Ξ"},
																			   		 		 {"ε", "ψ", "ω", "ξ", "Γ" ,"Λ" ,"Σ"}, //mim and nun are swapped?
																			   		 		 {"ζ", "μ", "Γ", "β", "κ", "φ", "π"},
																			 	 		 		 {"α", "θ", "ω", "σ", "β", "Δ", "φ"},
																			 	 		 		 {"σ", "η", "Λ", "Π", "ε", "ψ", "μ"},
																			 	 		 		 {"ξ", "κ", "Σ", "π", "ζ", "Ξ", "σ"} };
	// Lists
	private int postitionInTable;
	private string tableToRight = "ΞδηΛΣαζφεθωδξκπψμδβΔΠΓηΔΠαθΞεψωξΓΛΣζμΓβκφπαθωσβΔφσηΛΠεψμξκΣπζΞσ";
	private string tableToLeft = "ξκΣπζΞσσηΛΠεψμαθωσβΔφζμΓβκφπεψωξΓΛΣΓηΔΠαθΞπψμδβΔΠφεθωδξκΞδηΛΣαζ";

	private string tableInDirection;
	private string tableFromColumn;

	private string solution = "";
	private int stage = 0;

  private int choosenColumn;
	private bool reverseDirection = false; // is true when the ToLeft reading order is set
	private string serialNumber;

	private string checkAlphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

	private KMSelectable[] alreadyPressedButtons = new KMSelectable[8];
	//private KMSelectable lastButtonPressed;

	static int moduleIdCounter = 1;
	int moduleId;
	private bool moduleSolved;
	private bool inStrike;




	void Awake()
	{
		moduleId = moduleIdCounter++;
		//disable all text on the module until the lights turn on
		foreach (KMSelectable button in buttons)
		{
			button.GetComponentInChildren<TextMesh>().color = fontColors[0];
		}

		foreach (KMSelectable button in buttons)
			{
				KMSelectable pressedButton = button;
				button.OnInteract += delegate () { ButtonPress(pressedButton); return false; };
			}
		GetComponent<KMBombModule>().OnActivate += OnActivate;
	}

	void OnActivate()
  {
        //the lights have turned on, activate all text
 			foreach (KMSelectable button in buttons)
			{
				button.GetComponentInChildren<TextMesh>().color = fontColors[1];
			}
  }

	// Use this for initialization
	void Start ()
	{
		ChooseColumn();
		PopulateKeypad();
		FindSolution();
	}


	void ChooseColumn()
	{
		choosenColumn = UnityEngine.Random.Range(0, 9); // chose one of the 9 columns (left to right)

		for (int i = 0; i < 7; i++) //write the characters of the selected column in a string
		{
			charactersOnColumn[i] = column[choosenColumn, i];
		}

		foreach (var c in charactersOnColumn) //delete those caracters from the all possible charaters list.
		{
			allPossibleCharacters = allPossibleCharacters.Replace(c, "");
		}

		//randomize this list of 14 leftover characters
		System.Random r = new System.Random();
		displayedCharacters = new string(allPossibleCharacters.ToCharArray().OrderBy(s => (r.Next(2) % 2) == 0).ToArray());

		for (int i = 0; i < 5; i++) //write the first 5 characters of said string into a new one, they're gonna be deleted
		{
			charactersToDelete[i] = displayedCharacters[i].ToString();
		}
		//now delete the first 5 of the 14 characters for 9 remaining to display on buttons
		displayedCharacters = displayedCharacters.Remove(0, 5);


		Debug.LogFormat("[Classical Order #{0}] Choosen Column is: {1}. Possible letters on buttons are: {2}.", moduleId, choosenColumn+1, allPossibleCharacters);
		Debug.LogFormat("[Classical Order #{0}] 5 randomly deleted letters: {1}{2}{3}{4}{5}. 9 letters on buttons are: {6}.", moduleId, charactersToDelete[0], charactersToDelete[1], charactersToDelete[2], charactersToDelete[3], charactersToDelete[4] , displayedCharacters);
	}


	void PopulateKeypad()
	{
		for (int i = 0; i < 9; i++) // this loop assignes the other letters to the rest of the buttons, and skips the cyan button.
		{
			buttons[i].GetComponentInChildren<TextMesh>().text = displayedCharacters[i].ToString();
		}
	}


	void FindSolution()
	{
		CalculateDirection(); //calculate reading direction based on edgework
		tableFromColumn = tableInDirection; // this is the table that will now be modified

		if (reverseDirection) // for RtoL and LtoR reverse
		{
			postitionInTable = 56 - (choosenColumn * 7);
		}
		else // for LtoR and RtoL reverse
		{
			postitionInTable = choosenColumn * 7;
		}

		tableFromColumn = tableFromColumn.Remove(0, postitionInTable); // remove the beginning of the table before the start of the chosen column
		tableFromColumn = tableFromColumn.Insert((63 - (postitionInTable)), tableInDirection.Remove(postitionInTable, (63 - postitionInTable))); // fill in the rest of the string with the beginning again

		Debug.LogFormat("[Classical Order #{0}] The table in reading order from chosen column is: {1}.", moduleId, tableFromColumn);
		//remove all characters that are not on the keypad
		for (int i = 0; i < 7; i++)
		{
			tableFromColumn = tableFromColumn.Replace(charactersOnColumn[i], "");
		}
		for (int i = 0; i < 5; i++)
		{
			tableFromColumn = tableFromColumn.Replace(charactersToDelete[i], "");
		}
		Debug.LogFormat("[Classical Order #{0}] Letters matching the Keypad in reading order from chosen column are: {1}.", moduleId, tableFromColumn);
		//remove duplicates and only keep 1st occurance
		for (int i = 0; i < tableFromColumn.Length; i++)
		{
			// check if letter on i is present before
			int j;
			for (j = 0; j < i; j++)
			{
				if (tableFromColumn[i] == tableFromColumn[j])
				{
					break;
				}
			}
			//If not present, add it to solution
			if (i == j)
			{
				solution += tableFromColumn[i].ToString();
			}
		}
		Debug.LogFormat("[Classical Order #{0}] Solution is: {1}.", moduleId, solution);
	}


	void CalculateDirection()
	{
		serialNumber = bomb.GetSerialNumber();

		if (checkAlphabet.Contains(serialNumber[0])) //if first position is a letter
		{
			if (checkAlphabet.Contains(serialNumber[1])) //if second position is a letter
			{
				reverseDirection = true;
				tableInDirection = tableToLeft;
				Debug.LogFormat("[Classical Order #{0}] Serial format is: XX. Reading direction is: right to left, top down. (0)", moduleId);
			}
			else //if second position is a number
			{
				tableInDirection = tableToRight;
				Debug.LogFormat("[Classical Order #{0}] Serial format is: X#. Reading direction is: left to right, top down. (1)", moduleId);
			}
		}
		else //if first position is a number
		{
			if (checkAlphabet.Contains(serialNumber[1])) //if second position is a letter
			{
				reverseDirection = true;
				tableInDirection = Reverse(tableToRight);
				Debug.LogFormat("[Classical Order #{0}] Serial format is: #X. Reading direction is: right to left, bottom up. (2)", moduleId);
			}
			else //if second position is a number
			{
				tableInDirection = Reverse(tableToLeft);
				Debug.LogFormat("[Classical Order #{0}] Serial format is: ##. Reading direction is: left to right, bottom up. (3)", moduleId);
			}
		}
	}

	public static string Reverse( string s ) //method to reverse a string of text
	{
    char[] charArray = s.ToCharArray();
    Array.Reverse( charArray );
    return new string( charArray );
	}



	void ButtonPress(KMSelectable button)
	{
		if(moduleSolved || inStrike){return;}

		for (int i = 0; i < stage; i++)
		{
			if (button == alreadyPressedButtons[i])
			{
				Debug.LogFormat("[Classical Order #{0}] Stage:{1}. Module expecting: {2}. You already pressed: {3}. This button is inactive.", moduleId, stage+1, solution[stage].ToString(), button.GetComponentInChildren<TextMesh>().text);
				return;
			}
		}

		button.AddInteractionPunch();//adds a little punch to the bomb
		Audio.PlaySoundAtTransform("stoneButton", transform);
		//lastButtonPressed = button;

		if (solution[stage].ToString() == button.GetComponentInChildren<TextMesh>().text)
		{
			if (stage < 8)
			{
				Debug.LogFormat("[Classical Order #{0}] Stage:{1}. Module expecting: {2}. You pressed: {3}. Correct!", moduleId, stage+1, solution[stage].ToString(), button.GetComponentInChildren<TextMesh>().text);
				button.GetComponentInChildren<MeshRenderer>().material = ledOptions[1]; // turn LED green
				alreadyPressedButtons[stage] = button;
				stage++;
				StartCoroutine(ButtonAnimation(button));
			}
			else
			{
				GetComponent<KMBombModule>().HandlePass();
				Audio.PlaySoundAtTransform("solveSound", transform);
				moduleSolved = true;
				button.GetComponentInChildren<MeshRenderer>().material = ledOptions[1];
				Debug.LogFormat("[Classical Order #{0}] Stage:{1}. Module expecting: {2}. You pressed: {3}. Module Solved!", moduleId, stage+1, solution[stage].ToString(), button.GetComponentInChildren<TextMesh>().text);
				StartCoroutine(ButtonAnimation(button));
			}
		}
		else
		{
			GetComponent<KMBombModule>().HandleStrike();
			Debug.LogFormat("[Classical Order #{0}] Stage:{1}. Module expecting: {2}. You pressed: {3}. Wrong!", moduleId, stage+1, solution[stage].ToString(), button.GetComponentInChildren<TextMesh>().text);
			StartCoroutine(Strike(button));
		}


	}

				IEnumerator Strike(KMSelectable pressedButton)
				{
					inStrike = true; // this prevents the module from any unwnated button presses
					pressedButton.GetComponentInChildren<MeshRenderer>().material = ledOptions[2]; // the wrongly pressed buttons LED turnes red
					//yield return new WaitForSeconds(0.3f);
					int movement = 0;
					while (movement < 7)
					{
						yield return new WaitForSeconds(0.0001f);
						pressedButton.transform.localPosition = pressedButton.transform.localPosition + Vector3.up * -0.001f;
						movement++;
					}
					//yield return new WaitForSeconds(0.005f);
					movement = 0;
					while (movement < 7)
					{
						yield return new WaitForSeconds(0.0001f);
						pressedButton.transform.localPosition = pressedButton.transform.localPosition + Vector3.up * 0.001f;
						movement++;
					}
					pressedButton.GetComponentInChildren<MeshRenderer>().material = ledOptions[0]; // the wrongly pressed buttons LED turnes black
					inStrike = false; // this prevents the module from any unwnated button presses
				}

				IEnumerator ButtonAnimation(KMSelectable pressedButton)
				{

					int movement = 0;
					while (movement < 7)
					{
						yield return new WaitForSeconds(0.0001f);
						pressedButton.transform.localPosition = pressedButton.transform.localPosition + Vector3.up * -0.001f;
						movement++;
					}
					StopCoroutine("buttonAnimation");
				}


}
