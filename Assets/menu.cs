using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using rnd = UnityEngine.Random;
using KModkit;
using System.Text.RegularExpressions;

public class menu : MonoBehaviour {

	public GameObject[] menued;
	public Animator[] arrows = new Animator[2];

	void Awake() {
		arrows [0] = menued [1].GetComponent<Animator> ();
		arrows [1] = menued [2].GetComponent<Animator> ();
		arrows [0].Play ("Base Layer.left pulse", -1, 0f);
		arrows [1].Play ("Base Layer.right pulse", -1, 0f);
	}
}
