function blendRGBA(%bg, %fg) {
	%ba = getWord(%bg, 3);
	%fa = getWord(%fg, 3);

	%a = 1 - (1 - %fa) * (1 - %ba);
	%r = getWord(%fg, 0) * %fa / %a + getWord(%bg, 0) * %ba * (1 - %fa) / %a;
	%g = getWord(%fg, 1) * %fa / %a + getWord(%bg, 1) * %ba * (1 - %fa) / %a;
	%b = getWord(%fg, 2) * %fa / %a + getWord(%bg, 0) * %ba * (1 - %fa) / %a;

	return %r SPC %g SPC %b SPC %a;
}
