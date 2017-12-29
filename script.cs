// ======================
// EventScript
// Author: McTwist (9845)
// Version: 1.3.20171119
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

	%indexTableCount = 0;

	%state = 0;
	%line = 1;

	// PASS 1
	// Read through the string and add everything to the list

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
					else if (%c $= ":"
						&& %list.value[%list.count, "enabled"] $= ""
						&& %list.value[%list.count, "delay"] $= "")
					{
						break;
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
					else if (!%readChars && strpos("1234567890", strlwr(%c)) >= 0)
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
						call(%error, "Parse Error: Input event containing spaces on line " @ %line);
					else if (%n == -3)
						call(%error, "Parse Error: Input event starting with numbers on line " @ %line);
					%list.error = true;
					return %list;
				}
				else if (%n >= %len)
				{
					call(%error, "Parse Error: Missing input operator -> on line " @ %line);
					%list.error = true;
					return %list;
				}

				%data = trim(getSubStr(%script, %i, %n - %i));

				// Label
				if (%c $= ":")
				{
					if (%labelTable[%data] !$= "")
					{
						call(%error, "Parse Error: Duplicate label \"" @ %data @ "\" found on line " @ %line
							@ ", previous found at line " @ %list.value[%labelTable[%data], "line"]);
						%list.error = true;
						return %list;
					}

					%labelTable[%data] = %list.count;
					%i = %n;
				}
				// Input event
				else
				{
					// Update line found
					if (%list.value[%list.count, "line"] $= "")
						%list.value[%list.count, "line"] = %startLine;

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
					else if (strpos("1234567890abcdefghijklmnopqrstuvwxyz_()-", strlwr(%c)) < 0)
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
						%line++;
				}
				else if (%c $= "(" || (%readChars && %lastSpace))
				{
					break;
				}
				else if (strpos("1234567890abcdefghijklmnopqrstuvwxyz_", strlwr(%c)) < 0)
				{
					%n = -1;
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
				call(%error, "Parse Error: Found illegal character " @ %c @ " on line " @ %line);
				%list.error = true;
				return %list;
			}

			%data = trim(getSubStr(%script, %i, %n - %i));
			%list.value[%list.count, "outputEventName"] = %data;

			%i = %n;

			// Change state
			if (%n >= %len || %c !$= "(")
			{
				// Move back to avoid missing character
				if (%n < %len)
					%i--;
				%state = 0;
				// Finished event
				%list.count++;
			}
			else
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

			// Indexing
			case "[":

				// Set default values for table element
				%indexTableLine[%indexTableCount] = %line;
				%indexTableIndex[%indexTableCount] = %list.count;
				%indexTableParam[%indexTableCount] = getFieldCount(%list.value[%list.count, "param"]);
				%indexTableListCount[%indexTableCount] = 0;

				%state = 4;

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

		// Indexing
		case 4:

			%readChars = false;
			%lastSpace = false;
			%isLabel = false;

			// Verify characters
			for (%n = %i; %n < %len; %n++)
			{
				%c = getSubStr(%script, %n, 1);

				if (strpos(" \t\n", %c) >= 0)
				{
					if (%readChars)
						%lastSpace = true;
					if (%c $= "\n")
						%line++;
				}
				else if (%c $= "]" || %c $= ":" || %c $= ",")
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
					// Need at least one alpha character to be a proper label
					if (strpos("1234567890", strlwr(%c)) < 0)
						%isLabel = true;
					%readChars = true;
				}
			}

			// Found errors
			if (%n < 0)
			{
				if (%n == -1)
					call(%error, "Parse Error: Found illegal character " @ %c @ " on line " @ %line);
				else if (%n == -2)
					call(%error, "Parse Error: Index cannot contain whitespace on line " @ %line);
				%list.error = true;
				return %list;
			}
			else if (%n >= %len)
			{
				call(%error, "Parse Error: Missing index ending delimiter ] on line " @ %line);
				%list.error = true;
				return %list;
			}

			%data = trim(getSubStr(%script, %i, %n - %i));

			%currentListCount = %indexTableListCount[%indexTableCount];
			%indexTableListLabel[%indexTableCount, %currentListCount] = %isLabel;
			%indexTableList[%indexTableCount, %currentListCount] = %data;
			%indexTableListCount[%indexTableCount]++;

			%i = %n;

			if (%c $= ",")
			{
				// Empty index
				if (%data $= ""
					&& (%indexTableListCount[%indexTableCount] == 0
						|| %indexTableType[%indexTableCount, %currentListCount - 1] == 0))
				{
					%n = -2;
				}
				else
				{
					%indexTableType[%indexTableCount, %currentListCount] = 0;
				}

				%currentListCount++;
			}
			else if (%c $= ":")
			{
				// Already got a range
				if (%currentListCount > 0
					&& %indexTableType[%indexTableCount, %currentListCount - 1] == 1)
				{
					%n = -3;
				}
				else
				{
					%indexTableType[%indexTableCount, %currentListCount] = 1;
				}

				%currentListCount++;
			}
			else if (%c $= "]")
			{
				// Empty index
				if (%data $= ""
					&& (%currentListCount == 0
						|| %indexTableType[%indexTableCount, %currentListCount - 1] == 0))
				{
					%n = -2;
				}
				else
				{
					%indexTableType[%indexTableCount, %currentListCount] = 0;

					// Finished, so lets find next and get out of here
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

					if (%c $= ",")
					{
						%state = 3;
					}
					else if (%c $= ")")
					{
						%state = 0;
						// Finished events
						%list.count++;
					}

					%indexTableCount++;
				}
			}

			// Found errors
			if (%n < 0)
			{
				if (%n == -1)
					call(%error, "Parse Error: Found illegal character " @ %c @ " on line " @ %line);
				else if (%n == -2)
					call(%error, "Parse Error: Empty index found on line " @ %line);
				else if (%n == -3)
					call(%error, "Parse Error: Ranges allow only two values on line " @ %line);
				%list.error = true;
				return %list;
			}
			else if (%n >= %len)
			{
				call(%error, "Parse Error: Missing index ending delimiter ] on line " @ %line);
				%list.error = true;
				return %list;
			}

			%i = %n;
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

	// PASS 2
	// Parse parameters individually

	for (%i = 0; %i < %indexTableCount; %i++)
	{
		%line = %indexTableLine[%i];
		%index = %indexTableIndex[%i];
		%param = %indexTableParam[%i];

		%params = "";

		%l = -1;
		for (%n = 0; %n < %indexTableListCount[%i]; %n++)
		{
			%var = %indexTableList[%i, %n];

			// Handle labels
			if (%indexTableListLabel[%i, %n])
			{
				if (%labelTable[%var] $= "")
				{
					call(%error, "Parser Error: Label \"" @ %var @ "\" does not exist on line " @ %line);
					%list.error = true;
					return %list;
				}

				%var = %labelTable[%var];
			}

			// Default value
			if (%indexTableType[%i, %n] == 2 && %var $= "")
				%var = %list.count - 1;
			else if (%var $= "")
				%var = 0;

			%var = mClamp(%var, 0, %list.count - 1);

			switch (%indexTableType[%i, %n])
			{
			// Indexing
			case 0:

				%params = setWord(%params, %l++, %var);

			// Range start
			case 1:

				%start = %var;
				%indexTableType[%i, %n+1] = 2;

			// Range ends
			case 2:

				// Incorrect indexing order
				if (%start > %var)
				{
					call(%error, "Logic Error: Invalid indexing values " @ %start @ ":" @ %var @ " on line " @ %line);
					%list.error = true;
					return %list;
				}

				for (%m = %start; %m <= %var; %m++)
					%params = setWord(%params, %l++, %m);
			}
		}

		%list.value[%index, "params"] = setField(%list.value[%index, "params"], %param, %params);
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
