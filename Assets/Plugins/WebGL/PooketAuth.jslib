// The game's side of sign-in and cloud saves: calls into the page's pooket-auth.js (the WebGL template).
// The page answers with SendMessage to the "PooketAuth" game object (CloudAccount.cs).
mergeInto(LibraryManager.library, {
  // The game's receiver exists: the page sends the account state, then anything it queued.
  PS_AuthInit: function () {
    if (window.PooketAuth) window.PooketAuth.gameReady();
  },

  // Saves the profile (Profile.cs as JSON) to the player's cloud record, if they're signed in.
  PS_Save: function (json) {
    if (window.PooketAuth) window.PooketAuth.save(UTF8ToString(json));
  },

  // The sign-in button over the game: shown on the title screen and settings, hidden while playing.
  PS_ShowAccount: function (show) {
    if (window.PooketAuth) window.PooketAuth.show(!!show);
  },
});
