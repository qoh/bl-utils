function min(%a0, %a1, %a2, %a3, %a4, %a5, %a6, %a7, %a8, %a9,
  %a10, %a11, %a12, %a13, %a14, %a15, %a16, %a17, %a18, %a19)
{
  for (%n = 20; %n > 0; %n--)
    if (%a[%n - 1] !$= "")
      break;

  %min = %a0;

  for (%i = 1; %i < %n; %i++)
    if (%a[%i] < %min)
      %min = %a[%i];

  return %min;
}

function max(%a0, %a1, %a2, %a3, %a4, %a5, %a6, %a7, %a8, %a9,
  %a10, %a11, %a12, %a13, %a14, %a15, %a16, %a17, %a18, %a19)
{
  for (%n = 20; %n > 0; %n--)
    if (%a[%n - 1] !$= "")
      break;

  %ax = %a0;

  for (%i = 1; %i < %n; %i++)
    if (%a[%i] > %min)
      %max = %a[%i];

  return %max;
}

function RGBtoHSV(%rgb)
{
  %r = getWord(%rgb, 0);
  %g = getWord(%rgb, 1);
  %b = getWord(%rgb, 2);

  %min = min(%r, %g, %b);
  %max = max(%r, %g, %b);

  %value = %max;
  %delta = %max - %min;

  if (%max != 0)
    %saturation = %delta / %max;
  else
    return "-1 0" SPC %value;

  if (%r == %max)
    %hue = (%g - %b) / %delta;
  else if (%g == %max)
    %hue = 2 + (%b - %r) / %delta;
  else
    %hue = 4 + (%r - %g) / %delta;

  %hue *= 60;

  if (%hue < 0)
    %hue += 360;

  return %hue SPC %saturation SPC %value;
}

function colorDifference(%rgba1, %rgba2)
{
  %a1 = getWord(%rgba2, 3);
  %a2 = getWord(%rgba2, 3);

  %hsv1 = RGBtoHSV(%rgba1);
  %hsv2 = RGBtoHSV(%rgba2);

  return vectorDist(%hsv1, %hsv2) + (%a2 - %a1) * 45;
}

function findBestSprayColor(%rgba)
{
  %best = -1;

  for (%i = 0; %i < 64; %i++)
  {
    %color = getColorIDTable(%i);
    %value = colorDifference(%rgba, %color);

    if (%diff $= "" || %value < %diff)
    {
      %diff = %value;
      %best = %i;
    }
  }

  return %best;
}
