function PerlinNoise(%seed)
{
  if (%seed $= "")
    %seed = getRandom(0, 0xFFFFF);

  %obj = new ScriptObject()
  {
    class = PerlinNoise;
  };

  %obj.setSeed(%seed);
  return %obj;
}

function PerlinNoise::setSeed(%this, %seed)
{
  %this.seed = %seed;

  for (%i = 0; %i < 256; %i++)
    %this.p[%i] = %i;

  %prev = getRandomSeed();
  setRandomSeed(%seed);

  for (%i = 255; %i; %i--)
  {
    %j = getRandom(%i);
    %t = %this.p[%i];

    %this.p[%i] = %this.p[%j];
    %this.p[%j] = %t;
  }

  setRandomSeed(%prev);

  for (%i = 0; %i < 256; %i++)
    %this.p[%i + 256] = %this.p[%i];
}

function PerlinNoise::getSeed(%this)
{
  return %this.seed;
}

function PerlinNoise::noise3D(%this, %x, %y, %z)
{
  %xx = mFloor(%x) & 255;
  %yy = mFloor(%y) & 255;
  %zz = mFloor(%z) & 255;

  %x -= mFloor(%x);
  %y -= mFloor(%y);
  %z -= mFloor(%z);

  %u = fade(%x);
  %v = fade(%y);
  %w = fade(%z);

  %a  = %this.p[%x    ] + %y;
  %aa = %this.p[%a    ] + %z;
  %ab = %this.p[%a + 1] + %z;
  %b  = %this.p[%x + 1] + %y;
  %ba = %this.p[%b    ] + %z;
  %bb = %this.p[%b + 1] + %z;

  return lerp(%w, lerp(%v, lerp(%u, grad(%this.p[%aa  ], %x  , %y  , %z   ),
                                    grad(%this.p[%ba  ], %x-1, %y  , %z   )),
                           lerp(%u, grad(%this.p[%ab  ], %x  , %y-1, %z   ),
                                    grad(%this.p[%bb  ], %x-1, %y-1, %z   ))),
                  lerp(%v, lerp(%u, grad(%this.p[%aa+1], %x  , %y  , %z-1 ),
                                    grad(%this.p[%ba+1], %x-1, %y  , %z-1 )),
                           lerp(%u, grad(%this.p[%ab+1], %x  , %y-1, %z-1 ),
                                    grad(%this.p[%bb+1], %x-1, %y-1, %z-1 ))));
}

function PerlinNoise::noise2D(%this, %x, %y)
{
  return %this.noise3D(%x, %y, 0);
}

function PerlinNoise::noise1D(%this, %x)
{
  return %this.noise3D(%x, 0, 0);
}

function fade(%t)
{
  return %t * %t * %t * (%t * (%t * 6 - 15) + 10);
}

function lerp(%t, %a, %b)
{
  return %a + %t * (%b - %a);
}

function grad(%hash, %x, %y, %z)
{
  %h = %hash & 15;
  %u = %h < 8 ? %x : %y;
  %v = %h < 4 ? %y : (%h == 12 || %h == 14 ? %x : %z);
  return ((%h & 1) == 0 ? %u : -%u) + ((%h & 2) == 0 ? %v : -%v);
}
