//
// EventScript Profiles
// Author: McTwist (9845)
// Date: 2017-11-16
//
// The profiles used for the GUI.

new GUIControlProfile(EventScriptEditor_ScrollProfile : GuiScrollProfile)
{
	hasBitmapArray = true;
	bitmap = "base/client/ui/blockScroll";
	border = true;
	borderColor   = "86 86 86 255";
	borderColorHL = "86 86 86 255";
	borderColorNA = "86 86 86 255";
	borderThickness = 2;
	//cursorColor  = "248 248 248 255";
	//fillColor   = "34 34 34 255";
	//fillColorHL = "34 34 34 255";
	//fillColorNA = "34 34 34 255";
	modal = true;
	tab = 0;
	textOffset = "5 5";
};

new GUIControlProfile(EventScriptEditor_TextProfile : GuiMLTextEditProfile)
{
	hasBitmapArray = false;
	border = false;
	//fontColor   = "248 248 248 255";
	//fontColorHL = "248 248 248 255";
	//fontColorNA = "248 248 248 255";
	//fontColorSEL = "68 68 68 255";
	//fontSize = "14";
	//fontType = "Courier New"; // Lucida Console / Monaco / monospace
	justify = "left";
	modal = true;
	tab = 0;
	textOffset = "5 5";
};
