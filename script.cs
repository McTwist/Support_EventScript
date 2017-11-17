// ======================
// EventScript
// Author: McTwist (9845)
// Version: 1.1.20171117
// ======================
// EventScript is a script language designed for an easier conversion
// between the in-game event system and a text version that can be used
// in a text editor.

// Create list from script
// Requires a script which will be parsed into an object
// Returns an object containing information about the script
function EventScript_fromScript(%script, %error)
{
	if (!isFunction(%error))
		%error = error;

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
		// Skip white spaces
		if (strpos(" \t\n", %char) >= 0)
		{
			// Increase line
			if (%char $= "\n")
				%line++;
			continue;
		}

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

				// Update line found
				if (%list.value[%list.count, "line"] $= "")
					%list.value[%list.count, "line"] = %line;

				%enabled = "";
				%readChars = false;
				%lastSpace = false;

				// Verify characters
				for (%n = %i + 1; %n < %len; %n++)
				{
					%c = getSubStr(%script, %n, 1);

					if (strpos(" \t\n", %c) >= 0)
					{
						if (%readChars)
							%lastSpace = true;
						if (%c $= "\n")
							%line++;
					}
					else if (%c $= "]")
					{
						break;
					}
					else if (%readChars && %lastSpace)
					{
						%n = -1;
						break;
					}
					else if (strpos("1234567890", %c) < 0)
					{
						if (strlwr(%c) !$= "x" || %enabled !$= "" || %readChars)
						{
							%n = -1;
							break;
						}
						// Only allow one box with enabled
						else if (%list.value[%list.count, "enabled"] !$= "")
						{
							%n = -2;
							break;
						}
						else
						{
							%enabled = true;
						}
					}
					else if (%enabled !$= "")
					{
						%n = -1;
						break;
					}
					// Only allow one box with delay
					else if (%list.value[%list.count, "delay"] !$= "")
					{
						%n = -3;
						break;
					}
					else
					{
						%readChars = true;
					}
				}

				// Found errors
				if (%n < 0)
				{
					if (%n == -1)
						call(%error, "Parse Error: Found illegal character " @ %c @ " on line " @ %line);
					else if (%n == -2)
						call(%error, "Parse Error: Only one enabled allowed per event on line " @ %line);
					else if (%n == -3)
						call(%error, "Parse Error: Only one delay allowed per event on line " @ %line);
					%list.error = true;
					return %list;
				}
				else if (%n >= %len)
				{
					call(%error, "Parse Error: Missing ending delimiter ] on line " @ %line);
					%list.error = true;
					return %list;
				}

				// Get actual values
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

				%i = %n;

			// Input event
			default:

				%startLine = %line;
				%readChars = false;
				%lastSpace = false;

				// Verify characters
				for (%n = %i + 1; %n < %len; %n++)
				{
					%c = getSubStr(%script, %n, 1);

					if (strpos(" \t\n", %c) >= 0)
					{
						if (%readChars)
							%lastSpace = true;
						if (%c $= "\n")
							%line++;
					}
					else if (%c $= "-" && getSubStr(%script, %n, 2) $= "->")
					{
						break;
					}
					else if (strpos("1234567890abcdefghijklmnopqrstuvwxyz_", strlwr(%c)) < 0)
					{
						%n = -1;
						break;
					}
					else if (%readChars && %lastSpace)
					{
						%n = -2;
						break;
					}
					else
					{
						%readChars = true;
					}
				}

				// Found errors
				if (%n < 0)
				{
					if (%n == -1)
						call(%error, "Parse Error: Found illegal character " @ %c @ " on line " @ %line);
					else if (%n == -2)
						call(%error, "Parse Error: Input event containing spaces on line " @ %line);
					%list.error = true;
					return %list;
				}
				else if (%n >= %len)
				{
					call(%error, "Parse Error: Missing input operator -> on line " @ %line);
					%list.error = true;
					return %list;
				}

				// Update line found
				if (%list.value[%list.count, "line"] $= "")
					%list.value[%list.count, "line"] = %startLine;

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

				// Locate end of string
				%escape = false;
				for (%n = %i + 1; %n < %len; %n++)
				{
					%c = getSubStr(%script, %n, 1);

					if (%c $= "\n")
					{
						%line++;
						%n = -1;
						break;
					}
					else if (%escape)
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

				// Found errors
				if (%n < 0)
				{
					call(%error, "Parse Error: Newline in strings not supported on line " @ %line);
					%list.error = true;
					return %list;
				}
				else if (%n >= %len)
				{
					call(%error, "Parse Error: Missing ending delimiter \" on line " @ %line);
					%list.error = true;
					return %list;
				}

				%data = getSubStr(%script, %i+1, %n - (%i+1));
				%data = strReplace(%data, "\\\"", "\"");
				%data = strReplace(%data, "\\\\", "\\");
				%list.value[%list.count, "NTName"] = %data;

				%i = %n;

				for (%n = %i + 1; %n < %len; %n++)
				{
					%c = getSubStr(%script, %n, 1);

					if (strpos(" \t\n", %c) >= 0)
					{
						if (%c $= "\n")
							%line++;
					}
					else if (%c $= "-" && getSubStr(%script, %n, 2) $= "->")
					{
						break;
					}
					else
					{
						%n = -1;
						break;
					}
				}

				// Found errors
				if (%n < 0)
				{
					call(%error, "Parse Error: Found illegal character " @ %c @ " on line " @ %line);
					%list.error = true;
					return %list;
				}
				else if (%n >= %len)
				{
					call(%error, "Parse Error: Missing target operator -> on line " @ %line);
					%list.error = true;
					return %list;
				}

				%i = %n + 1;
				%state = 2;

			// Default target
			default:

				%readChars = false;
				%lastSpace = false;

				// Verify characters
				for (%n = %i + 1; %n < %len; %n++)
				{
					%c = getSubStr(%script, %n, 1);

					if (strpos(" \t\n", %c) >= 0)
					{
						if (%readChars)
							%lastSpace = true;
						if (%c $= "\n")
							%line++;
					}
					else if (%c $= "-" && getSubStr(%script, %n, 2) $= "->")
					{
						break;
					}
					else if (strpos("1234567890abcdefghijklmnopqrstuvwxyz_", strlwr(%c)) < 0)
					{
						%n = -1;
						break;
					}
					else if (%readChars && %lastSpace)
					{
						%n = -2;
						break;
					}
					else
					{
						%readChars = true;
					}
				}

				// Found errors
				if (%n < 0)
				{
					if (%n == -1)
						call(%error, "Parse Error: Found illegal character " @ %c @ " on line " @ %line);
					else if (%n == -2)
						call(%error, "Parse Error: Target name containing spaces on line " @ %line);
					%list.error = true;
					return %list;
				}
				else if (%n >= %len)
				{
					call(%error, "Parse Error: Missing target operator -> on line " @ %line);
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

			%readChars = false;
			%lastSpace = false;

			// Verify characters
			for (%n = %i + 1; %n < %len; %n++)
			{
				%c = getSubStr(%script, %n, 1);

				if (strpos(" \t\n", %c) >= 0)
				{
					if (%readChars)
						%lastSpace = true;
					if (%c $= "\n")
					{
						%line++;
						if (%readChars)
							break;
					}
				}
				else if (%c $= "(")
				{
					break;
				}
				else if (strpos("1234567890abcdefghijklmnopqrstuvwxyz_", strlwr(%c)) < 0)
				{
					%n = -1;
					break;
				}
				else if (%readChars && %lastSpace)
				{
					%n = -2;
					break;
				}
				else
				{
					%readChars = true;
				}
			}

			// Found errors
			if (%n < 0)
			{
				if (%n == -1)
					call(%error, "Parse Error: Found illegal character " @ %c @ " on line " @ %line);
				else if (%n == -2)
					call(%error, "Parse Error: Output event containing spaces on line " @ %line);
				%list.error = true;
				return %list;
			}

			%data = trim(getSubStr(%script, %i, %n - %i));
			%list.value[%list.count, "outputEventName"] = %data;

			%i = %n;

			// Change state
			if (%n >= %len || %c $= "\n")
			{
				%state = 0;
				// Finished event
				%list.count++;
			}
			else if (%c $= "(")
			{
				%state = 3;
			}

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

					if (%c $= "\n")
					{
						%line++;
						%n = -1;
						break;
					}
					else if (%escape)
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

				// Found errors
				if (%n < 0)
				{
					call(%error, "Parse Error: Newline in strings not supported on line " @ %line);
					%list.error = true;
					return %list;
				}
				else if (%n >= %len)
				{
					call(%error, "Parse Error: Missing ending delimiter \" on line " @ %line);
					%list.error = true;
					return %list;
				}

				%data = getSubStr(%script, %i+1, %n - (%i+1));
				%data = strReplace(%data, "\\\"", "\"");
				%data = strReplace(%data, "\\\\", "\\");

				// Append list (Dependency)
				%list.value[%list.count, "params"] = pushBack(%list.value[%list.count, "params"], %data, "\t");

				%i = %n;

				for (%n = %i + 1; %n < %len; %n++)
				{
					%c = getSubStr(%script, %n, 1);

					if (strpos(" \t\n", %c) >= 0)
					{
						if (%c $= "\n")
							%line++;
					}
					else if (%c $= "," || %c $= ")")
					{
						break;
					}
					else
					{
						%n = -1;
						break;
					}
				}

				// Found errors
				if (%n < 0)
				{
					call(%error, "Parse Error: Found illegal character " @ %c @ " on line " @ %line);
					%list.error = true;
					return %list;
				}
				else if (%n >= %len)
				{
					call(%error, "Parse Error: Missing parameter ending delimiter ) on line " @ %line);
					%list.error = true;
					return %list;
				}

				%i = %n;

				// Change state
				if (%c $= ")")
				{
					%state = 0;
					// Finished events
					%list.count++;
				}

			// Number / Boolean / Naked string
			default:

				%readChars = false;
				%lastSpace = false;

				// Verify characters
				for (%n = %i + 1; %n < %len; %n++)
				{
					%c = getSubStr(%script, %n, 1);

					if (strpos(" \t\n", %c) >= 0)
					{
						if (%c $= "\n")
						{
							%line++;
							if (%readChars)
								%lastSpace = true;
						}
					}
					else if (%c $= "," || %c $= ")")
					{
						break;
					}
					else if (strpos("1234567890abcdefghijklmnopqrstuvwxyz_-+.", strlwr(%c)) < 0)
					{
						%n = -1;
						break;
					}
					else if (%readChars && %lastSpace)
					{
						%n = -2;
						break;
					}
					else
					{
						%readChars = true;
					}
				}

				// Found errors
				if (%n < 0)
				{
					if (%n == -1)
						call(%error, "Parse Error: Found illegal character " @ %c @ " on line " @ %line);
					else if (%n == -2)
						call(%error, "Parse Error: Parameter containing newline on line " @ %line);
					%list.error = true;
					return %list;
				}
				else if (%n >= %len)
				{
					call(%error, "Parse Error: Missing parameter ending delimiter ) on line " @ %line);
					%list.error = true;
					return %list;
				}

				%data = trim(getSubStr(%script, %i, %n - %i));

				// Append list (Dependency)
				%list.value[%list.count, "params"] = pushBack(%list.value[%list.count, "params"], %data, "\t");

				%i = %n;

				// Change state
				if (%c $= ")")
				{
					%state = 0;
					// Finished events
					%list.count++;
				}
			}
		}
	}

	// Handle ending errors
	if (%state != 0
		|| %list.value[%list.count, "enabled"] !$= ""
		|| %list.value[%list.count, "delay"] !$= "")
	{
		call(%error, "Parse Error: Unfinished event on line " @ %line);
		%list.error = true;
	}

	return %list;
}

// Create script from list
// Requires an object containing a list of information of how to build the script
// Returns a script
// Return script is a redommendation of style standard
function EventScript_toScript(%list)
{
	// Set newline
	%nl = isWindows() ? "\r\n" : "\n";

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
		%script = %script @ %nl;
	}

	return %script;
}
