=========== WHEEL OF VIDEO GAMES ============
FOOHY AND SUNABOUZU MADE THIS
http://www.pixeltailgames.com/steamwheel/
https://github.com/Foohy/wheelofsteamgames
http://foohy.net
=============================================

0) System Requirements
	This requires .Net runtime 4.0 and OpenGL 3.2 (or above)
	Some RAM and a CPU won't hurt either


1) The settings file (settings.cfg)
	The file is stored in a simple human readable-editable JSON format. If it doesn't exist, the engine will recreate it with default settings. Else, it'll load it up and use the settings described

	WHAT EACH SETTING DOES (THERE ISN'T A LOT):
	VSync: Sets the vsync mode.
		0 - off
		1 - on
		2 - adaptive

	WindowMode: How the window will appear
		0 - Normal (windowed)
		1 - Minimized
		2 - Maximized
		3 - Fullscreen

	NoBorder: Remove the border of the window (Useful for appearing like fullscreen but acting like a window)

	Width/Height: The resolution of the screen. This is rather self explanatory

	ShadowMapSize: The size (width and height) of the shadowmap. Higher values will result in a better looking map but is more perfomance intensive.

	Samples: MSAA samples for antialiasing

	AnisotropicFiltering: How much anisotropic filtering should be applied to textures. This makes textures that aren't perpendicular to the camera look better.

	GlobalVolume: Global volume override for all audio.

	ShowFPS: Draw an FPS indicator ingame

	ShowConsole: Show the debug console. Do this if bad things are happening.


2) In game
	1) The game begins with a prompt. This prompt is asking for your steam community URL, but it accepts multiple forms of information. 
		Acceptable formats include:
			foohy
			http://steamcommunity.com/id/foohy/
			http://steamcommunity.com/profiles/76561197997689747
			76561197997689747
		Note that your profile MUST BE SET TO PUBLIC in order for this to work.
	
	2) The game will scrape a bunch of game information upon first load. This saves to <workingdirectory>/Saves/COMMUNITYID USERNAME. This prevents having to load the file again and makes it loadable without an internet connection.


3) I'm not responsible if it breaks your everything
	I was nowhere near isle 7
