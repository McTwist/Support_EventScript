//
// EventScript Server
// Author: McTwist (9845)
// Date: 2017-11-14
//
// Contains the server version of transforming an EventScript list object
// into either readable script or usable events.


// Load needed script file
exec("./script.cs");

// Save current brick events and return a script
function EventScriptServer_save(%brick)
{
	if (!isObject(%brick))
		return;

	%list = new ScriptObject();
	%list.count = 0;

	for (%i = 0; %i < %brick.numEvents; %i++)
	{
		// Store variables
		%list.value[%i, "enabled"] = %brick.eventEnabled[%i];
		%list.value[%i, "delay"] = %brick.eventDelay[%i];
		%list.value[%i, "inputEventName"] = %brick.eventInput[%i];
		%list.value[%i, "outputEventName"] = %brick.eventOutput[%i];

		// Target
		if (%brick.eventTarget[%i] $= "-1")
		{
			%NTName = %brick.eventNT[%i];
			%list.value[%i, "NTName"] = getSubStr(%NTName, 1, strlen(%NTName));
			%targetClass = fxDTSBrick;
		}
		else
		{
			%list.value[%i, "targetName"] = %brick.eventTarget[%i];
			%targetClass = inputEvent_GetTargetClass(%brick.getClassName(), %brick.eventInputIdx[%i], %brick.eventTargetIdx[%i]);
		}

		// Parameters
		%paramCount = outputEvent_GetNumParametersFromIdx(%targetClass, %brick.eventOutputIdx[%i]);
		%paramList = $OutputEvent_parameterList[%targetClass, %brick.eventOutputIdx[%i]];

		%params = "";
		for (%n = 0; %n < %paramCount; %n++)
		{
			%paramField = getField(%paramList, %n);
			%param = %brick.eventOutputParameter[%i, %n+1];
			
			// Translate parameter
			switch$ (getWord(%paramField, 0))
			{
			case "int":
				%param = atoi(%param);
			case "intList":
				%param = atoi(getWord(%paramField, 1 + (%param * 2)));
			case "float":
				%param = atof(%param);
			case "bool":
				%param = !!%param;
			case "string":
				%param = %param;
			case "datablock":
				if (isObject(%param))
					%param = %param.getName();
				else
					%param = -1;
			case "vector":
				%param = %param;
			case "list":
				%param = getWord(%paramField, 1 + (%param * 2));
			case "paintColor":
				%param = getColorIDTable(%param);
			}

			%params = (%params !$= "") ? %params TAB %param : %param;
		}

		%list.value[%i, "params"] = %params;

		%list.count++;
	}

	%script = EventScript_toScript(%list);
	%list.delete();

	return %script;
}

// Give a script to a brick to produce events
function EventScriptServer_load(%brick, %script)
{
	if (!isObject(%brick))
		return;
	
	%list = EventScript_fromScript(%script);

	// Handle errors
	if (%list.error)
	{
		%list.delete();
		return;
	}

	%brick.clearEvents();

	%brick.numEvents = 0;

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

		// Enabled
		%brick.eventEnabled[%brick.numEvents] = !!%enabled;

		// Delay
		%brick.eventDelay[%brick.numEvents] = mClamp(%delay, 0, 30000);

		// Input event
		%inputEventIdx = inputEvent_GetInputEventIdx(%inputEventName);
		if (%inputEventIdx < 0)
		{
			error("EventScriptServer_load :: Invalid input name \"" @ %inputEventName @"\" on line " @ %line);
			return;
		}
		%brick.eventInput[%brick.numEvents] = %inputEventName;
		%brick.eventInputIdx[%brick.numEvents] = %inputEventIdx;

		// Target name
		if (%targetName $= "")
		{
			%targetName = "-1";
			%prev = %NTName;
			%NTName = "_" @ %NTName;
			if (!isObject(%NTName))
			{
				error("EventScriptServer_load :: Invalid target name \"" @ %prev @"\" on line " @ %line);
				return;
			}
			%prev = "";
		}
		else
		{
			%targetIdx = inputEvent_GetTargetIndex(fxDtsBrick, %inputEventIdx, %targetName);
			if (%targetIdx < 0)
			{
				error("EventScriptServer_load :: Invalid target name \"" @ %targetName @"\" on line " @ %line);
				return;
			}
		}

		%brick.eventTarget[%brick.numEvents] = %targetName;
		%brick.eventTargetIdx[%brick.numEvents] = %targetIdx;
		%brick.eventNT[%brick.numEvents] = %NTName;

		// Get target class
		%targetClass = %targetIdx >= 0 ? inputEvent_GetTargetClass(%brick.getClassName(), %inputEventIdx, %targetIdx) : fxDTSBrick;

		// Output event
		%outputEventIdx = outputEvent_GetOutputEventIdx(%targetClass, %outputEventName);
		if (%outputEventIdx < 0)
		{
			error("EventScriptServer_load :: Invalid output name \"" @ %outputEventName @"\" on line " @ %line);
			return;
		}
		%brick.eventOutput[%brick.numEvents] = %outputEventName;
		%brick.eventOutputIdx[%brick.numEvents] = %outputEventIdx;

		// Parameters
		%paramCount = outputEvent_GetNumParametersFromIdx(%targetClass, %outputEventIdx);
		%paramList = $OutputEvent_parameterList[%targetClass, %outputEventIdx];
		%count = getFieldCount(%params);
		if (%paramCount != %count)
		{
			error("EventScriptServer_load :: Invalid amount of parameters for \"" @ %outputEventName @"\" on line " @ %line);
			return;
		}

		// TODO: Verify these params accordingly
		for (%n = 0; %n <= %count; %n++)
		{
			%paramField = getField(%paramList, %n);
			%param = getField(%params, %n);
			
			// Translate parameter
			switch$ (getWord(%paramField, 0))
			{
			case "int":
				%param = atoi(%param);
			case "intList":
				%count = getWordcount(%paramField);
				for (%m = 0; %m < %count; %m++)
				{
					if (%param $= getWord(%paramField, 1 + (%m * 2)))
					{
						%param = getField(%paramField, 2 + (%m * 2));
						break;
					}
				}
				%param = atoi(getWord(%paramField, 1 + (%param * 2)));
			case "float":
				%param = atof(%param);
			case "bool":
				%param = !!%param;
			case "string":
				%param = %param;
			case "datablock":
				if (isObject(%param))
					%param = %param.getID();
				else
					%param = -1;
			case "vector":
				%param = %param;
			case "list":
				%count = getWordcount(%paramField);
				for (%m = 0; %m < %count; %m++)
				{
					if (stricmp(%param, getWord(%paramField, 1 + (%m * 2))) == 0)
					{
						%param = getField(%paramField, 2 + (%m * 2));
						break;
					}
				}
			case "paintColor":
				// Locate the closest color
				%dist = 2;
				%closest = 0;
				%par = %param;
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
					warn("EventScriptServer_load :: Colorset does not contain the desired color on line " @ %line);
				%param = %closest;
			}
			// Store it
			%brick.eventOutputParameter[%brick.numEvents, %n+1] = %param;
		}

		// Append client
		%brick.eventOutputAppendClient[%brick.numEvents] = $OutputEvent_AppendClient[%targetClass, %outputEventIdx];

		%brick.numEvents++;
	}

	%list.delete();
}

