// =================
// EventScript Client
// Author: McTwist (9845)
// Date: 2018-03-02
//
// Workaround fixes to solve issues regarding strange phenomena
// on either platform

// =================
// Mac keybind workaround
// Enabled for all platforms for convenience
// Creates buttons for copy, paste and editor
// =================
if (isObject(EventScriptWrenchButtons))
	EventScriptWrenchButtons.delete();

new GuiSwatchCtrl(EventScriptWrenchButtons) {
	profile = "GuiDefaultProfile";
	horizSizing = "left";
	vertSizing = "bottom";
	position = "700 3";
	extent = "61 19";
	minExtent = "8 2";
	enabled = "1";
	visible = "1";
	clipToParent = "1";
	color = "0 104 176 4";

	new GuiBitmapButtonCtrl() {
		profile = "BlockButtonProfile";
		horizSizing = "right";
		vertSizing = "bottom";
		position = "0 0";
		extent = "16 19";
		minExtent = "8 2";
		enabled = "1";
		visible = "1";
		clipToParent = "1";
		command = "EventScriptClient_copy(true);";
		text = "C";
		groupNum = "-1";
		buttonType = "PushButton";
		bitmap = "base/client/ui/button2";
		lockAspectRatio = "0";
		alignLeft = "0";
		alignTop = "0";
		overflowImage = "0";
		mKeepCached = "0";
		mColor = "255 255 255 255";
	};
	new GuiBitmapButtonCtrl() {
		profile = "BlockButtonProfile";
		horizSizing = "right";
		vertSizing = "bottom";
		position = "20 0";
		extent = "16 19";
		minExtent = "8 2";
		enabled = "1";
		visible = "1";
		clipToParent = "1";
		command = "EventScriptClient_paste(true);";
		text = "P";
		groupNum = "-1";
		buttonType = "PushButton";
		bitmap = "base/client/ui/button2";
		lockAspectRatio = "0";
		alignLeft = "0";
		alignTop = "0";
		overflowImage = "0";
		mKeepCached = "0";
		mColor = "255 255 255 255";
	};
	new GuiBitmapButtonCtrl() {
		profile = "BlockButtonProfile";
		horizSizing = "right";
		vertSizing = "bottom";
		position = "40 0";
		extent = "16 19";
		minExtent = "8 2";
		enabled = "1";
		visible = "1";
		clipToParent = "1";
		command = "EventScriptClient_openEditor(false);";
		text = "E";
		groupNum = "-1";
		buttonType = "PushButton";
		bitmap = "base/client/ui/button2";
		lockAspectRatio = "0";
		alignLeft = "0";
		alignTop = "0";
		overflowImage = "0";
		mKeepCached = "0";
		mColor = "255 255 255 255";
	};
};

WrenchEvents_Window.add(EventScriptWrenchButtons);

// =================
// Mac keybind workaround v2
// Force global key
// =================

package EventScriptPackage_Keybind
{
	// Event window is open
	function wrenchEventsDlg::onWake(%this)
	{
		Parent::onWake(%this);

		%map = GlobalActionMap.getId();

		// Get possible binding
		%copy = MoveMap.getbinding(EventScriptClient_copy);
		%paste = MoveMap.getbinding(EventScriptClient_paste);
		%editor = MoveMap.getbinding(EventScriptClient_openEditor);
		$EventScript::bind::copy = (%copy !$= "") ? %copy : (isWindows() ? "keyboard ctrl-shift c" : "keyboard cmd-shift c");
		$EventScript::bind::paste = (%paste !$= "") ? %paste : (isWindows() ? "keyboard ctrl-shift v" : "keyboard cmd-shift v");
		$EventScript::bind::editor = (%editor !$= "") ? %editor : (isWindows() ? "keyboard ctrl-shift e" : "keyboard cmd-shift e");

		GlobalActionMap.pushBind(firstWord($EventScript::bind::copy), restWords($EventScript::bind::copy), EventScriptClient_copy);
		GlobalActionMap.pushBind(firstWord($EventScript::bind::paste), restWords($EventScript::bind::paste), EventScriptClient_paste);
		GlobalActionMap.pushBind(firstWord($EventScript::bind::editor), restWords($EventScript::bind::editor), EventScriptClient_openEditor);
	}

	// Event window is closed
	function wrenchEventsDlg::onSleep(%this)
	{
		Parent::onSleep(%this);

		GlobalActionMap.popUnbind(firstWord($EventScript::bind::copy), restWords($EventScript::bind::copy));
		GlobalActionMap.popUnbind(firstWord($EventScript::bind::paste), restWords($EventScript::bind::paste));
		GlobalActionMap.popUnbind(firstWord($EventScript::bind::editor), restWords($EventScript::bind::editor));
	}
};
activatePackage(EventScriptPackage_Keybind);

// Push a new command to be used instead of current
function ActionMap::pushBind(%map, %device, %action, %command)
{
	if (%map.getCommand(%device, %action) !$= "")
	{
		%map.stackBind[%device, %action] |= 0;
		%map.stackBind[%device, %action, %map.stackBind[%device, %action]] = %map.getCommand(%device, %action);
		%map.stackBind[%device, %action]++;
	}

	%map.bind(%device, %action, %command);
}

// Pop current command in favor for previous one, if there is one
function ActionMap::popUnbind(%map, %device, %action)
{
	if (%map.stackBind[%device, %action] !$= "")
	{
		%map.stackBind[%device, %action]--;
		%map.bind(%device, %action, %map.stackBind[%device, %action, %map.stackBind[%device, %action]]);
		if (%map.stackBind[%device, %action] == 0)
			%map.stackBind[%device, %action] = "";
	}
}
