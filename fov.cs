// Public: Test if a point is within a view cone.
//
// a - The origin of the cone.
// b - The point to test.
// vector - The direction of the cone.
// angle - The angle size of the cone (default: 90).
//
// Returns true or false.
function isWithinView(%a, %b, %vector, %angle)
{
	if (%angle $= "")
	{
		%angle = 90;
	}

	%product = vectorDot(%vector,
		vectorNormalize(vectorSub(%b, %a))
	);

	return %product >= 1 - (%angle / 360) * 2;
}

function Player::isWithinView(%this, %point, %angle)
{
	if (%angle $= "")
	{
		%client = %this.getControllingClient();

		if (isObject(%client))
		{
			%angle = %client.getControlCameraFOV();
		}
	}

	return isWithinView(%this.getEyePoint(), %b, %this.getEyeVector(), %angle);
}
