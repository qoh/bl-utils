function SeededRandom(%seed)
{
  return new ScriptObject()
  {
    class = SeededRandom;
    seed = %seed $= "" ? getRandom(0xFFFFF) : %seed;
  };
}

function SeededRandom::getRandom(%this, %a, %b)
{
  %prev = getRandomSeed();
  setRandomSeed(%this.seed);

  if (%a $= "" && %b $= "")
    %random = getRandom();
  else if (%b $= "")
    %random = getRandom(%a);
  else
    %random = getRandom(%a, %b);

  %this.seed = getRandomSeed();
  setRandomSeed(%prev);

  return %random;
}
