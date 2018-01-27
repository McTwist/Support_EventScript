// =================
// EventScript Client
// Author: McTwist (9845)
// Date: 2018-01-27
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

