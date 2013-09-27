// @func SimObject::addChildLayer(scope)
// @return void
// @desc
//   Adds a new namespace (x::) to the list of child layers on the object.
//   If the namespace is already a child layer, it is pushed to the top.

function SimObject::addChildLayer(%this, %scope)
{
	// Memory optimization: Don't set the counter on objects when they are created,
	// only set it when an object actually needs to use child layers.

	if (%this.childLayerCount $= "")
	{
		%this.childLayerCount = 0;
	}

	// Not using ::hasChildLayer here due to needing access to %i for optimization.

	for (%i = 0; %i < %this.childLayerCount; %i++)
	{
		if (%this.childLayerScope[%i] $= %scope)
		{
			%this.removeChildLayer(%scope, %i);
			break;
		}
	}

	%this.childLayerScope[%this.childLayerCount] = %scope;
	%this.childLayerCount++;
}

// @func SimObject::removeChildLayer(scope)
// @return void
// @desc
//   Removes a namespace (x::) from the list of child layers on the object.
//   If the namespace is not a child layer, no errors occur.

function SimObject::removeChildLayer(%this, %scope, %i)
{
	// Simple O(n) list removal, does not remove multiple occurences.
	// Properly removes trailing items.

	if (%i $= "")
	{
		%i = 0;
	}

	for (%i; %i < %this.childLayerCount; %i++)
	{
		if (%r)
		{
			%this.childLayerScope[%i] = %this.childLayerScope[%i + 1];
		}
		else if (%this.childLayerScope[%i] $= %scope)
		{
			%r = 1;
			%i--;

			continue;
		}
	}

	if (%r)
	{
		%this.childLayerCount--;
		%this.childLayerScope[%this.childLayerCount] = "";
	}
}

// @func SimObject::hasChildLayer(scope)
// @return bool
// @desc
//   Checks whether or not the object has the given scope as a child layer.

function SimObject::hasChildLayer(%this, %scope)
{
	for (%i = 0; %i < %this.childLayerCount; %i++)
	{
		if (%this.childLayerScope[%i] $= %scope)
		{
			return 1;
		}
	}

	return 0;
}

// @func SimObject::callChildLayer(method[, *args:40])
// @return @any
// @desc
//   Calls the given method on every child layer consecutively from the top.
//   Returns the first return value that is not "".

function SimObject::callChildLayer(%this, %method,
	%a0,  %a1,  %a2,  %a3,  %a4,  %a5,  %a6,  %a7,  %a8,  %a9,
	%a10, %a11, %a12, %a13, %a14, %a15, %a16, %a17, %a18, %a19,
	%a20, %a21, %a22, %a23, %a24, %a25, %a26, %a27, %a28, %a29,
	%a30, %a31, %a32, %a33, %a34, %a35, %a36, %a37, %a38, %a39)
{
	%args = "(%this,%a0,%a1,%a2,%a3,%a4,%a5,%a6,%a7,%a8,%a9,";
	%args = %args @ "%a10,%a11,%a12,%a13,%a14,%a15,%a16,%a17,%a18,%a19,";
	%args = %args @ "%a20,%a21,%a22,%a23,%a24,%a25,%a26,%a27,%a28,%a29,";
	%args = %args @ "%a30,%a31,%a32,%a33,%a34,%a35,%a36,%a37,%a38,%a39);";

	for (%i = %this.childLayerCount - 1; %i >= 0; %i--)
	{
		%scope = %this.childLayerScope[%i];

		if (isFunction(%scope, %method))
		{
			if (%value $= "")
			{
				%_value = eval("return" SPC %scope @ "::" @ %method @ %args);

				if (%_value !$= "")
				{
					%value = %_value;
				}
			}
			else
			{
				eval(%scope @ "::" @ %method @ %args);
			}
		}
	}

	return %value;
}

// @func SimObject::terminalCallChildLayer(method[, *args:40])
// @return @any
// @desc
//   Calls the given method on every child layer consecutively from the top.
//   Returns the first return value that is not "". The difference to ::callChildLayer,
//   is that this method stops when it reaches a layer that gives a return value that is not "".

function SimObject::terminalCallChildLayer(%this, %method,
	%a0,  %a1,  %a2,  %a3,  %a4,  %a5,  %a6,  %a7,  %a8,  %a9,
	%a10, %a11, %a12, %a13, %a14, %a15, %a16, %a17, %a18, %a19,
	%a20, %a21, %a22, %a23, %a24, %a25, %a26, %a27, %a28, %a29,
	%a30, %a31, %a32, %a33, %a34, %a35, %a36, %a37, %a38, %a39)
{
	%args = "(%this,%a0,%a1,%a2,%a3,%a4,%a5,%a6,%a7,%a8,%a9,";
	%args = %args @ "%a10,%a11,%a12,%a13,%a14,%a15,%a16,%a17,%a18,%a19,";
	%args = %args @ "%a20,%a21,%a22,%a23,%a24,%a25,%a26,%a27,%a28,%a29,";
	%args = %args @ "%a30,%a31,%a32,%a33,%a34,%a35,%a36,%a37,%a38,%a39);";

	for (%i = %this.childLayerCount - 1; %i >= 0; %i--)
	{
		%scope = %this.childLayerScope[%i];

		if (isFunction(%scope, %method))
		{
			%value = eval("return" SPC %scope @ "::" @ %method @ %args);

			if (%value !$= "")
			{
				return %value;
			}
		}
	}

	return "";
}
