# Caapture The Flag  
  
Capture-the-flag game project created for GD2P01 Artificial Intelligence For Games Assessment 4  
  
  
## Table of Contents  
  
- [Description](#Description)  
- [Features](#Features)  
- [Requirements](#Requirements)  
- [Installation](#Installation)  
- [Controls](#Controls)  
- [Disclaimer](#Disclaimer)  
- [Credits](#Credits)  
  
  
## Description  
  
Capture The Flag is a capture-the-flag game implemented in Unity, featuring AI-controlled agents and player-controlled agents. The game includes mechanics for territory control, flag capturing, and agent imprisonment and rescue.
  
  
## Features  
  
### Player Agent Selection  
The player moves using the WASD keys.  
Selection of ally agents to control with 1-4 keys.  
  
### AI Implementations  
State machines - AI operates based on states that define their behaviours  
Pathfinding - AI uses basic pathfinding to navigate their own base  
Decision Making - AI make decisions at certain intervals  
Patrolling - AI move around their base as default action  
Chase - AI attempt to catch enemy agents in their territory  
Evade - AI attempt to avoid being caught by enemy agents in enemy territory  
  
  
## Requirements  
  
- Windows based PC  
- Unity Engine Editor 2022.3.29f1 for project files  
- C# editor  
- Colliders and Rigidbodies dependencies  
  
  
## Installation  
  
This program can be run from the .exe file provided in the "Build.zip" folder after extracting. There are folders also provided to load necessary game files.  
This program can be downloaded and run in Unity Engine Editor 2022.3.29f1 from the Unity Hub and adding a project from disk > select "Source Code" folder after extracting.  
  
  
## Controls  
  
- Up: w / up arrow key  
- Down: s / down arrow key  
- Left: a / left arrow key  
- Right: d / right arrow key  
- 1, 2, 3, 4: Agent selections  
- Esc: Pause  
  
  
## Disclaimer  
  
This program is as complete as I can get it for submission.  
Debug logs were used extensively throughout the code to trace agent states and actions. All functions have been cleaned or submission.  
All code was written by Shiko based off my own knowledge from classes with lecturers Samah and Alexa, and self driven research of the Unity Engine Editor.  
  
  
## Credits  
  
Shikomisen (Ayoub) 2024  
Media Design School  
GD2P01 - Artificial Intelligence for Games  
Written for Assessment 4  