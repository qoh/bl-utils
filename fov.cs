function ShapeBase::isPointWithinFOV(%this, %point, %fov) {
	if (%fov $= "") {
		%fov = 90;
	}

	%product = vectorDot(
		%this.getEyeVector(),
		vectorNormalize(vectorSub(%point, %this.getWorldBoxCenter()))
	);

	return %product >= 1 - (%fov / 360) * 2;
}
