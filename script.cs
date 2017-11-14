// ======================
// EventScript
// Author: McTwist (9845)
// Version: 1.0.20171114
// ======================
// EventScript is a script language designed for an easier conversion
// between the in-game event system and a text version that can be used
// in a text editor.

// Create list from script
// Requires a script which will be parsed into an object
// Returns an object containing information about the script
function EventScript_fromScript(%script)
{
	%list = new ScriptObject();
	%list.error = false;
	%list.count = 0;

	// Fix special character
	// Note: trim does not trim \r
	%script = strReplace(%script, "\r\n", "\n");
	%script = strReplace(%script, "\r", "\n");

	%len = strlen(%script);

	%state = 0;
	%line = 1;

	for (%i = 0; %i < %len; %i++)
	{
		%char = getSubStr(%script, %i, 1);
		// Increase line
		if (%char $= "\n")
			%line++;
		// Skip white spaces
		if (strpos(" \t\r\n", %char) >= 0)
			continue;

		switch (%state)
		{
		// Default
		case 0:
			switch$ (%char)
			{
			// Comment
			case "#":
				%n = strpos(%script, "\n", %i);
				%i = (%n < 0) ? %len : %n;
				%line++;
			// Enabled / Delay
			case "[":
				%n = strpos(%script, "]", %i);
				if (%n < 0)
				{
					error("Parse Error: Missing ending delimeter ] on line " @ %line);
					%list.error = true;
					return %list;
				}
				%data = trim(getSubStr(%script, %i+1, %n - (%i+1)));
				// Enabled
				if (strlwr(%data) $= "x")
				{
					%list.value[%list.count, "enabled"] = true;
				}
				// Disabled
				else if (%data $= "")
				{
					%list.value[%list.count, "enabled"] = false;
				}
				// Delay
				else
				{
					%list.value[%list.count, "delay"] = atoi(%data);
				}
				if (%list.value[%list.count, "line"] $= "")
					%list.value[%list.count, "line"] = %line;
				%i = %n;
			// Input event
			default:
				%n = strpos(%script, "->", %i);
				if (%n < 0)
				{
					error("Parse Error: Missing input delimeter -> on line " @ %line);
					%list.error = true;
					return %list;
				}
				%data = trim(getSubStr(%script, %i, %n - %i));
				%list.value[%list.count, "inputEventName"] = %data;
				%i = %n + 1;
				%state = 1;

				// Set default values
				if (%list.value[%list.count, "enabled"] $= "")
					%list.value[%list.count, "enabled"] = true;
				if (%list.value[%list.count, "delay"] $= "")
					%list.value[%list.count, "delay"] = 0;
				if (%list.value[%list.count, "line"] $= "")
					%list.value[%list.count, "line"] = %line;
			}
		// Target
		case 1:
			switch$ (%char)
			{
			// Named target
			case "\"":
				%n = strpos(%script, "\"", %i+1);
				if (%n < 0)
				{
					error("Parse Error: Missing ending delimeter \" on line " @ %line);
					%list.error = true;
					return %list;
				}
				%data = getSubStr(%script, %i+1, %n - (%i+1));
				%list.value[%list.count, "NTName"] = %data;

				%i = %n;

				%n = strpos(%script, "->", %i);
				if (%n < 0)
				{
					error("Parse Error: Missing target delimeter -> on line " @ %line);
					%list.error = true;
					return %list;
				}
				%i = %n + 1;
				%state = 2;
			// Default target
			default:
				%n = strpos(%script, "->", %i);
				if (%n < 0)
				{
					error("Parse Error: Missing target delimeter -> on line " @ %line);
					%list.error = true;
					return %list;
				}
				%data = trim(getSubStr(%script, %i, %n - %i));
				%list.value[%list.count, "targetName"] = %data;
				%i = %n + 1;
				%state = 2;
			}
		// Output
		case 2:
			%s = strpos(%script, "\n", %i);
			%n = strpos(%script, "(", %i);

			// Got both
			if (%s >= 0 && %n >= 0)
			{
				// Paranthesis is before newline
				if (%s > %n)
				{
					%state = 3;
				}
				// Newline is before paranthesis
				else
				{
					%n = %s;
					%state = 0;
				}
			}
			// Got paranthesis
			else if (%n >= 0)
			{
				%state = 3;
			}
			// Got none or newline
			else
			{
				%n = (%s >= 0) ? %s : %len;
				%state = 0;
			}
			%data = trim(getSubStr(%script, %i, %n - %i));
			%list.value[%list.count, "outputEventName"] = %data;

			%i = %n;
			// New events
			if (%state == 0)
				%list.count++;
		// Param
		case 3:
			switch$ (%char)
			{
			// End
			case ")":
				%list.count++;
				%state = 0;
			// String
			case "\"":
				// Locate end of string
				%escape = false;
				for (%n = %i + 1; %n < %len; %n++)
				{
					%c = getSubStr(%script, %n, 1);
					if (%escape)
					{
						%escape = false;
					}
					else if (%c $= "\\")
					{
						%escape = true;
					}
					else if (%c $= "\"")
					{
						break;
					}
				}

				%data = trim(getSubStr(%script, %i+1, %n - (%i+1)));
				%data = strReplace(%data, "\\\"", "\"");
				%data = strReplace(%data, "\\\\", "\\");

				%list.value[%list.count, "params"] =
					(%list.value[%list.count, "params"] !$= "")
					? %list.value[%list.count, "params"] TAB %data
					: %data;

				%i = %n;

				// Locate the end
				%s = strpos(%script, ",", %i);
				%n = strpos(%script, ")", %i);

				// Got both
				if (%s >= 0 && %n >= 0)
				{
					// Paranthesis is before comma
					if (%s > %n)
					{
						%state = 0;
					}
					// Comma is before paranthesis
					else
					{
						%n = %s;
					}
				}
				// Got paranthesis
				else if (%n >= 0)
				{
					%state = 0;
				}
				// Got comma
				else if (%s >= 0)
				{
					%n = %s;
				}
				// Got none
				else
				{
					error("Parse Error: Missing paranthesis delimeter ) on line " @ %line);
					%list.error = true;
					return %list;
				}
				%i = %n;
				// New events
				if (%state == 0)
					%list.count++;

			// Number / Boolean / Naked string
			default:
				%s = strpos(%script, ",", %i);
				%n = strpos(%script, ")", %i);

				// Got both
				if (%s >= 0 && %n >= 0)
				{
					// Paranthesis is before comma
					if (%s > %n)
					{
						%state = 0;
					}
					// Comma is before paranthesis
					else
					{
						%n = %s;
					}
				}
				// Got paranthesis
				else if (%n >= 0)
				{
					%state = 0;
				}
				// Got comma
				else if (%s >= 0)
				{
					%n = %s;
				}
				// Got none
				else
				{
					error("Parse Error: Missing paranthesis delimeter ) on line " @ %line);
					%list.error = true;
					return %list;
				}
				%data = trim(getSubStr(%script, %i, %n - %i));

				%list.value[%list.count, "params"] =
					(%list.value[%list.count, "params"] !$= "")
					? %list.value[%list.count, "params"] TAB %data
					: %data;

				%i = %n;
				// New events
				if (%state == 0)
					%list.count++;
			}
		}
	}

	return %list;
}

// Create script from list
// Requires an object containing a list of information of how to build the script
// Returns a script
// Return script is a redommendation of style standard
function EventScript_toScript(%list)
{
	%script = "";

	for (%i = 0; %i < %list.count; %i++)
	{
		%enabled = %list.value[%i, "enabled"];
		%delay = %list.value[%i, "delay"];
		%inputEventName = %list.value[%i, "inputEventName"];
		%targetName = %list.value[%i, "targetName"];
		%NTName = %list.value[%i, "NTName"];
		%outputEventName = %list.value[%i, "outputEventName"];
		%params = %list.value[%i, "params"];

		// Enabled
		%script = %script @ "[" @ (%enabled ? "x" : " ") @ "]";

		// Delay
		%script = %script @ "[" @ (%delay ? %delay : "0") @ "]";

		// Input event
		%script = %script SPC %inputEventName @ " -> ";

		// Target
		if (%targetName !$= "")
			%script = %script @ %targetName;
		// Target name
		else
			%script = %script @ "\"" @ %NTName @ "\"";

		// Output event
		%script = %script @ " -> " @ %outputEventName;


		// Parameters
		%count = getFieldCount(%params);
		if (%count > 0)
			%script = %script @ "(";

		for (%n = 0; %n < %count; %n++)
		{
			if (%n != 0)
				%script = %script @ ", ";

			%param = getField(%params, %n);

			// Check if number
			if ((%param + 0) $= %param)
				%script = %script @ %param;
			else
			{
				// Add escapes
				%param = strReplace(%param, "\"", "\\\"");
				%param = strReplace(%param, "\\", "\\\\");
				%script = %script @ "\"" @ %param @ "\"";
			}
		}

		// End params
		if (%count > 0)
			%script = %script @ ")";
		%script = %script @ "\n";
	}

	return %script;
}
