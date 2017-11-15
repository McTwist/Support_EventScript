//
// EventScript Client
// Author: McTwist (9845)
// Date: 2017-11-14
//
// Contains the client version of transforming an EventScript list object
// into either readable script or usable events.


// Load needed script file
exec("./script.cs");
exec("./bind.cs");

// Declare keybinds
CreateBind("EventScript", "Copy", EventScriptClient_copy);
CreateBind("EventScript", "Paste", EventScriptClient_paste);

// Save events to a script string
function EventScriptClient_save()
{
	if (!isObject(WrenchEvents_Box))
		return;
	if (!WrenchEventsDlg.isAwake())
		return;

	%list = new ScriptObject();
	%list.count = 0;

	%count = WrenchEvents_Box.getCount();
	for (%i = 0; %i < %count; %i++)
	{
		%event = WrenchEvents_Box.getObject(%i);
		%n = -1;

		// Enabled
		%enabled = %event.getObject(%n++).getValue();
		// Delay
		%delay = %event.getObject(%n++).getValue();
		// Input event
		%inputEventIdx = %event.getObject(%n++).getSelected();
		%inputEventName = %event.getObject(%n).getValue();

		// No input event
		if (%inputEventIdx $= -1 || %event.getCount() <= 3)
			continue;

		// Target idx
		%targetIdx = %event.getObject(%n++).getSelected();
		%targetName = "";
		%NTName = "";
		if (%targetIdx $= -1)
		{
			// Named target
			%NTName = %event.getObject(%n++).getValue();
			%NTNameIdx = %event.getObject(%n).getSelected();
			// No target
			if (%NTNameIdx $= -1)
				continue;
		}
		else
		{
			// Normal target
			%targetName = %event.getObject(%n).getValue();
		}

		// Output event
		%outputEventName = %event.getObject(%n++).getValue();

		%params = "";

		// Parameters
		for (%n++; %n < %event.getCount(); %n++)
		{
			%param = %event.getObject(%n);
			switch$ (%param.getClassName())
			{
			// Special cases
			case "GuiSwatchCtrl":
				// Color
				if (%param.getCount() == 1)
				{
					%val = %param.getColor();
				}
				// Vector
				else if (%param.getCount() == 3)
				{
					%val =  atof(%param.getObject(0).getValue())
						SPC atof(%param.getObject(1).getValue())
						SPC atof(%param.getObject(2).getValue());
				}
			// The rest
			default:
				%val = %param.getValue();
			}
			%params = (%params !$= "") ? %params TAB %val : %val;
		}

		// Store into list
		%list.value[%i, "enabled"] = %enabled;
		%list.value[%i, "delay"] = %delay;
		%list.value[%i, "inputEventName"] = %inputEventName;
		%list.value[%i, "targetName"] = %targetName;
		%list.value[%i, "NTName"] = %NTName;
		%list.value[%i, "outputEventName"] = %outputEventName;
		%list.value[%i, "params"] = %params;
		%list.count++;
	}

	%script = EventScript_toScript(%list);
	%list.delete();

	return %script;
}

// Load events from a script string
function EventScriptClient_load(%script)
{
	if (!isObject(WrenchEventsDlg) || !isObject(WrenchEvents_Box))
		return;
	if (!WrenchEventsDlg.isAwake())
		return;

	%list = EventScript_fromScript(%script, EventScriptClient_error);

	// Handle errors
	if (%list.error)
	{
		%list.delete();
		return;
	}

	%warnings = "";

	// Clear previous ones
	WrenchEventsDlg.clear();

	for (%i = 0; %i < %list.count; %i++)
	{
		%line = %list.value[%i, "line"];
		%enabled = %list.value[%i, "enabled"];
		%delay = %list.value[%i, "delay"];
		%inputEventName = %list.value[%i, "inputEventName"];
		%targetName = %list.value[%i, "targetName"];
		%NTName = %list.value[%i, "NTName"];
		%outputEventName = %list.value[%i, "outputEventName"];
		%params = %list.value[%i, "params"];

		%event = WrenchEvents_Box.getObject(WrenchEvents_Box.getCount() - 1);
		%n = -1;

		$WrenchEventLoading = 1;
		// Enabled
		%event.getObject(%n++).setValue(%enabled);

		// Delay
		%event.getObject(%n++).setValue(%delay);

		// Input event index
		%inputEventIdx = %event.getObject(%n++).findText(%inputEventName);
		// Invalid
		if (%inputEventIdx $= -1)
		{
			EventScriptClient_error("Error :: Invalid input event \"" @ %inputEventName @"\" on line " @ %line);
			%event.getObject(%n).setSelected(-1);
			$WrenchEventLoading = 0;
			wrenchEventsDlg.newEvent();
			return;
		}

		// Input event
		%event.getObject(%n).setSelected(%inputEventIdx);

		// Target index
		%n++;
		%targetIdx = %targetName $= "" ? -1 : %event.getObject(%n).findText(%targetName);

		// Special named targets rule
		if (ServerConnection.allowNamedTargets)
		{
			// Target
			%event.getObject(%n).setSelected(%targetIdx);
			if (%targetIdx $= -1)
			{
				// Target name index
				%NTIdx = %event.getObject(%n++).findText(%NTName);
				// Invalid
				if (%NTIdx $= -1)
				{
					EventScriptClient_error("Error :: Invalid brick name \"" @ %NTName @ "\" on line " @ %line);
					$WrenchEventLoading = 0;
					return;
				}
				// Target name
				%event.getObject(%n).setSelected(%NTIdx);
			}
		}
		else
		{
			// Target name
			if (%targetIdx $= -1)
			{
				%n++;
				%event.getObject(%n).add("<NAMED BRICK>", -1);
				%event.getObject(%n).setActive(0);
				%event.getObject(%n).setSelected(%targetIdx);

				%event.getObject(%n++).setValue(%NTIdx);
				%event.getObject(%n).setActive(0);
				WrenchEventsDlg.createOutputlist(%event, %event.getObject(%n), %inputEventIdx, fxDTSBrick);
			}
			else
			{
				%event.getObject(%n).setSelected(%targetIdx);
			}
		}

		// Output event index
		%outputEventIdx = %event.getObject(%n++).findText(%outputEventName);

		if (%outputEventIdx $= -1)
		{
			EventScriptClient_error("Error :: Invalid output event \"" @ %outputEventName @ "\" on line " @ %line);
			$WrenchEventLoading = 0;
			return;
		}

		// Output event name
		%event.getObject(%n).setSelected(%outputEventIdx);
		%n++;

		if (getFieldCount(%params) != (%event.getCount() - %n))
		{
			EventScriptClient_error("Error :: Invalid amount of parameters for output event \"" @ %outputEventName @ "\" on line " @ %line);
			$WrenchEventLoading = 0;
			return;
		}

		// Parameters
		for (%m = -1; %n < %event.getCount(); %n++)
		{
			%param = %event.getObject(%n);

			%par = getField(%params, %m++);

			switch$ (%param.getClassName())
			{
			// Special cases
			case "GuiSwatchCtrl":
				// Color
				if (%param.getCount() == 1)
				{
					// Locate the closest color
					%dist = 2;
					%closest = 0;
					for (%l = 0; %l < 64; %l++)
					{
						%col = getColorIDTable(%l);
						// abs(RGB - RGB) + (A - A)^2
						%d = vectorDist(getWords(%par, 0, 3), getWords(%col, 0, 3))
							+ mPow(getWord(%par, 3) - getWord(%col, 3), 2);
						if (%d < %dist)
						{
							%dist = %d;
							%closest = %l;
							// Closest as we can get
							if (%dist == 0)
								break;
						}
					}
					if (%dist > 0)
						%warnings = (%warnings !$= "" ? %warnings @ "\n" : "") @ ("Warning :: Colorset does not contain the desired color on line " @ %line);
					wrenchEventsDlg.pickColor(%param, %closest);
				}
				// Vector
				else if (%param.getCount() == 3)
				{
					for (%l = 0; %l < 3; %l++)
						%param.getObject(%l).setText(atof(getWord(%par, %l)));
				}
			// List
			case "GuiPopUpMenuCtrl":
				%parIdx = %param.findText(%par);
				if (%parIdx $= -1)
					%warnings = (%warnings !$= "" ? %warnings @ "\n" : "") @ ("Warning :: Invalid output parameter \"" @ %par @ "\" on line " @ %line);
				%param.setSelected(%parIdx);
			// The rest
			default:
				%param.setvalue(%par);
			}
		}
		$WrenchEventLoading = 0;
	}

	%list.delete();

	if (%warnings !$= "")
		EventScriptClient_error(%warnings);
}

// Sets the clipboard the result of saving the events to a script
function EventScriptClient_copy(%down)
{
	if (%down)
	{
		%script = EventScriptClient_save();
		setClipboard(%script);
	}
}

// Gets the clipboard for the script and transforms it into usable events
function EventScriptClient_paste(%down)
{
	if (%down)
	{
		%script = getClipboard();
		EventScriptClient_load(%script);
	}
}

// Display information about an error that occured
function EventScriptClient_error(%error)
{
	MessageBoxOK("EventScript", %error);
}

