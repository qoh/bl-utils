package BotMountThreads {
   function AIPlayer::mountObject(%this, %object, %slot) {
      Parent::mountObject(%this, %object, %slot);
      %thread = %this.getDataBlock().mountThread[%slot];

      if (%thread $= "") {
         %thread = "root";
      }

      %object.setActionThread(%thread);
   }
};

activatePackage("BotMountThreads");
