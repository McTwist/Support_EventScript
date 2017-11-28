//
// GuiPopUpMenuCtrl
// Author: McTwist (9845)
// Date: 2017-11-28
//
// Replaces some functionality for GuiPopUpMenuCtrl with
// case-insensitive lookups.


package GuiPopUpMenuCtrlPackage
{
	// Add new text entry with id to popup menu controller
	function GuiPopUpMenuCtrl::add(%this, %entryText, %entryID, %entryScheme)
	{
		Parent::add(%this, %entryText, %entryID, %entryScheme);

		if (%this.table[%entryText] $= "")
		{
			%this.table[%entryText] = %entryID;
			%this.count += 0;
			%this.list[%this.count] = %entryText;
			%this.count++;
		}
	}

	// Find text in popup menu controller
	function GuiPopUpMenuCtrl::findText(%this, %text)
	{
		if (%this.table[%text] !$= "")
			return %this.table[%text];
		return Parent::findText(%this, %text);
	}

	// Clear popup menu controller
	function GuiPopUpMenuCtrl::clear(%this)
	{
		for (%i = 0; %i < %this.count; %i++)
		{
			%this.table[%this.list[%i]] = "";
			%this.list[%i] = "";
		}
		%this.count = "";
		Parent::clear(%this);
	}
};
activatePackage(GuiPopUpMenuCtrlPackage);
